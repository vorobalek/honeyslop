## Honeyslop

An LLM-backed catch-all for HTTP requests that no real route answers. Traefik sends such requests here, and a small
language model running on CPU writes the response a misconfigured server for that host would plausibly give: a leaked
`.env`, a `.git/config`, a WordPress login page, a Spring `actuator/env`. Every response is generated once per
`method + host + path?query` and then served from a shared PostgreSQL cache, so scanners that repeat the same probes
cost nothing.

### Components

| Image | Project | Role |
|---|---|---|
| `honeyslop-trap` | `src/Honeyslop.Trap` | Answers the catch-all on `:8080`; `/health` and `/metrics` on `:8081` |
| `honeyslop-admin` | `src/Honeyslop.Admin` | Web UI: generated responses, request history per client, runtime settings |
| `honeyslop-migrator` | `src/Honeyslop.Migrator` | Creates the database if it does not exist and applies EF Core migrations |
| `honeyslop-llm` | `llm/Dockerfile` | `llama-server` from llama.cpp in distroless: no shell, no TLS, no subprocesses, no model |
| `honeyslop-model` | `model/Dockerfile` | Qwen3-4B-Instruct-2507 Q4_K_M as a single `/model.gguf`, pinned by sha256 |

The model image is never run. The model server mounts it read-only at `/models` as a Kubernetes image volume, so the
node pulls it like any image and the model server pod needs no network access of its own. Changing the model means
building a different `model/Dockerfile`; the engine image does not change.

### How a request is answered

1. Requests for binary files (`.ico`, `.png`, `.pdf`, `.zip`, fonts, media, archives, executables, databases) and
   unsupported methods get a static nginx-style error without touching the cache or the model.
2. The cache is checked in memory, then in PostgreSQL. A ready entry is served as stored, with
   `Content-Security-Policy: sandbox` added so generated pages cannot run scripts or submit forms under our domain.
3. A new path is put into the shared queue, which is the `entry` table itself, and the request waits for it within
   the wait window. A request for a path that is already queued or being generated waits for that entry instead.
4. Workers of every trap instance take entries with `update … for update skip locked`: entries somebody asked for
   within the wait window go first, newest first, then the rest oldest first, so nothing starves. A worker keeps a
   heartbeat on the entry it generates; an entry without a heartbeat is taken over by another instance.
5. Generations go to the model server with a free slot: every instance resolves `LLM_URL` (a headless service or a
   comma-separated list) to all servers, probes their `/slots` and picks the one with the most idle slots. A server is
   named by the reverse DNS of its address, so the pods of a StatefulSet keep their names across restarts; without a
   reverse record the name is the address.
6. A model server that is down, restarting or answering 5xx puts the entry straight back into the queue without
   spending an attempt. Broken model output counts as a failed attempt; a path that fails three times gets a plain
   404 until it is expired.
7. Every request is recorded with its headers and body. A request that has to wait is recorded at once as
   `waiting` and updated with its outcome later.

### Admin

Entries, Requests, Clients, Generators and Settings update live: PostgreSQL triggers send `pg_notify` on every change,
the admin listens and pushes over a WebSocket at `/live`. Generators shows every model server with its slots, speed
and production, the entries being generated right now and the queue.

The page of an entry being generated shows the model output as it streams. The worker reads the completion as a
stream and sends what it has so far once a second through `pg_notify('honeyslop_outputs', …)`. The partial output is
not stored anywhere: the admin keeps the last one in memory for a minute, and only the tail fits when it outgrows
the 8000-byte notification limit.

### Configuration

Trap:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `LLM_URL` | — | Model servers: a headless service name resolving to every replica, or a comma-separated list |
| `TRAP_PORT`, `MANAGEMENT_PORT` | `8080`, `8081` | Catch-all port and health/metrics port |
| `TRUSTED_NETWORKS` | `10.0.0.0/8` | Proxies allowed to set `X-Forwarded-For` |
| `GENERATION_WORKERS` | `6` | Concurrent generations per instance |
| `GENERATION_MAX_ATTEMPTS` | `3` | Failed generations of one path before giving up |
| `STATIC_EXTENSIONS` | built-in list | Comma-separated extensions answered without the model |
| `LLM_MAX_TOKENS`, `LLM_TEMPERATURE` | `1024`, `0.7` | Generation parameters |

Admin:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `PORT` | `8080` | HTTP port |
| `MAINTENANCE_INTERVAL_SECONDS` | `60` | How often expiration and pruning run |
| `LLM_URL` | — | Model servers to show on the Generators page |
| `OIDC_AUTHORITY` | — | OpenID Connect issuer to sign in with; unset turns sign-in off |
| `OIDC_CLIENT_ID` | `honeyslop` | Client registered at the issuer |
| `OIDC_CLIENT_SECRET` | — | Its secret, required with `OIDC_AUTHORITY` |

Runtime settings live in the database and are edited on the admin Settings page; trap instances reload them every ten
seconds:

| Setting | Default | Meaning |
|---|---|---|
| Expire generated responses after | never | A response older than this expires and is generated again on the next request |
| Expire entries nobody requested for | never | Counted from the last time a response was served |
| Hold a request open for its new response | 1 minute | After this a client waiting for a new path gets a plain 404 |
| Let the model refuse | on | Off: every path gets status 200 and invented content |
| Give one generation at most | 10 minutes | Counted from the moment a model server slot takes it |
| Treat a generation as abandoned after silence of | 2 minutes | Heartbeats come every quarter of this |
| Retry a failed generation after | 1 minute | Only broken model output counts as a failure |
| Keep the request history for | 30 days | Every request is recorded with its headers and body |
| Generate responses for new requests | on | Off serves only what is cached |
| Show everything read-only without signing in | off | On: anyone may browse the admin, changes still take signing in |

Saving applies the thresholds to everything already stored right away.

Expiring an entry, by hand or by these thresholds, does not delete it. It stops being served, frees its path for a
new generation and stays viewable from the requests it answered, and it still counts in the generator statistics. It
is deleted once the request history no longer refers to it and it expired more than a day ago.

### Access

With `OIDC_AUTHORITY` set, the admin signs users in itself through the authorization code flow with PKCE and keeps
them signed in with its own cookie for twelve hours since the last visit. Whoever the issuer lets through may do
everything: the issuer decides who gets in. Without a session every page sends the visitor to sign in, unless the
read-only setting is on: then anyone may open every page and the live feed, while saving settings and expiring
entries still send them to sign in. Turning the setting off cuts open live connections of visitors who did not sign
in. The admin trusts `X-Forwarded-Proto` from anyone, so only the proxy may reach it.

Without `OIDC_AUTHORITY` the admin has no authentication of its own, ignores the setting and must sit behind an
authenticating proxy.

The cookie keys live in memory, so a restart signs everyone out.

### Database

The service only needs a role that may create databases:

```sql
create role honeyslop with login createdb password '...';
```

The migrator connects with `Database=honeyslop` in the connection string, creates that database owned by the role and
applies the migrations. Add a migration with `etc/add_migration.sh <name>`.

### Local run

```bash
docker compose -f docker-compose.postgres.yaml up -d
docker compose -f docker-compose.postgres.yaml exec postgres psql -c "create role honeyslop with login createdb password 'honeyslop'"
dotnet run --project src/Honeyslop.Migrator
dotnet run --project src/Honeyslop.Trap
dotnet run --project src/Honeyslop.Admin
```

`launchSettings.json` expects a model server on `http://localhost:18080`.

To try sign-in, start a mock OpenID Connect issuer that lets anyone in under any name and run the admin with the
`Honeyslop.Admin.Oidc` profile:

```bash
docker compose -f docker-compose.oidc.yaml up -d
dotnet run --project src/Honeyslop.Admin --launch-profile Honeyslop.Admin.Oidc
```

### Tests

```bash
dotnet test Honeyslop.sln
```

`Honeyslop.Data.Tests` start PostgreSQL in Docker through Testcontainers and are marked `Integration`; the image build
runs only the unit tests.

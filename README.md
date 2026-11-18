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
| `honeyslop-reporter` | `src/Honeyslop.Reporter` | Sends queued abuse reports over SMTP; `/health` and `/metrics` on `:8081` |
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

Every client address carries the flag of its country and links to the page of that address: country, first and last
request, its requests with the address filter locked and the other filters free, and what RIPEstat knows about the
network. That is the routed prefix and origin AS, the registered network and its owner, the abuse contacts and the
raw registry records. The network part loads after the page, since RIPEstat sometimes takes seconds. Its answers are
kept in the `client_whois` table and asked again once they are older than twelve hours; when RIPEstat does not answer,
the stored one is shown. The page opens only for addresses in the request history or recorded as the first client of
an entry, so the admin cannot be used to look up arbitrary addresses. RIPEstat sees the User-Agent set on the
Settings page, or `USER_AGENT` when the setting is empty.

Countries come from the [DB-IP](https://db-ip.com) IP to Country Lite database, licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The image build downloads it into the admin image, unmodified
and with a `NOTICE` next to it, and every admin page links to DB-IP in its footer. The build takes the current month's
file, or the previous month's before the new one is out. Each release brings it up to date, and every admin replica
reads the same file from its image.

### Abuse reports

A signed-in user can send the abuse contacts of an address a report about it: a letter with the totals and the latest
requests, and a zip archive with up to 50 000 of its requests, oldest first, as they were received. The archive has no
responses of ours, no cookie values and none of the headers our proxy adds. Visitors who did not sign in see neither
the letter nor the send form, and never who sent a report. The sender name is the one typed next to the button,
filled in from the user's personal setting or `ABUSE_REPORT_SENDER_NAME`; it goes only into the From header. Every
report is listed on the address page and on the Reports page, with the letter as it was sent; the From header and
who asked for the report are shown only to signed-in users. Under the list a signed-in user also finds the current
letter and can download the archive the reporter would attach, to send it by hand.

The button only queues the report in the `abuse_report` table; `honeyslop-reporter` sends it:

1. A reporter claims the oldest pending report with a conditional update, so two reporters never take the same one,
   and keeps the claim alive every 15 seconds while it sends.
2. A claim silent for two minutes is taken over by another reporter: a reporter that died does not leave the report
   stuck. Every claim counts as an attempt, and a report that used five attempts is marked failed.
3. A failed delivery is retried after 1, 5, 15 and 60 minutes. A recipient or sender the SMTP server refuses fails
   the report at once.
4. An address has at most one pending report. A sent report records the id of the last request in its archive, and
   a new report is offered only once requests after that one arrive.

A reporter that dies between the SMTP server accepting the letter and the database recording it sends the letter
again after the takeover. Until `SMTP_PASSWORD` and the rest are set, reporters leave the queue alone.

Reporters export `honeyslop_abuse_reports{state}`, the reports in the table by state (queued, sending, retrying,
sent, failed), counted from the database on every scrape: every reporter shows the same numbers, so dashboards take
the `max`. `honeyslop_reporter_configured` is 1 on a reporter with SMTP settings.

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
| `GEOIP_DATABASE` | `/app/geoip/country.mmdb` | DB-IP country database; a missing file only hides the flags |
| `USER_AGENT` | `Honeyslop (+https://github.com/vorobalek/honeyslop)` | User-Agent for RIPEstat when the setting is empty |
| `ABUSE_REPORT_SENDER_NAME` | — | Sender name of abuse reports for users without their own |

Reporter:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `SMTP_HOST`, `SMTP_PORT` | —, `587` | SMTP submission server |
| `SMTP_STARTTLS` | `true` | Off only for a local test server |
| `SMTP_USERNAME`, `SMTP_PASSWORD` | — | SMTP login |
| `SMTP_FROM` | — | Sender address; the SMTP server must allow it for this login |
| `MAX_ATTACHED_REQUESTS` | `50000` | The latest requests of an address put into the archive |
| `MANAGEMENT_PORT` | `8081` | Health and metrics port |

The image build takes a `USER_AGENT` build argument for the DB-IP download.

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
| User-Agent | `USER_AGENT` | Sent with RIPEstat lookups |

Each signed-in user also has a Personal section there, which nobody else sees: the name they sign abuse reports as.

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

Flags need the country database; point `GEOIP_DATABASE` at a downloaded copy:

```bash
curl -fsSL https://download.db-ip.com/free/dbip-country-lite-$(date -u +%Y-%m).mmdb.gz | gunzip > country.mmdb
```

To try abuse reports without mailing anyone, run a local mail catcher and point the reporter at it; the letters show up
at `http://localhost:8025`:

```bash
docker run -d --rm -p 8025:8025 -p 1025:1025 -e MP_SMTP_AUTH_ACCEPT_ANY=1 -e MP_SMTP_AUTH_ALLOW_INSECURE=1 axllent/mailpit:v1.34.0
SMTP_HOST=localhost SMTP_PORT=1025 SMTP_STARTTLS=false SMTP_USERNAME=test SMTP_PASSWORD=test SMTP_FROM=security@example.test \
  dotnet run --project src/Honeyslop.Reporter
```

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

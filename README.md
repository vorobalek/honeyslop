## Honeyslop

LLM-backed catch-all for HTTP requests that no real route answers. A small language model writes the response a
misconfigured server for that host would plausibly give (a leaked `.env`, a `.git/config`, a WordPress login), once
per `method + host + path?query`; the answer is cached in PostgreSQL and served to everyone who asks again.

### Key features

- Generated responses cached in PostgreSQL, a shared generation queue across trap instances
- Every request recorded with headers and body, per-client history with country and RIPEstat network data
- Admin web UI with live updates, read-only public view, sign-in through OpenID Connect
- Abuse reports to the network's abuse contacts, queued and sent over SMTP with the request log attached
- MCP endpoint over the same services, for Claude and other MCP clients
- Prometheus metrics on every service

### How a request is answered

1. Binary files and unsupported methods get a static error without the cache or the model.
2. A cached response is served from memory or PostgreSQL with `Content-Security-Policy: sandbox`.
3. A new path is queued in the `entry` table; the request waits for it within the wait window.
4. Trap workers claim queued entries with `for update skip locked`, keep a heartbeat, and send them to the model server
   with the most idle slots. A silent claim is taken over by another instance.
5. A model server that is down requeues the entry without spending an attempt; three broken answers end in a 404.

Times that are stored or compared across instances come from `select now()` in PostgreSQL.

### Solution layout

| Project | Role |
|---|---|
| `src/Honeyslop.Trap` | Catch-all on `:8080`, health and metrics on `:8081` |
| `src/Honeyslop.Admin` | Razor UI, live updates at `/live`, MCP at `/mcp` |
| `src/Honeyslop.Reporter` | Sends queued abuse reports over SMTP |
| `src/Honeyslop.Migrator` | Creates the database and applies EF Core migrations |
| `lib/Honeyslop.Admin.Core` | Admin services shared by the UI and MCP, with access checks |
| `lib/Honeyslop.Data` | Entities, repositories, migrations model |
| `lib/Honeyslop.Llm` | Model server discovery and client |
| `lib/Honeyslop.Reports` | Request log archive for abuse reports |
| `llm/`, `model/` | `llama-server` image and the Qwen3-4B model image |
| `tests/visual` | Screenshot comparison of every admin page |

### Configuration

Trap:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `LLM_URL` | — | Model servers: a headless service or a comma-separated list |
| `TRAP_PORT`, `MANAGEMENT_PORT` | `8080`, `8081` | Catch-all port, health and metrics port |
| `TRUSTED_NETWORKS` | `10.0.0.0/8` | Proxies allowed to set `X-Forwarded-For` |
| `GENERATION_WORKERS` | `6` | Concurrent generations per instance |
| `GENERATION_MAX_ATTEMPTS` | `3` | Failed generations of one path before giving up |
| `STATIC_EXTENSIONS` | built-in list | Extensions answered without the model |
| `LLM_MAX_TOKENS`, `LLM_TEMPERATURE` | `1024`, `0.7` | Generation parameters |

Admin:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `PORT` | `8080` | HTTP port |
| `MAINTENANCE_INTERVAL_SECONDS` | `60` | How often expiration and pruning run |
| `LLM_URL` | — | Model servers shown on the Generators page |
| `OIDC_AUTHORITY` | — | OpenID Connect issuer; unset turns sign-in and MCP off |
| `OIDC_CLIENT_ID`, `OIDC_CLIENT_SECRET` | `honeyslop`, — | Admin client at the issuer |
| `MCP_RESOURCE` | — | Public address of `/mcp`, the audience of its access tokens; unset turns MCP off |
| `GEOIP_DATABASE` | `/app/geoip/country.mmdb` | DB-IP country database |
| `USER_AGENT` | `Honeyslop (+https://github.com/vorobalek/honeyslop)` | User-Agent for RIPEstat |
| `ABUSE_REPORT_SENDER_NAME` | — | Default sender name of abuse reports |
| `SMTP_FROM` | — | Sender address shown before a report is sent |

Reporter:

| Variable | Default | Meaning |
|---|---|---|
| `DB_CONNECTION_STRING` | — | PostgreSQL connection string |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_STARTTLS` | —, `587`, `true` | SMTP submission server |
| `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM` | — | SMTP login and sender address |
| `MAX_ATTACHED_REQUESTS` | `50000` | Requests of an address put into the archive |
| `MANAGEMENT_PORT` | `8081` | Health and metrics port |

Runtime settings (cache lifetimes, generation limits, request retention, public view, outbound User-Agent) live in the
database and are edited on the admin Settings page or through MCP; trap instances reload them every ten seconds.

### Access and MCP

- With `OIDC_AUTHORITY` the admin signs users in with the authorization code flow; without it the admin must sit
  behind an authenticating proxy. The public view lets anyone read, changes always need signing in.
- `/mcp` serves the Model Context Protocol (Streamable HTTP, stateless). Calls need a JWT access token from the same
  issuer with `MCP_RESOURCE` as audience; `/.well-known/oauth-protected-resource` points clients to the issuer and asks
  for `openid profile offline_access`.

### Abuse reports

The admin queues a report in `abuse_report`; reporters claim it with a heartbeat, retry after 1, 5, 15 and 60 minutes
and give up after five attempts. An address has at most one pending report and gets a new one only after new requests.

Countries come from [DB-IP](https://db-ip.com) IP to Country Lite, licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

### Local development

```bash
docker compose -f docker-compose.postgres.yaml up -d
docker compose -f docker-compose.postgres.yaml exec postgres psql -c "create role honeyslop with login createdb password 'honeyslop'"
dotnet run --project src/Honeyslop.Migrator
dotnet run --project src/Honeyslop.Trap
dotnet run --project src/Honeyslop.Admin
```

- Model server: `launchSettings.json` expects one on `http://localhost:18080`.
- Flags: point `GEOIP_DATABASE` at `https://download.db-ip.com/free/dbip-country-lite-YYYY-MM.mmdb.gz`, unpacked.
- Sign-in: `docker compose -f docker-compose.oidc.yaml up -d` and the `Honeyslop.Admin.Oidc` launch profile.
- Abuse reports: run `axllent/mailpit` on `:1025` and the reporter with `SMTP_STARTTLS=false`.

### Tests

```bash
dotnet test Honeyslop.sln
```

`Honeyslop.Data.Tests` start PostgreSQL through Testcontainers and are marked `Integration`; the image build runs only
the unit tests.

`tests/visual` compares the admin before and after a change: `./reset.sh` seeds the `honeyslop_visual` database,
`node capture.mjs <admin url> <dir>` screenshots every page, `node compare.mjs <before> <after> <diff>` diffs them.

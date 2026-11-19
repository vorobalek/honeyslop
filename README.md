## Honeyslop

Catch-all for HTTP requests that no real route answers. A small language model writes what the software a request probes
for would return (a camera's device info, a leaked `.env`, a WordPress installer), once per method, host and path; the
answer is cached in PostgreSQL and served to everyone who asks again, until an operator has it regenerated with a comment
for the model or writes it by hand. Entries can also be made by hand before anyone asks, and responses can be held back
by a delay set on a client, on an entry or as the default, to keep scanners waiting. Every request is recorded, and the
networks that scan can be sent an abuse report with their request log attached; the reports that bounce show up next to
the addresses that bounced them.

### Services

|                          |                                                                             |
| ------------------------ | --------------------------------------------------------------------------- |
| `src/Honeyslop.Trap`     | The catch-all, with a generation queue shared by all instances              |
| `src/Honeyslop.Admin`    | Admin web app from `web/` (React, updated live over SignalR), MCP at `/mcp` |
| `src/Honeyslop.Reporter` | Mails queued abuse reports and reads their bounces from a mailbox           |
| `src/Honeyslop.Migrator` | Creates the database and applies migrations                                 |
| `llm/`, `model/`         | `llama-server` and the Qwen3-4B model images                                |

### Configuration

| Trap                                | Default        |                                                                               |
| ----------------------------------- | -------------- | ----------------------------------------------------------------------------- |
| `DB_CONNECTION_STRING`              |                | PostgreSQL                                                                    |
| `LLM_URL`                           |                | Model servers: a headless service or comma-separated URLs                     |
| `TRAP_PORT`, `MANAGEMENT_PORT`      | `8080`, `8081` | Catch-all; health and metrics                                                 |
| `TRUSTED_NETWORKS`                  | `10.0.0.0/8`   | Proxies trusted with `X-Forwarded-For`                                        |
| `GENERATION_WORKERS`                | `6`            | Concurrent generations per instance                                           |
| `GENERATION_MAX_ATTEMPTS`           | `3`            | Broken answers before a path gets a 404                                       |
| `STATIC_EXTENSIONS`                 | built in       | Extensions answered without the model                                         |
| `MAX_TARGET_LENGTH`                 | `1024`         | Longer paths get a 414 without the model, at most 2048                        |
| `RESPONSE_DELAY_MILLISECONDS`       | `0`            | Least time from a request to an answer from an entry, unless set in the admin |
| `LLM_MAX_TOKENS`, `LLM_TEMPERATURE` | `1024`, `0.7`  |                                                                               |

| Admin                                                                   | Default                                               |                                                                              |
| ----------------------------------------------------------------------- | ----------------------------------------------------- | ---------------------------------------------------------------------------- |
| `DB_CONNECTION_STRING`                                                  |                                                       | PostgreSQL                                                                   |
| `PORT`                                                                  | `8080`                                                |                                                                              |
| `LLM_URL`                                                               |                                                       | Model servers for the Generators page                                        |
| `OIDC_AUTHORITY`                                                        |                                                       | Sign-in issuer; without it, an authenticating proxy goes in front            |
| `OIDC_CLIENT_ID`, `OIDC_CLIENT_SECRET`                                  | `honeyslop`                                           |                                                                              |
| `MCP_RESOURCE`                                                          |                                                       | Public URL of `/mcp`; turns MCP on when sign-in is on                        |
| `GEOIP_DATABASE`                                                        | `/app/geoip/country.mmdb`                             | DB-IP country database                                                       |
| `USER_AGENT`                                                            | `Honeyslop (+https://github.com/vorobalek/honeyslop)` | For RIPEstat lookups                                                         |
| `ABUSE_REPORT_SENDER_NAME`, `SMTP_FROM`                                 |                                                       | Shown on a report before it is sent                                          |
| `MAINTENANCE_INTERVAL_SECONDS`                                          | `60`                                                  | Expiration and pruning                                                       |
| `STATIC_EXTENSIONS`, `MAX_TARGET_LENGTH`, `RESPONSE_DELAY_MILLISECONDS` | as for the trap                                       | Set like the trap's: they check hand-made entries and show the default delay |

| Reporter                                      | Default       |                                                             |
| --------------------------------------------- | ------------- | ----------------------------------------------------------- |
| `DB_CONNECTION_STRING`                        |               | PostgreSQL                                                  |
| `SMTP_HOST`, `SMTP_PORT`, `SMTP_STARTTLS`     | `587`, `true` |                                                             |
| `SMTP_USERNAME`, `SMTP_PASSWORD`, `SMTP_FROM` |               |                                                             |
| `MAX_ATTACHED_REQUESTS`                       | `50000`       | Requests in the attached archive                            |
| `IMAP_HOST`, `IMAP_PORT`                      | `993`         | Mailbox that receives the bounces; without it none are read |
| `IMAP_USERNAME`, `IMAP_PASSWORD`              |               |                                                             |
| `IMAP_FOLDERS`                                | `INBOX`       | Comma-separated folders to read                             |
| `IMAP_POLL_SECONDS`                           | `300`         |                                                             |
| `MANAGEMENT_PORT`                             | `8081`        | Health and metrics                                          |

Cache lifetimes, generation limits, request retention, the public view, the default response delay and the abuse
addresses that never get reports are edited in the admin or over MCP.

### Development

```bash
docker compose -f docker-compose.postgres.yaml up -d
docker compose -f docker-compose.postgres.yaml exec postgres psql -c "create role honeyslop with login createdb password 'honeyslop'"
dotnet run --project src/Honeyslop.Migrator
dotnet run --project src/Honeyslop.Trap
(cd web && npm ci && npm run build)
dotnet run --project src/Honeyslop.Admin
```

`launchSettings.json` expects a model server on `:18080`; `docker-compose.oidc.yaml` with the `Honeyslop.Admin.Oidc`
profile adds sign-in, and Mailpit on `:1025` with `SMTP_STARTTLS=false` receives abuse reports. `npm run dev` in `web`
serves the app with hot reload against the admin at `HONEYSLOP_ADMIN_URL`.

### Tests

```bash
dotnet test Honeyslop.sln
(cd web && npm run check)
```

`Honeyslop.Data.Tests` need Docker. `tests/visual/reset.sh` seeds `honeyslop_visual` for the browser checks:
`capture.mjs` and `compare.mjs` in `tests/visual` diff screenshots of every admin page, and `npm run audit:renders` in
`web`, after `npm run build:audit`, holds every admin action to a render budget.

Countries come from [DB-IP](https://db-ip.com) IP to Country Lite, licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

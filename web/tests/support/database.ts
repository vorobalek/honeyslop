import { execFileSync } from 'node:child_process';

const COMMAND = (
    process.env.HONEYSLOP_AUDIT_PSQL ??
    'docker exec -i postgres-postgres-1 psql -U postgres -d honeyslop_visual -v ON_ERROR_STOP=1 -q -tA'
).split(' ');

export function sql(statement: string): string {
    const [command, ...args] = COMMAND;
    return execFileSync(command, args, { input: statement, encoding: 'utf8' }).trim();
}

export function recordRequest(clientIp: string, target: string): void {
    sql(`insert into request (received_at, client_ip, method, host, target, user_agent, outcome, response_status, duration_ms)
         values (now(), '${clientIp}', 'GET', 'audit.example.test', '${target}', 'render-audit', 0, 200, 12);`);
}

export function hitEntry(id: number): void {
    sql(`update entry set hits = hits + 1, last_hit_at = now() where id = ${id};`);
}

export function streamOutput(entryId: number, content: string): void {
    const payload = JSON.stringify({ entryId, tokens: content.split(' ').length, content, truncated: false });
    sql(`select pg_notify('honeyslop_outputs', '${payload.replace(/'/g, "''")}');`);
}

import { EntryResponseType } from '../../types/entryResponseType';
import { ResponseHeaderType } from '../../types/responseHeaderType';

export type ResponseRead = { response: EntryResponseType } | { error: string };

export function headerLines(headers: readonly ResponseHeaderType[]): string {
    return headers.map((header) => `${header.name}: ${header.value}`).join('\n');
}

export function readGuidance(data: FormData): string | null {
    const guidance = String(data.get('Guidance') ?? '').trim();
    return guidance === '' ? null : guidance;
}

export function readResponse(data: FormData): ResponseRead {
    const headers: ResponseHeaderType[] = [];
    for (const line of String(data.get('Headers') ?? '').split(/\r?\n/)) {
        if (line.trim() === '') continue;
        const colon = line.indexOf(':');
        if (colon < 1) return { error: `Write each header as Name: value, "${line.trim()}" is not.` };
        headers.push({ name: line.slice(0, colon).trim(), value: line.slice(colon + 1).trim() });
    }
    return { response: { status: Number(data.get('Status')), headers, body: String(data.get('Body') ?? '') } };
}

import { postNotice } from './noticeStore';

const HUB_ERROR = /HubException: (.*)$/;

function reasonOf(error: unknown): string {
    const message = error instanceof Error ? error.message : String(error);
    return HUB_ERROR.exec(message)?.[1] ?? 'The server did not answer, try again.';
}

export function perform(action: () => Promise<void>): void {
    action().catch((error: unknown) => postNotice(reasonOf(error)));
}

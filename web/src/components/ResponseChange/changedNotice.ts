import { ResponseChangeModeType } from '../../types/responseChangeModeType';

export function changedNotice(mode: ResponseChangeModeType, count: number): string {
    const responses = count === 1 ? 'response' : 'responses';
    return mode === 'regenerate'
        ? `Queued ${count} new ${responses} for generation.`
        : `Replaced ${count} ${responses}.`;
}

export function createdNotice(mode: ResponseChangeModeType): string {
    return mode === 'regenerate'
        ? 'Created the entry and queued it for generation.'
        : 'Created the entry with your response.';
}

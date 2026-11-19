import { ResponseChangeModeType } from '../../types/responseChangeModeType';

export function changedNotice(mode: ResponseChangeModeType, count: number): string {
    const responses = count === 1 ? 'response' : 'responses';
    return mode === 'regenerate'
        ? `Queued ${count} new ${responses} for generation.`
        : `Replaced ${count} ${responses}.`;
}

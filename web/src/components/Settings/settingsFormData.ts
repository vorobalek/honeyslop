import { joinDuration } from '../../lib/durations';
import { SettingsFormType } from '../../types/settingsFormType';

export const FIELD_OF_ERROR: Record<string, string> = {
    CacheTtl: 'CacheTtl',
    KeepUnused: 'KeepUnused',
    ClaimTimeout: 'ClaimTimeout',
    RetryFailedAfter: 'RetryFailed',
    GenerationTimeLimit: 'TimeLimit',
    GenerationWait: 'GenerationWait',
    RequestRetention: 'RequestRetention',
    UserAgent: 'UserAgent',
    ResponseDelayMilliseconds: 'ResponseDelay',
    AutoDelayMinMilliseconds: 'AutoDelayMin',
    AutoDelayMaxMilliseconds: 'AutoDelayMax',
};

export function readSettingsForm(form: HTMLFormElement): SettingsFormType {
    const data = new FormData(form);
    const duration = (name: string) => joinDuration(data.get(`${name}Value`), data.get(`${name}Unit`));
    return {
        cacheTtlSeconds: duration('CacheTtl'),
        keepUnusedSeconds: duration('KeepUnused'),
        generationEnabled: data.has('GenerationEnabled'),
        requestRetentionSeconds: duration('RequestRetention'),
        generationWaitSeconds: duration('GenerationWait'),
        allowRefusals: data.has('AllowRefusals'),
        claimTimeoutSeconds: duration('ClaimTimeout'),
        retryFailedSeconds: duration('RetryFailed'),
        generationTimeLimitSeconds: duration('TimeLimit'),
        publicView: data.has('PublicView'),
        userAgent: String(data.get('UserAgent') ?? ''),
        responseDelayMilliseconds: optionalInteger(data.get('ResponseDelay')),
        autoDelayEnabled: data.has('AutoDelayEnabled'),
        autoDelayMinMilliseconds: Number(data.get('AutoDelayMin')),
        autoDelayMaxMilliseconds: Number(data.get('AutoDelayMax')),
    };
}

function optionalInteger(value: FormDataEntryValue | null): number | null {
    const text = String(value ?? '').trim();
    return text === '' ? null : Number(text);
}

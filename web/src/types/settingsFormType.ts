export interface SettingsFormType {
    cacheTtlSeconds: number;
    keepUnusedSeconds: number;
    generationEnabled: boolean;
    requestRetentionSeconds: number;
    generationWaitSeconds: number;
    allowRefusals: boolean;
    claimTimeoutSeconds: number;
    retryFailedSeconds: number;
    generationTimeLimitSeconds: number;
    publicView: boolean;
    userAgent: string | null;
    responseDelayMilliseconds: number | null;
}

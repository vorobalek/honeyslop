export interface HoneyslopSettingsType {
    cacheTtl: string;
    keepUnused: string;
    generationEnabled: boolean;
    requestRetention: string;
    generationWait: string;
    allowRefusals: boolean;
    claimTimeout: string;
    retryFailedAfter: string;
    generationTimeLimit: string;
    publicView: boolean;
    userAgent: string | null;
}

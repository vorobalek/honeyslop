export interface LlmServerStatusType {
    model: string | null;
    parameters: number | null;
    sizeBytes: number | null;
    slots: number;
    busySlots: number;
    predictedTokens: number;
    promptTokensPerSecond: number | null;
    predictedTokensPerSecond: number | null;
    requestsDeferred: number;
}

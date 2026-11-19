import { LlmServerStatusType } from './llmServerStatusType';

export interface GeneratorType {
    name: string;
    address: string | null;
    status: LlmServerStatusType | null;
    discovered: boolean;
    generated: number;
    averageMs: number;
    averageTokens: number;
    lastGeneratedAt: string | null;
}

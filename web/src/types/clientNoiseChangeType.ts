import { NoiseTierType } from './noiseTierType';

export interface ClientNoiseChangeType {
    errors: Record<string, string>;
    noise: NoiseTierType | null;
    changed: boolean;
}

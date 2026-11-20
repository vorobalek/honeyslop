import { NoiseTierType } from '../types/noiseTierType';

export const NOISE_TIERS: readonly NoiseTierType[] = ['Tier1', 'Tier2', 'Tier3'];

const MEGABYTES: Record<NoiseTierType, number> = {
    Tier1: 10,
    Tier2: 20,
    Tier3: 40,
};

export function noiseLabel(tier: NoiseTierType): string {
    return `Tier ${tier.slice(-1)}`;
}

export function noiseSize(tier: NoiseTierType): string {
    return `${MEGABYTES[tier]} MB`;
}

import { SortableColumn } from '../../hooks/useClientSort';
import { grouped, oneDecimal } from '../../lib/numbers';
import { displayOrDash } from '../../lib/time';
import { GeneratorType } from '../../types/generatorType';

export function modelDetails(generator: GeneratorType): string[] {
    const status = generator.status;
    if (status === null) return [];
    const details: string[] = [];
    if (status.parameters !== null) details.push(`${oneDecimal(status.parameters / 1e9)}B`);
    if (status.sizeBytes !== null) details.push(`${oneDecimal(status.sizeBytes / 1024 / 1024 / 1024)} GB`);
    return details;
}

export function generatorState(generator: GeneratorType): { label: string; badge: string } {
    const status = generator.status;
    if (status !== null) {
        const saturated = status.busySlots === status.slots;
        return saturated
            ? { label: 'saturated', badge: 'badge badge-pending' }
            : { label: 'up', badge: 'badge badge-ready' };
    }
    return generator.discovered
        ? { label: 'unreachable', badge: 'badge badge-failed' }
        : { label: 'gone', badge: 'badge badge-static' };
}

function rate(value: number | null | undefined): string {
    return value === null || value === undefined ? '—' : oneDecimal(value);
}

export const GENERATOR_COLUMNS: readonly SortableColumn<GeneratorType>[] = [
    { label: 'Generator', value: (x) => x.name + (x.address && x.address !== x.name ? x.address : '') },
    { label: 'State', value: (x) => generatorState(x).label },
    { label: 'Model', value: (x) => (x.status ? (x.status.model ?? '') + modelDetails(x).join(', ') : '—') },
    { label: 'Slots', className: 'number', value: (x) => String(x.status?.busySlots ?? -1) },
    { label: 'Prompt t/s', className: 'number', value: (x) => rate(x.status?.promptTokensPerSecond) },
    { label: 'Gen t/s per slot', className: 'number', value: (x) => rate(x.status?.predictedTokensPerSecond) },
    { label: 'Deferred', className: 'number', value: (x) => (x.status ? String(x.status.requestsDeferred) : '—') },
    { label: 'Tokens out', className: 'number', value: (x) => (x.status ? grouped(x.status.predictedTokens) : '—') },
    { label: 'Answers 24h', className: 'number', value: (x) => String(x.generated) },
    {
        label: 'Avg time',
        className: 'number',
        value: (x) => (x.generated > 0 ? `${oneDecimal(x.averageMs / 1000)} s` : '—'),
    },
    {
        label: 'Avg tokens',
        className: 'number',
        value: (x) => (x.generated > 0 ? String(Math.round(x.averageTokens)) : '—'),
    },
    { label: 'Last answer', value: (x) => displayOrDash(x.lastGeneratedAt) },
];

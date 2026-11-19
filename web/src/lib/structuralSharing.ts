type Plain = Record<string, unknown>;

function isPlain(value: unknown): value is Plain {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function idOf(value: unknown): unknown {
    return isPlain(value) ? value.id : undefined;
}

function shareArray(previous: unknown[], next: unknown[]): unknown[] {
    const keyed = next.length > 0 && next.every((item) => typeof idOf(item) === 'number');
    const byId = keyed ? new Map(previous.map((item) => [idOf(item), item])) : undefined;
    let same = previous.length === next.length;
    const result = next.map((item, index) => {
        const before = byId ? byId.get(idOf(item)) : previous[index];
        const shared = replaceEqualDeep(before, item);
        if (shared !== previous[index]) same = false;
        return shared;
    });
    return same ? previous : result;
}

function shareObject(previous: Plain, next: Plain): Plain {
    const keys = Object.keys(next);
    let same = keys.length === Object.keys(previous).length;
    const result: Plain = {};
    for (const key of keys) {
        result[key] = replaceEqualDeep(previous[key], next[key]);
        if (result[key] !== previous[key]) same = false;
    }
    return same ? previous : result;
}

export function replaceEqualDeep<T>(previous: unknown, next: T): T {
    if (Object.is(previous, next)) return previous as T;
    if (Array.isArray(previous) && Array.isArray(next)) return shareArray(previous, next) as T;
    if (isPlain(previous) && isPlain(next)) return shareObject(previous, next) as T;
    return next;
}

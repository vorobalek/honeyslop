import { acquireWatch, retainWatch, Watch } from '../lib/watchStore';
import { useEffect } from 'react';

export function useWatch<T>(method: string, ...args: unknown[]): Watch<T> {
    const watch = acquireWatch<T>(method, args);
    useEffect(() => retainWatch(watch), [watch]);
    if (!watch.loaded) throw watch.ready;
    return watch;
}

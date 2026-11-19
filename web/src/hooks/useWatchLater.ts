import { acquireWatch, retainWatch, subscribeWatch, Watch } from '../lib/watchStore';
import { useEffect, useSyncExternalStore } from 'react';

export function useWatchLater<T>(method: string, ...args: unknown[]): [Watch<T>, boolean] {
    const watch = acquireWatch<T>(method, args);
    useEffect(() => retainWatch(watch), [watch]);
    const loaded = useSyncExternalStore(
        (listener) => subscribeWatch(watch, listener),
        () => watch.loaded,
    );
    return [watch, loaded];
}

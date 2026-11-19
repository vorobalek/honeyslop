import { subscribeWatch, Watch } from '../lib/watchStore';
import { useSyncExternalStore } from 'react';

export function useWatched<T, S>(watch: Watch<T>, select: (value: T) => S): S {
    return useSyncExternalStore(
        (listener) => subscribeWatch(watch, listener),
        () => select(watch.value as T),
    );
}

export type WatchPresence = 'present' | 'missing' | 'failed';

export function useWatchPresence(watch: Watch<unknown>): WatchPresence {
    return useSyncExternalStore(
        (listener) => subscribeWatch(watch, listener),
        () => (watch.value !== undefined ? 'present' : watch.failed ? 'failed' : 'missing'),
    );
}

export function useWatchRevision(watch: Watch<unknown>): number {
    return useSyncExternalStore(
        (listener) => subscribeWatch(watch, listener),
        () => watch.revision,
    );
}

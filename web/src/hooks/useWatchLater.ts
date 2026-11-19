import { acquireWatch, retainWatch, subscribeWatch, Watch } from '../lib/watchStore';
import { WatchNameType, WatchRequestType, WatchViewType } from '../types/watchRequestType';
import { useEffect, useSyncExternalStore } from 'react';

export function useWatchLater<N extends WatchNameType>(
    request: WatchRequestType<N>,
): [Watch<WatchViewType<N>>, boolean] {
    const watch = acquireWatch<WatchViewType<N>>(request);
    useEffect(() => retainWatch(watch), [watch]);
    const loaded = useSyncExternalStore(
        (listener) => subscribeWatch(watch, listener),
        () => watch.loaded,
    );
    return [watch, loaded];
}

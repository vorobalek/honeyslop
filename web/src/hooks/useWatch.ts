import { acquireWatch, retainWatch, Watch } from '../lib/watchStore';
import { WatchNameType, WatchRequestType, WatchViewType } from '../types/watchRequestType';
import { useEffect } from 'react';

export function useWatch<N extends WatchNameType>(request: WatchRequestType<N>): Watch<WatchViewType<N>> {
    const watch = acquireWatch<WatchViewType<N>>(request);
    useEffect(() => retainWatch(watch), [watch]);
    if (!watch.loaded) throw watch.ready;
    return watch;
}

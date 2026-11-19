import { isAnyWatchPending, subscribePending } from '../lib/watchStore';
import { useSyncExternalStore } from 'react';

export function useBusy(): boolean {
    return useSyncExternalStore(subscribePending, isAnyWatchPending);
}

import { getLiveStatus, subscribeLiveStatus } from '../lib/hubConnection';
import { LiveStatusType } from '../types/liveStatusType';
import { useSyncExternalStore } from 'react';

export function useLiveStatus(): LiveStatusType {
    return useSyncExternalStore(subscribeLiveStatus, getLiveStatus);
}

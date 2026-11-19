import { getNotice, subscribeNotice } from '../lib/noticeStore';
import { useSyncExternalStore } from 'react';

export function useNotice(): string | null {
    return useSyncExternalStore(subscribeNotice, getNotice);
}

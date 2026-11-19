import { getSession, subscribeSession, waitForSession } from '../lib/hubConnection';
import { SessionType } from '../types/sessionType';
import { useSyncExternalStore } from 'react';

export function useSession(): SessionType {
    const session = useSyncExternalStore(subscribeSession, getSession);
    if (session === undefined) throw waitForSession();
    return session;
}

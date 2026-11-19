import { clearNotice } from './noticeStore';
import { startTransition } from 'react';

export type Location = {
    path: string;
    search: string;
};

type Listener = (location: Location) => void;

let listener: Listener = () => {};
let scrollOnCommit = false;

export function currentLocation(): Location {
    return { path: window.location.pathname, search: window.location.search };
}

export function listenToLocation(next: Listener): () => void {
    listener = next;
    const onPop = () => {
        clearNotice();
        startTransition(() => listener(currentLocation()));
    };
    window.addEventListener('popstate', onPop);
    return () => window.removeEventListener('popstate', onPop);
}

export function navigate(to: string, options: { replace?: boolean; keepNotice?: boolean } = {}): void {
    if (!options.keepNotice) clearNotice();
    if (options.replace) {
        window.history.replaceState(null, '', to);
    } else {
        window.history.pushState(null, '', to);
        scrollOnCommit = true;
    }
    startTransition(() => listener(currentLocation()));
}

export function takeScrollRequest(): boolean {
    const requested = scrollOnCommit;
    scrollOnCommit = false;
    return requested;
}

export function signInHref(location: Location): string {
    return `/signin?returnUrl=${encodeURIComponent(location.path + location.search)}`;
}

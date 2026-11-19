import { isConnected, onHubEvent, onOnline, sendToHub } from './hubConnection';
import { replaceEqualDeep } from './structuralSharing';

const RELEASE_DELAY = 1000;
const UNUSED_DELAY = 30_000;

type Listener = () => void;

export type Watch<T> = {
    readonly key: string;
    readonly method: string;
    readonly args: readonly unknown[];
    readonly id: number;
    readonly ready: Promise<void>;
    value: T | undefined;
    loaded: boolean;
    failed: boolean;
    revision: number;
};

type Entry = Watch<unknown> & {
    settle: () => void;
    listeners: Set<Listener>;
    refs: number;
    releaseTimer: number;
};

const watches = new Map<string, Entry>();
const byId = new Map<number, Entry>();
const latestByMethod = new Map<string, unknown>();
const pendingListeners = new Set<Listener>();
let nextId = 1;

function notifyPending(): void {
    for (const listener of pendingListeners) listener();
}

function subscribe(entry: Entry): void {
    if (!document.hidden) sendToHub(entry.method, entry.id, ...entry.args);
}

function settle(entry: Entry, apply: (entry: Entry) => void): void {
    apply(entry);
    const first = !entry.loaded;
    entry.loaded = true;
    entry.revision += 1;
    entry.settle();
    if (first) notifyPending();
    for (const listener of entry.listeners) listener();
}

function scheduleRelease(entry: Entry, delay: number): void {
    window.clearTimeout(entry.releaseTimer);
    entry.releaseTimer = window.setTimeout(() => {
        if (entry.refs > 0 || watches.get(entry.key) !== entry) return;
        watches.delete(entry.key);
        byId.delete(entry.id);
        sendToHub('unwatch', entry.id);
        notifyPending();
    }, delay);
}

onHubEvent('update', (id: number, value: unknown) => {
    const entry = byId.get(id);
    if (entry === undefined) return;
    settle(entry, (target) => {
        target.value = replaceEqualDeep(target.value ?? latestByMethod.get(target.method), value);
        target.failed = false;
        latestByMethod.set(target.method, target.value);
    });
});

onHubEvent('missing', (id: number) => {
    const entry = byId.get(id);
    if (entry === undefined) return;
    settle(entry, (target) => {
        target.value = undefined;
        target.failed = false;
    });
});

onHubEvent('failed', (id: number) => {
    const entry = byId.get(id);
    if (entry === undefined) return;
    settle(entry, (target) => {
        target.failed = true;
    });
});

onHubEvent('denied', () => window.location.reload());

onOnline(() => {
    for (const entry of watches.values()) subscribe(entry);
});

document.addEventListener('visibilitychange', () => {
    if (!isConnected()) return;
    for (const entry of watches.values()) {
        if (document.hidden) sendToHub('unwatch', entry.id);
        else subscribe(entry);
    }
});

export function acquireWatch<T>(method: string, args: readonly unknown[]): Watch<T> {
    const key = `${method}:${JSON.stringify(args)}`;
    let entry = watches.get(key);
    if (entry === undefined) {
        let resolve: () => void = () => {};
        const ready = new Promise<void>((done) => (resolve = done));
        entry = {
            key,
            method,
            args,
            id: nextId++,
            ready,
            value: undefined,
            loaded: false,
            failed: false,
            revision: 0,
            settle: resolve,
            listeners: new Set(),
            refs: 0,
            releaseTimer: 0,
        };
        watches.set(key, entry);
        byId.set(entry.id, entry);
        scheduleRelease(entry, UNUSED_DELAY);
        notifyPending();
        if (isConnected()) subscribe(entry);
    }
    return entry as Watch<T>;
}

export function retainWatch(watch: Watch<unknown>): () => void {
    const entry = watch as Entry;
    entry.refs += 1;
    window.clearTimeout(entry.releaseTimer);
    return () => {
        entry.refs -= 1;
        if (entry.refs === 0) scheduleRelease(entry, RELEASE_DELAY);
    };
}

export function refreshWatch(watch: Watch<unknown>): void {
    subscribe(watch as Entry);
}

export function subscribeWatch(watch: Watch<unknown>, listener: Listener): () => void {
    const entry = watch as Entry;
    entry.listeners.add(listener);
    return () => {
        entry.listeners.delete(listener);
    };
}

export function subscribePending(listener: Listener): () => void {
    pendingListeners.add(listener);
    return () => {
        pendingListeners.delete(listener);
    };
}

export function isAnyWatchPending(): boolean {
    for (const entry of watches.values()) if (!entry.loaded) return true;
    return false;
}

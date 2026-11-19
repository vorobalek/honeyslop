type Listener = () => void;

const listeners = new Set<Listener>();
let notice: string | null = null;

function set(next: string | null): void {
    if (notice === next) return;
    notice = next;
    for (const listener of listeners) listener();
}

export function postNotice(message: string): void {
    set(message);
}

export function clearNotice(): void {
    set(null);
}

export function subscribeNotice(listener: Listener): () => void {
    listeners.add(listener);
    return () => {
        listeners.delete(listener);
    };
}

export function getNotice(): string | null {
    return notice;
}

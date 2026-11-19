type Listener = () => void;

export type SelectionStore = {
    subscribe(listener: Listener): () => void;
    has(id: number): boolean;
    size(): number;
    ids(): number[];
    toggle(id: number, selected: boolean): void;
    replace(ids: readonly number[]): void;
};

export function createSelectionStore(): SelectionStore {
    let selected = new Set<number>();
    const listeners = new Set<Listener>();

    function commit(next: Set<number>): void {
        selected = next;
        for (const listener of listeners) listener();
    }

    return {
        subscribe(listener) {
            listeners.add(listener);
            return () => {
                listeners.delete(listener);
            };
        },
        has: (id) => selected.has(id),
        size: () => selected.size,
        ids: () => [...selected],
        toggle(id, on) {
            if (selected.has(id) === on) return;
            const next = new Set(selected);
            if (on) next.add(id);
            else next.delete(id);
            commit(next);
        },
        replace(ids) {
            if (ids.length === 0 && selected.size === 0) return;
            commit(new Set(ids));
        },
    };
}

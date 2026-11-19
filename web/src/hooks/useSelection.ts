import { SelectionStore } from '../lib/selectionStore';
import { useSyncExternalStore } from 'react';

export function useSelected(selection: SelectionStore, id: number): boolean {
    return useSyncExternalStore(selection.subscribe, () => selection.has(id));
}

export function useSelectionSize(selection: SelectionStore): number {
    return useSyncExternalStore(selection.subscribe, selection.size);
}

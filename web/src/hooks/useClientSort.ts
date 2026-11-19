import { SortDirectionType } from '../types/sortDirectionType';
import { useCallback, useMemo, useState } from 'react';

export type SortableColumn<T, C = undefined> = {
    label: string;
    className?: string;
    value: (row: T, context: C) => string;
};

export type ClientSortState = {
    index: number;
    direction: SortDirectionType;
} | null;

const UNIT_NUMBER = /^-?[\d,]+(\.\d+)?(\s?[a-zA-Z/%]+)?$/;

function sortRows<T, C>(
    rows: readonly T[],
    column: SortableColumn<T, C>,
    context: C,
    direction: SortDirectionType,
): T[] {
    const values = new Map(
        rows.map((row) => {
            const raw = column.value(row, context).trim();
            return [row, raw === '—' ? '' : raw];
        }),
    );
    const numeric = [...values.values()].every((value) => value === '' || UNIT_NUMBER.test(value));
    const number = (value: string) => Number.parseFloat(value.replace(/,/g, ''));
    return [...rows].sort((a, b) => {
        const x = values.get(a) ?? '';
        const y = values.get(b) ?? '';
        if (x === '' || y === '') return x === y ? 0 : x === '' ? 1 : -1;
        const result = numeric ? number(x) - number(y) : x.toLowerCase().localeCompare(y.toLowerCase());
        return direction === 'Asc' ? result : -result;
    });
}

export function useClientSort<T, C = undefined>(
    rows: readonly T[],
    columns: readonly SortableColumn<T, C>[],
    context: C,
): [readonly T[], ClientSortState, (index: number) => void] {
    const [state, setState] = useState<ClientSortState>(null);
    const sorted = useMemo(
        () => (state === null ? rows : sortRows(rows, columns[state.index], context, state.direction)),
        [rows, columns, context, state],
    );
    const toggle = useCallback(
        (index: number) =>
            setState((previous) => {
                if (previous === null || previous.index !== index) return { index, direction: 'Asc' };
                return previous.direction === 'Asc' ? { index, direction: 'Desc' } : null;
            }),
        [],
    );
    return [sorted, state, toggle];
}

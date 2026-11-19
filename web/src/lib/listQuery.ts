import { SortDirectionType } from '../types/sortDirectionType';
import { SortingType } from '../types/sortingType';
import { currentLocation } from './navigation';

export type QueryValues = Record<string, string | number | null | undefined>;

export function href(path: string, values: QueryValues): string {
    const query = new URLSearchParams();
    for (const [key, value] of Object.entries(values)) {
        if (value !== null && value !== undefined && value !== '') query.set(key, String(value));
    }
    const search = query.toString();
    return search ? `${path}?${search}` : path;
}

export function textParam(query: URLSearchParams, name: string): string {
    return query.get(name) ?? '';
}

export function oneOf<T extends string>(query: URLSearchParams, name: string, values: readonly T[]): T | null {
    const value = query.get(name);
    return values.includes(value as T) ? (value as T) : null;
}

export function numberParam(query: URLSearchParams, name: string): number | null {
    const value = Number.parseInt(query.get(name) ?? '', 10);
    return Number.isFinite(value) ? value : null;
}

export function pageParam(query: URLSearchParams): number {
    const page = numberParam(query, 'p');
    return page !== null && page > 0 ? page : 0;
}

export function sortingParam<TColumn extends string>(
    query: URLSearchParams,
    columns: readonly TColumn[],
): SortingType<TColumn> | null {
    const column = oneOf(query, 'Sort', columns);
    if (column === null) return null;
    return { column, direction: query.get('Dir') === 'Desc' ? 'Desc' : 'Asc' };
}

export function listValues(filter: QueryValues, sorting: SortingType<string> | null, page: number): QueryValues {
    return { ...filter, Sort: sorting?.column, Dir: sorting?.direction, p: page > 0 ? page : null };
}

export function sortHref(path: string, filter: QueryValues, column: string, current: SortDirectionType | null): string {
    if (current === 'Desc') return href(path, filter);
    return href(path, { ...filter, Sort: column, Dir: current === null ? 'Asc' : 'Desc' });
}

export function directionOf(sorting: SortingType<string> | null, column: string): SortDirectionType | null {
    return sorting?.column === column ? sorting.direction : null;
}

export function currentSorting(): QueryValues {
    const query = new URLSearchParams(currentLocation().search);
    return { Sort: query.get('Sort'), Dir: query.get('Dir') };
}

export function pagerLabels(sorted: boolean, newer: string): { previous: string; next: string } {
    return sorted ? { previous: '← Previous', next: 'Next →' } : { previous: newer, next: 'Older →' };
}

export function firstPageHref(): string {
    const { path, search } = currentLocation();
    const query = new URLSearchParams(search);
    query.delete('p');
    const rest = query.toString();
    return rest ? `${path}?${rest}` : path;
}

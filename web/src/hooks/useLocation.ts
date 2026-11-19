import { PathContext, SearchContext } from '../components/Router/locationContexts';
import { useContext, useMemo } from 'react';

export function usePath(): string {
    return useContext(PathContext);
}

export function useSearch(): string {
    return useContext(SearchContext);
}

export function useQuery(): URLSearchParams {
    const search = useSearch();
    return useMemo(() => new URLSearchParams(search), [search]);
}

import { useWatched } from '../../hooks/useWatched';
import { href, listValues, pagerLabels, QueryValues } from '../../lib/listQuery';
import { Watch } from '../../lib/watchStore';
import { PagedType } from '../../types/pagedType';
import { SortingType } from '../../types/sortingType';
import Link from './Link';
import React from 'react';

type ListPagerProps<T> = {
    watch: Watch<T>;
    select: (view: T) => PagedType<unknown>;
    path: string;
    filter: QueryValues;
    sorting: SortingType<string> | null;
    newer: string;
};

function ListPager<T>({ watch, select, path, filter, sorting, newer }: ListPagerProps<T>) {
    const page = useWatched(watch, (view) => select(view).page);
    const pageCount = useWatched(watch, (view) => select(view).pageCount);
    const { previous, next } = pagerLabels(sorting !== null, newer);
    return (
        <nav className='pager'>
            {page > 0 && <Link to={href(path, listValues(filter, sorting, page - 1))}>{previous}</Link>}
            <span>{`Page ${page + 1} of ${pageCount}`}</span>
            {page + 1 < pageCount && <Link to={href(path, listValues(filter, sorting, page + 1))}>{next}</Link>}
        </nav>
    );
}

export default React.memo(ListPager) as typeof ListPager;

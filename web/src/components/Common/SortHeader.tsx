import { QueryValues, sortHref } from '../../lib/listQuery';
import { SortDirectionType } from '../../types/sortDirectionType';
import Link from './Link';
import React from 'react';

type SortHeaderProps = {
    path: string;
    filter: QueryValues;
    column: string;
    label: string;
    current: SortDirectionType | null;
    className?: string;
};

const SortHeader: React.FC<SortHeaderProps> = ({ path, filter, column, label, current, className }) => {
    const state = current === 'Asc' ? 'sorted-asc' : current === 'Desc' ? 'sorted-desc' : '';
    const aria = current === 'Asc' ? 'ascending' : current === 'Desc' ? 'descending' : 'none';
    return (
        <th
            className={`sortable ${className ?? ''} ${state}`}
            aria-sort={aria}
        >
            <Link to={sortHref(path, filter, column, current)}>{label}</Link>
        </th>
    );
};

export default React.memo(SortHeader);

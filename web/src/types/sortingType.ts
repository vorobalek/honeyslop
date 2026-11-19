import { SortDirectionType } from './sortDirectionType';

export interface SortingType<TColumn> {
    column: TColumn;
    direction: SortDirectionType;
}

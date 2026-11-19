export interface PagedType<T> {
    items: T[];
    total: number;
    page: number;
    pageSize: number;
    pageCount: number;
}

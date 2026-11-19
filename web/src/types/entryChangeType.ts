export interface EntryChangeType {
    errors: Record<string, string>;
    ids: number[];
    changed: boolean;
}

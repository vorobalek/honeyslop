export interface ContactsChangeType {
    errors: Record<string, string>;
    changed: number;
    done: boolean;
}

export interface ClientDelayChangeType {
    errors: Record<string, string>;
    responseDelayMilliseconds: number | null;
    changed: boolean;
}

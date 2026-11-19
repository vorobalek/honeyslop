export interface EntryRequestType {
    method: string;
    host: string;
    target: string;
    headers: string | null;
    body: string | null;
}

import { ResponseHeaderType } from './responseHeaderType';

export interface EntryResponseType {
    status: number;
    headers: ResponseHeaderType[];
    body: string;
}

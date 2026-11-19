import { BlockedContactType } from './blockedContactType';
import { PagedType } from './pagedType';

export interface BlockedContactsViewType {
    contacts: PagedType<BlockedContactType>;
    asOf: string;
}

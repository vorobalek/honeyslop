import { LetterType } from './letterType';

export interface AbuseDraftType {
    letter: LetterType;
    senderName: string | null;
    fromAddress: string | null;
    archivedRequests: number;
    archiveFileName: string;
}

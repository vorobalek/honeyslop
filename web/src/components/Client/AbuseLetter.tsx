import { countOf } from '../../lib/numbers';
import { AbuseDraftType } from '../../types/abuseDraftType';
import CopyLetterButton from './CopyLetterButton';
import React from 'react';

type AbuseLetterProps = {
    ip: string;
    draft: AbuseDraftType;
    recipients: readonly string[];
};

const LETTER_ID = 'abuse-report';

const AbuseLetter: React.FC<AbuseLetterProps> = ({ ip, draft, recipients }) => (
    <>
        <details id='abuse-letter'>
            <summary>Letter</summary>
            <dl className='meta'>
                <dt>To</dt>
                <dd className='mono'>{recipients.length === 0 ? '—' : recipients.join(', ')}</dd>
                <dt>Subject</dt>
                <dd>{draft.letter.subject}</dd>
                <dt>Attachment</dt>
                <dd>
                    {`${draft.archiveFileName}: ${countOf(draft.archivedRequests, 'request')} from this address as received, oldest first`}
                </dd>
            </dl>
            <pre
                className='code'
                id={LETTER_ID}
            >
                {draft.letter.body}
            </pre>
        </details>
        <div className='actions'>
            <CopyLetterButton letterId={LETTER_ID} />
            <a
                className='button-link'
                href={`/clients/${encodeURIComponent(ip)}/archive`}
                download
            >
                Download archive
            </a>
        </div>
    </>
);

export default React.memo(AbuseLetter);

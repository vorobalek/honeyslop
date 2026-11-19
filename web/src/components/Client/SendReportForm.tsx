import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { SENDER_NAME_MAX_LENGTH } from '../../lib/constants';
import { invokeHub } from '../../lib/hubConnection';
import { postNotice } from '../../lib/noticeStore';
import { countOf } from '../../lib/numbers';
import { AbuseDraftType } from '../../types/abuseDraftType';
import { ReportOutcomeType } from '../../types/reportOutcomeType';
import { ReportRequestType } from '../../types/reportRequestType';
import React, { type FormEvent } from 'react';

type SendReportFormProps = {
    ip: string;
    draft: AbuseDraftType;
    recipients: readonly string[];
    followUp: boolean;
    newSinceLastSent: number;
};

const REFUSALS: Partial<Record<ReportOutcomeType, string>> = {
    AlreadyQueued: 'A report about this address is already on its way, so no second one was queued.',
    AlreadyReported:
        'This address was already reported and has sent nothing new since, so no second report was queued.',
    NoContact: 'The registry publishes no abuse contact for this network, so there is nobody to send the report to.',
};

function senderLine(name: string, address: string | null): string {
    if (address) return `From: ${name ? `"${name}" <${address}>` : address}`;
    return `From: ${name || 'the address alone, without a name'}`;
}

const SendReportForm: React.FC<SendReportFormProps> = ({ ip, draft, recipients, followUp, newSinceLastSent }) => {
    const to = recipients.join(', ');
    const label = followUp
        ? `Send a follow-up to ${to} (${countOf(newSinceLastSent, 'new request')})`
        : `Send to ${to}`;
    const send = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const senderName = String(new FormData(event.currentTarget).get('SenderName') ?? '');
        const confirmed = await confirmAction({
            question: `Send the abuse report about ${ip} to ${to}?`,
            detail: `It goes out right away with ${countOf(draft.archivedRequests, 'request')} from this address attached, and cannot be taken back.`,
            sender: senderLine(senderName.trim(), draft.fromAddress),
            button: 'Send report',
        });
        if (!confirmed) return;
        const request = await invokeHub<ReportRequestType>('report', ip, senderName);
        const refusal = REFUSALS[request.outcome];
        if (refusal) postNotice(refusal);
    };
    return (
        <form
            className='send-report'
            method='post'
            onSubmit={(event) => perform(() => send(event))}
        >
            <label>
                Sender name
                <input
                    type='text'
                    id='sender-name'
                    name='SenderName'
                    defaultValue={draft.senderName ?? ''}
                    maxLength={SENDER_NAME_MAX_LENGTH}
                    placeholder='the address only'
                />
            </label>
            <button
                type='submit'
                className='danger-filled'
            >
                {label}
            </button>
        </form>
    );
};

export default React.memo(SendReportForm);

import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { EntryChangeType } from '../../types/entryChangeType';
import { EntryType } from '../../types/entryType';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import Link from '../Common/Link';
import GuidanceFields from './GuidanceFields';
import RequestFields from './RequestFields';
import ResponseChangeTabs from './ResponseChangeTabs';
import ResponseFields from './ResponseFields';
import { changeConfirmation } from './changeConfirmation';
import { createConfirmation } from './createConfirmation';
import { readGuidance, readRequest, readResponse } from './responseChangeData';
import React, { useEffect, useRef, useState, type FormEvent } from 'react';

type ResponseChangeDialogProps = {
    ids: readonly number[];
    current: EntryType | null;
    creating?: boolean;
    onChanged: (mode: ResponseChangeModeType, ids: number[]) => void;
    onClose: () => void;
};

const GENERAL_ERROR = 'Entries';

const ResponseChangeDialog: React.FC<ResponseChangeDialogProps> = ({
    ids,
    current,
    creating = false,
    onChanged,
    onClose,
}) => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [mode, setMode] = useState<ResponseChangeModeType>('regenerate');
    const [errors, setErrors] = useState<Record<string, string>>({});
    const [existingId, setExistingId] = useState<number | null>(null);
    const [sending, setSending] = useState(false);

    useEffect(() => {
        if (dialog.current?.open === false) dialog.current.showModal();
    }, []);

    const change = async (data: FormData): Promise<EntryChangeType | null> => {
        const read = mode === 'write' ? readResponse(data) : null;
        if (read !== null && 'error' in read) {
            setErrors({ Headers: read.error });
            return null;
        }
        if (creating) {
            if (!(await confirmAction(createConfirmation(mode)))) return null;
            const request = readRequest(data);
            return read === null
                ? invokeHub<EntryChangeType>('createEntry', request, readGuidance(data))
                : invokeHub<EntryChangeType>('createEntryWithResponse', request, read.response);
        }
        if (!(await confirmAction(changeConfirmation(mode, ids.length)))) return null;
        return read === null
            ? invokeHub<EntryChangeType>('regenerateEntries', ids, readGuidance(data))
            : invokeHub<EntryChangeType>('replaceEntries', ids, read.response);
    };
    const send = async (data: FormData) => {
        setSending(true);
        try {
            const result = await change(data);
            if (result === null) return;
            if (!result.changed) {
                setErrors(result.errors);
                setExistingId(result.ids.length === 1 ? result.ids[0] : null);
                return;
            }
            dialog.current?.close();
            onChanged(mode, result.ids);
        } finally {
            setSending(false);
        }
    };
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        perform(
            () => send(data),
            (reason) => setErrors({ [GENERAL_ERROR]: reason }),
        );
    };

    const single = ids.length === 1;
    return (
        <dialog
            className='change-response'
            ref={dialog}
            onClose={onClose}
        >
            <form onSubmit={submit}>
                <p className='dialog-title'>
                    {creating
                        ? 'New entry'
                        : single
                          ? 'Change the response'
                          : `Change the response of ${ids.length} entries`}
                </p>
                {creating && (
                    <RequestFields
                        disabled={sending}
                        errors={errors}
                    />
                )}
                <ResponseChangeTabs
                    mode={mode}
                    creating={creating}
                    onSelect={setMode}
                />
                <fieldset disabled={sending}>
                    <GuidanceFields
                        active={mode === 'regenerate'}
                        guidance={current?.guidance ?? null}
                        error={errors.Guidance}
                    />
                    <ResponseFields
                        active={mode === 'write'}
                        current={current}
                        errors={errors}
                    />
                </fieldset>
                {errors[GENERAL_ERROR] && (
                    <p className='error'>
                        {errors[GENERAL_ERROR]}
                        {existingId !== null && (
                            <>
                                {' '}
                                <Link to={`/entries/${existingId}`}>Open it →</Link>
                            </>
                        )}
                    </p>
                )}
                <p className='hint'>
                    {creating
                        ? 'A new entry answers this request from now on.'
                        : single
                          ? current?.status === 'Expired'
                              ? 'A new entry answers this path from now on.'
                              : 'The entry expires and a new one takes its place.'
                          : 'The selected entries expire and new ones take their place. An expired entry is skipped when its path already has a newer one.'}
                </p>
                <div className='actions'>
                    <button
                        type='button'
                        onClick={() => dialog.current?.close()}
                    >
                        Cancel
                    </button>
                    <button
                        type='submit'
                        className='danger-filled'
                        disabled={sending}
                    >
                        {creating ? 'Create the entry' : mode === 'regenerate' ? 'Regenerate' : 'Replace the response'}
                    </button>
                </div>
            </form>
        </dialog>
    );
};

export default React.memo(ResponseChangeDialog);

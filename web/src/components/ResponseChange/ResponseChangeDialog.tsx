import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { EntryChangeType } from '../../types/entryChangeType';
import { EntryType } from '../../types/entryType';
import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import GuidanceFields from './GuidanceFields';
import ResponseChangeTabs from './ResponseChangeTabs';
import ResponseFields from './ResponseFields';
import { changeConfirmation } from './changeConfirmation';
import { readGuidance, readResponse } from './responseChangeData';
import React, { useEffect, useRef, useState, type FormEvent } from 'react';

type ResponseChangeDialogProps = {
    ids: readonly number[];
    current: EntryType | null;
    onChanged: (mode: ResponseChangeModeType, ids: number[]) => void;
    onClose: () => void;
};

const GENERAL_ERROR = 'Entries';

const ResponseChangeDialog: React.FC<ResponseChangeDialogProps> = ({ ids, current, onChanged, onClose }) => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [mode, setMode] = useState<ResponseChangeModeType>('regenerate');
    const [errors, setErrors] = useState<Record<string, string>>({});
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
                    {single ? 'Change the response' : `Change the response of ${ids.length} entries`}
                </p>
                <ResponseChangeTabs
                    mode={mode}
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
                {errors[GENERAL_ERROR] && <p className='error'>{errors[GENERAL_ERROR]}</p>}
                <p className='hint'>
                    {single
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
                        {mode === 'regenerate' ? 'Regenerate' : 'Replace the response'}
                    </button>
                </div>
            </form>
        </dialog>
    );
};

export default React.memo(ResponseChangeDialog);

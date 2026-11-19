import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { RESPONSE_DELAY_MAX_SECONDS } from '../../lib/constants';
import { invokeHub } from '../../lib/hubConnection';
import { postNotice } from '../../lib/noticeStore';
import { EntryChangeType } from '../../types/entryChangeType';
import React, { useEffect, useRef, useState, type FormEvent } from 'react';

type EntryDelayDialogProps = {
    id: number;
    seconds: number | null;
    fallback: number;
    onClose: () => void;
};

function delayNotice(seconds: number | null, fallback: number): string {
    return seconds === null
        ? `The entry waits the default ${fallback} s again.`
        : `The entry waits ${seconds} s before every response.`;
}

const EntryDelayDialog: React.FC<EntryDelayDialogProps> = ({ id, seconds, fallback, onClose }) => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [error, setError] = useState<string | null>(null);
    const [sending, setSending] = useState(false);

    useEffect(() => {
        if (dialog.current?.open === false) dialog.current.showModal();
    }, []);

    const send = async (value: number | null) => {
        const confirmed = await confirmAction({
            question: 'Change the delay of this entry?',
            detail: `The trap waits ${value ?? fallback} s before every response it serves from this entry.`,
            button: 'Change',
        });
        if (!confirmed) return;
        setSending(true);
        try {
            const result = await invokeHub<EntryChangeType>('setEntryDelay', [id], value);
            if (!result.changed) {
                setError(Object.values(result.errors)[0] ?? null);
                return;
            }
            dialog.current?.close();
            postNotice(delayNotice(value, fallback));
        } finally {
            setSending(false);
        }
    };
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const text = String(new FormData(event.currentTarget).get('Seconds') ?? '').trim();
        perform(
            () => send(text === '' ? null : Number(text)),
            (reason) => setError(reason),
        );
    };

    return (
        <dialog
            className='entry-delay'
            ref={dialog}
            onClose={onClose}
        >
            <form onSubmit={submit}>
                <p className='dialog-title'>Delay of this entry</p>
                <fieldset disabled={sending}>
                    <div className='setting'>
                        <label htmlFor='entry-delay'>Hold every response for, seconds</label>
                        <input
                            type='number'
                            id='entry-delay'
                            name='Seconds'
                            min='0'
                            max={RESPONSE_DELAY_MAX_SECONDS}
                            step='1'
                            placeholder={String(fallback)}
                            defaultValue={seconds ?? ''}
                        />
                        {error && <span className='error'>{error}</span>}
                        <p className='hint'>{`Empty uses the default, ${fallback} s now.`}</p>
                    </div>
                </fieldset>
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
                        Change the delay
                    </button>
                </div>
            </form>
        </dialog>
    );
};

export default React.memo(EntryDelayDialog);

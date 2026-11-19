import { perform } from '../../lib/actions';
import { confirmAction } from '../../lib/confirmation';
import { RESPONSE_DELAY_MAX_MILLISECONDS } from '../../lib/constants';
import { postNotice } from '../../lib/noticeStore';
import React, { useEffect, useRef, useState, type FormEvent } from 'react';

type DelayDialogProps = {
    title: string;
    question: string;
    milliseconds: number | null;
    placeholder: string;
    hint: string;
    detail: (milliseconds: number | null) => string;
    notice: (milliseconds: number | null) => string;
    save: (milliseconds: number | null) => Promise<string | null>;
    onClose: () => void;
};

const DelayDialog: React.FC<DelayDialogProps> = ({
    title,
    question,
    milliseconds,
    placeholder,
    hint,
    detail,
    notice,
    save,
    onClose,
}) => {
    const dialog = useRef<HTMLDialogElement>(null);
    const [error, setError] = useState<string | null>(null);
    const [sending, setSending] = useState(false);

    useEffect(() => {
        if (dialog.current?.open === false) dialog.current.showModal();
    }, []);

    const send = async (value: number | null) => {
        const confirmed = await confirmAction({ question, detail: detail(value), button: 'Change' });
        if (!confirmed) return;
        setSending(true);
        try {
            const refused = await save(value);
            if (refused !== null) {
                setError(refused);
                return;
            }
            dialog.current?.close();
            postNotice(notice(value));
        } finally {
            setSending(false);
        }
    };
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const text = String(new FormData(event.currentTarget).get('Milliseconds') ?? '').trim();
        perform(
            () => send(text === '' ? null : Number(text)),
            (reason) => setError(reason),
        );
    };

    return (
        <dialog
            className='delay-dialog'
            ref={dialog}
            onClose={onClose}
        >
            <form onSubmit={submit}>
                <p className='dialog-title'>{title}</p>
                <fieldset disabled={sending}>
                    <div className='setting'>
                        <label htmlFor='delay-milliseconds'>
                            Answer no sooner than, milliseconds after the request
                        </label>
                        <input
                            type='number'
                            id='delay-milliseconds'
                            name='Milliseconds'
                            min='0'
                            max={RESPONSE_DELAY_MAX_MILLISECONDS}
                            step='1'
                            placeholder={placeholder}
                            defaultValue={milliseconds ?? ''}
                        />
                        {error && <span className='error'>{error}</span>}
                        <p className='hint'>{hint}</p>
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

export default React.memo(DelayDialog);

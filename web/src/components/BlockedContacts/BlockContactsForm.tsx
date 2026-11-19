import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { blockContacts } from '../../lib/blockedContacts';
import React, { useState, type FormEvent } from 'react';

const NOTE_MAX_LENGTH = 256;

const BlockContactsForm: React.FC = () => {
    const { canOperate } = useSession();
    const [error, setError] = useState<string | null>(null);
    if (!canOperate) return null;
    const block = async (form: HTMLFormElement) => {
        const data = new FormData(form);
        const contacts = String(data.get('Contacts') ?? '')
            .split(/[\n,;]/)
            .map((x) => x.trim())
            .filter((x) => x.length > 0);
        const note = String(data.get('Note') ?? '').trim();
        if (contacts.length === 0) {
            setError('Name at least one address or @domain.');
            return;
        }
        const change = await blockContacts(contacts, note === '' ? null : note);
        if (change === null) return;
        if (change.done) {
            setError(null);
            form.reset();
        } else setError(Object.values(change.errors)[0] ?? null);
    };
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const form = event.currentTarget;
        perform(() => block(form));
    };
    return (
        <form
            className='block-contacts'
            onSubmit={submit}
        >
            <div className='setting'>
                <label htmlFor='block-contacts'>Block abuse contacts</label>
                <textarea
                    id='block-contacts'
                    name='Contacts'
                    className='mono'
                    rows={3}
                    spellCheck={false}
                    placeholder={'abuse@hoster.example\n@hoster.example'}
                />
                {error && <span className='error'>{error}</span>}
                <p className='hint'>
                    One address or @domain per line. Reports never go to them, the ones already queued included.
                </p>
            </div>
            <div className='setting'>
                <label htmlFor='block-note'>Why</label>
                <input
                    type='text'
                    id='block-note'
                    name='Note'
                    maxLength={NOTE_MAX_LENGTH}
                    placeholder='Answers reports with bounces'
                />
            </div>
            <button
                type='submit'
                className='danger'
            >
                Block
            </button>
        </form>
    );
};

export default React.memo(BlockContactsForm);

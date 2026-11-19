import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { SENDER_NAME_MAX_LENGTH } from '../../lib/constants';
import { invokeHub } from '../../lib/hubConnection';
import { SettingsViewType } from '../../types/settingsViewType';
import React, { type FormEvent } from 'react';

type PersonalSettingsFormProps = {
    view: SettingsViewType;
    onSaved: () => void;
};

const PersonalSettingsForm: React.FC<PersonalSettingsFormProps> = ({ view, onSaved }) => {
    const session = useSession();
    const fallback = view.defaultSenderName === null ? 'the address alone' : `"${view.defaultSenderName}"`;
    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const name = String(new FormData(event.currentTarget).get('PersonalSenderName') ?? '');
        await invokeHub<string | null>('savePersonal', name);
        onSaved();
    };
    return (
        <form
            method='post'
            onSubmit={(event) => perform(() => save(event))}
        >
            <fieldset className='settings'>
                <h2>
                    {'Personal '}
                    <span className='hint'>{`only for ${session.name}`}</span>
                </h2>
                <div className='setting'>
                    <label htmlFor='PersonalSenderName'>Sign abuse reports as</label>
                    <input
                        type='text'
                        id='PersonalSenderName'
                        name='PersonalSenderName'
                        maxLength={SENDER_NAME_MAX_LENGTH}
                        placeholder={view.defaultSenderName ?? 'the address only'}
                        defaultValue={view.personalSenderName ?? ''}
                    />
                    <p className='hint'>
                        {`The sender name on the reports you send; it never goes into the letter itself. Empty uses ${fallback}.`}
                    </p>
                </div>
                <button type='submit'>Save personal settings</button>
            </fieldset>
        </form>
    );
};

export default React.memo(PersonalSettingsForm);

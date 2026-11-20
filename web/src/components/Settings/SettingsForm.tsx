import { useSession } from '../../hooks/useSession';
import { perform } from '../../lib/actions';
import { ConfirmationRequest, confirmAction } from '../../lib/confirmation';
import { invokeHub } from '../../lib/hubConnection';
import { timeSpanSeconds } from '../../lib/time';
import { MaintenanceType } from '../../types/maintenanceType';
import { SettingsSaveType } from '../../types/settingsSaveType';
import { SettingsViewType } from '../../types/settingsViewType';
import AutoDelayRange from './AutoDelayRange';
import AutoDelayResets from './AutoDelayResets';
import CheckboxSetting from './CheckboxSetting';
import DurationSetting from './DurationSetting';
import ResponseDelaySetting from './ResponseDelaySetting';
import UserAgentSetting from './UserAgentSetting';
import { FIELD_OF_ERROR, readSettingsForm } from './settingsFormData';
import React, { useState, type FormEvent } from 'react';

const SAVE_CONFIRMATION: ConfirmationRequest = {
    question: 'Save the settings for everyone?',
    detail: 'A shorter cache lifetime or history retention expires entries and deletes requests at the next maintenance run.',
    button: 'Save',
};

type SettingsFormProps = {
    view: SettingsViewType;
    onSaved: (maintenance: MaintenanceType) => void;
};

const SettingsForm: React.FC<SettingsFormProps> = ({ view, onSaved }) => {
    const session = useSession();
    const [errors, setErrors] = useState<Record<string, string>>({});
    const settings = view.settings;
    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const form = readSettingsForm(event.currentTarget);
        if (!(await confirmAction(SAVE_CONFIRMATION))) return;
        const result = await invokeHub<SettingsSaveType>('saveSettings', form);
        if (result.maintenance !== null) {
            onSaved(result.maintenance);
            return;
        }
        const next: Record<string, string> = {};
        for (const [field, message] of Object.entries(result.errors)) next[FIELD_OF_ERROR[field] ?? field] = message;
        setErrors(next);
    };
    return (
        <form
            method='post'
            onSubmit={(event) => perform(() => save(event))}
        >
            <fieldset
                className='settings'
                disabled={!session.canOperate}
            >
                <h2>Cache</h2>
                <DurationSetting
                    name='CacheTtl'
                    label='Expire generated responses after'
                    seconds={timeSpanSeconds(settings.cacheTtl)}
                    error={errors.CacheTtl}
                >
                    A response older than this expires and is generated again on the next request. 0 keeps responses
                    forever.
                </DurationSetting>
                <DurationSetting
                    name='KeepUnused'
                    label='Expire entries nobody requested for'
                    seconds={timeSpanSeconds(settings.keepUnused)}
                    error={errors.KeepUnused}
                >
                    Counts from the last time the response was served, or from its creation if it never was. 0 keeps
                    them forever.
                </DurationSetting>
                <ResponseDelaySetting
                    milliseconds={settings.responseDelayMilliseconds}
                    fallback={view.defaultResponseDelayMilliseconds}
                    error={errors.ResponseDelay}
                />

                <h2>Generation</h2>
                <DurationSetting
                    name='GenerationWait'
                    label='Hold a request open for its new response up to'
                    seconds={timeSpanSeconds(settings.generationWait)}
                    error={errors.GenerationWait}
                >
                    A client asking for a path nobody asked before waits this long for the model, then gets a plain 404
                    while the response keeps generating. Clients that give up earlier show up as abandoned.
                </DurationSetting>

                <h2>Timeouts</h2>
                <DurationSetting
                    name='TimeLimit'
                    label='Give one generation at most'
                    seconds={timeSpanSeconds(settings.generationTimeLimit)}
                    error={errors.TimeLimit}
                >
                    Counted from the moment a model server slot takes the job, waiting for a free slot is not included.
                    A generation that runs longer counts as a failed attempt.
                </DurationSetting>
                <DurationSetting
                    name='ClaimTimeout'
                    label='Treat a generation as abandoned after silence of'
                    seconds={timeSpanSeconds(settings.claimTimeout)}
                    error={errors.ClaimTimeout}
                >
                    A trap instance working on an entry confirms it every quarter of this time. When the confirmations
                    stop, because the instance died, another one takes the entry over.
                </DurationSetting>
                <DurationSetting
                    name='RetryFailed'
                    label='Retry a failed generation after'
                    seconds={timeSpanSeconds(settings.retryFailedAfter)}
                    error={errors.RetryFailed}
                >
                    Only broken model output counts as a failure, up to three attempts per path. A model server that is
                    down or restarting does not cost an attempt: the entry goes straight back to the queue.
                </DurationSetting>

                <h2>History</h2>
                <DurationSetting
                    name='RequestRetention'
                    label='Keep the request history for'
                    seconds={timeSpanSeconds(settings.requestRetention)}
                    error={errors.RequestRetention}
                >
                    Every request the trap answers is recorded with its headers and body. 0 keeps the history forever,
                    which grows by roughly 2 GB a month at the current scanner traffic. An expired entry stays viewable
                    while any recorded request refers to it.
                </DurationSetting>
                <CheckboxSetting
                    name='AllowRefusals'
                    label='Let the model refuse'
                    checked={settings.allowRefusals}
                >
                    On: the model may answer with a login wall, 401, 403, a redirect or a 404 when that is what real
                    software would do. Off: full anarchy, every path gets status 200 and invented content, and every
                    login succeeds. Applies to new generations; expire everything to regenerate what is cached.
                </CheckboxSetting>
                <CheckboxSetting
                    name='GenerationEnabled'
                    label='Generate responses for new requests'
                    checked={settings.generationEnabled}
                >
                    When off, cached responses are still served and every new path gets a plain 404 without touching the
                    model.
                </CheckboxSetting>

                <h2>Access</h2>
                <CheckboxSetting
                    name='PublicView'
                    label='Show everything read-only without signing in'
                    checked={settings.publicView}
                >
                    {`On: anyone can open this admin without signing in and see all of it, the request history with headers, bodies and client addresses included. Every change still takes signing in. Off: every page asks to sign in.${session.signInEnabled ? '' : ' Sign-in is not configured, so this has no effect: the admin relies on the proxy in front of it.'}`}
                </CheckboxSetting>

                <h2>Outbound requests</h2>
                <UserAgentSetting
                    value={settings.userAgent}
                    fallback={view.defaultUserAgent}
                    error={errors.UserAgent}
                />

                <h2>Tricks</h2>
                <CheckboxSetting
                    name='AutoDelayEnabled'
                    label='Auto-Delay: learn how long each client waits'
                    checked={settings.autoDelayEnabled}
                >
                    The trap learns how long each client waits before dropping the connection and holds it just below
                    that, trickling the response so its last byte leaves at that moment. A delay set by hand wins. Off:
                    learned delays are kept but not used.
                </CheckboxSetting>
                <AutoDelayRange
                    min={settings.autoDelayMinMilliseconds}
                    max={settings.autoDelayMaxMilliseconds}
                    minError={errors.AutoDelayMin}
                    maxError={errors.AutoDelayMax}
                />
                <AutoDelayResets />
                <CheckboxSetting
                    name='NoiseEnabled'
                    label='Enable noise defence'
                    checked={settings.noiseEnabled}
                >
                    Addresses and entries given a noise tier get 10, 20 or 40 MB of zero bytes instead of any response,
                    with nothing generated. The tier of an address wins over the tier of an entry. Off: tiers are kept
                    but not used.
                </CheckboxSetting>

                <button
                    type='submit'
                    className='danger'
                >
                    Save
                </button>
            </fieldset>
        </form>
    );
};

export default React.memo(SettingsForm);

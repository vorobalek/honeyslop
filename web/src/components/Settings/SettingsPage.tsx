import { useSession } from '../../hooks/useSession';
import { useTitle } from '../../hooks/useTitle';
import { useWatch } from '../../hooks/useWatch';
import { useWatched, useWatchPresence, useWatchRevision } from '../../hooks/useWatched';
import { refreshWatch } from '../../lib/watchStore';
import { MaintenanceType } from '../../types/maintenanceType';
import { SettingsViewType } from '../../types/settingsViewType';
import PageNotice from '../Common/PageNotice';
import Presence from '../Common/Presence';
import PersonalSettingsForm from './PersonalSettingsForm';
import ReadOnlyNotice from './ReadOnlyNotice';
import SavedNotice from './SavedNotice';
import SettingsForm from './SettingsForm';
import React, { useCallback, useState } from 'react';

type Saved = { kind: 'settings'; maintenance: MaintenanceType } | { kind: 'personal' } | null;

const SettingsPage: React.FC = () => {
    useTitle('Settings');
    const session = useSession();
    const watch = useWatch<SettingsViewType>('watchSettings');
    const presence = useWatchPresence(watch);
    const revision = useWatchRevision(watch);
    const view = useWatched(watch, (value) => value);
    const [saved, setSaved] = useState<Saved>(null);
    const onSettingsSaved = useCallback(
        (maintenance: MaintenanceType) => {
            setSaved({ kind: 'settings', maintenance });
            refreshWatch(watch);
        },
        [watch],
    );
    const onPersonalSaved = useCallback(() => {
        setSaved({ kind: 'personal' });
        refreshWatch(watch);
    }, [watch]);

    if (presence !== 'present') return <Presence presence={presence} />;
    return (
        <>
            <h1>Settings</h1>
            <PageNotice />
            {saved?.kind === 'settings' && <SavedNotice maintenance={saved.maintenance} />}
            {saved?.kind === 'personal' && <p className='notice'>Saved your personal settings.</p>}
            {!session.canOperate && <ReadOnlyNotice />}
            {session.userId !== null && (
                <PersonalSettingsForm
                    key={`personal-${revision}`}
                    view={view}
                    onSaved={onPersonalSaved}
                />
            )}
            <SettingsForm
                key={`settings-${revision}`}
                view={view}
                onSaved={onSettingsSaved}
            />
        </>
    );
};

export default React.memo(SettingsPage);

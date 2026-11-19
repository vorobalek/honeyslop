import { usePath } from '../../hooks/useLocation';
import BlockedContactsPage from '../BlockedContacts/BlockedContactsPage';
import ClientPage from '../Client/ClientPage';
import ClientsPage from '../Clients/ClientsPage';
import NotFound from '../Common/NotFound';
import EntriesPage from '../Entries/EntriesPage';
import EntryPage from '../Entry/EntryPage';
import GeneratorsPage from '../Generators/GeneratorsPage';
import ReportPage from '../Report/ReportPage';
import ReportsPage from '../Reports/ReportsPage';
import RequestPage from '../Request/RequestPage';
import RequestsPage from '../Requests/RequestsPage';
import SettingsPage from '../Settings/SettingsPage';
import React from 'react';

const ENTRY = /^\/entries\/(\d+)$/;
const REQUEST = /^\/requests\/(\d+)$/;
const CLIENT = /^\/clients\/([^/]+)$/;
const REPORT = /^\/reports\/(\d+)$/;

const Routes: React.FC = () => {
    const path = usePath();
    let match: RegExpExecArray | null;

    if (path === '/') return <EntriesPage />;
    if ((match = ENTRY.exec(path)))
        return (
            <EntryPage
                key={match[1]}
                id={Number(match[1])}
            />
        );
    if (path === '/requests') return <RequestsPage />;
    if ((match = REQUEST.exec(path)))
        return (
            <RequestPage
                key={match[1]}
                id={Number(match[1])}
            />
        );
    if (path === '/clients') return <ClientsPage />;
    if ((match = CLIENT.exec(path))) {
        const ip = decodeURIComponent(match[1]);
        return (
            <ClientPage
                key={ip}
                ip={ip}
            />
        );
    }
    if (path === '/reports') return <ReportsPage />;
    if (path === '/reports/blocked') return <BlockedContactsPage />;
    if ((match = REPORT.exec(path)))
        return (
            <ReportPage
                key={match[1]}
                id={Number(match[1])}
            />
        );
    if (path === '/generators') return <GeneratorsPage />;
    if (path === '/settings') return <SettingsPage />;
    return <NotFound />;
};

export default React.memo(Routes);

import { MaintenanceType } from '../../types/maintenanceType';
import React from 'react';

type SavedNoticeProps = {
    maintenance: MaintenanceType;
};

const SavedNotice: React.FC<SavedNoticeProps> = ({ maintenance }) => (
    <p className='notice'>
        {`Saved and applied to everything already stored: expired ${maintenance.expired} entries past their time to live and ${maintenance.unused} unused ones, pruned ${maintenance.pruned} request records, deleted ${maintenance.deleted} expired entries no request refers to. Trap instances pick the change up within ten seconds and may serve an expired response from memory for up to a minute.`}
    </p>
);

export default React.memo(SavedNotice);

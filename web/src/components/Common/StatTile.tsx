import React from 'react';

type StatTileProps = {
    value: number | string;
    label: string;
};

const StatTile: React.FC<StatTileProps> = ({ value, label }) => (
    <div className='stat'>
        <span className='stat-value'>{value}</span>
        <span className='stat-label'>{label}</span>
    </div>
);

export default React.memo(StatTile);

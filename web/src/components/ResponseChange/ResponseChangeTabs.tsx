import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import React from 'react';

type ResponseChangeTabsProps = {
    mode: ResponseChangeModeType;
    onSelect: (mode: ResponseChangeModeType) => void;
};

const TABS: readonly { mode: ResponseChangeModeType; label: string }[] = [
    { mode: 'regenerate', label: 'Regenerate' },
    { mode: 'write', label: 'Write the response' },
];

const ResponseChangeTabs: React.FC<ResponseChangeTabsProps> = ({ mode, onSelect }) => (
    <div
        className='tabs'
        role='tablist'
    >
        {TABS.map((tab) => (
            <button
                key={tab.mode}
                type='button'
                role='tab'
                aria-selected={tab.mode === mode}
                onClick={() => onSelect(tab.mode)}
            >
                {tab.label}
            </button>
        ))}
    </div>
);

export default React.memo(ResponseChangeTabs);

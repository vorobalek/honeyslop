import { ResponseChangeModeType } from '../../types/responseChangeModeType';
import React from 'react';

type ResponseChangeTabsProps = {
    mode: ResponseChangeModeType;
    creating: boolean;
    onSelect: (mode: ResponseChangeModeType) => void;
};

const ResponseChangeTabs: React.FC<ResponseChangeTabsProps> = ({ mode, creating, onSelect }) => {
    const tabs: readonly { mode: ResponseChangeModeType; label: string }[] = [
        { mode: 'regenerate', label: creating ? 'Generate' : 'Regenerate' },
        { mode: 'write', label: 'Write the response' },
    ];
    return (
        <div
            className='tabs'
            role='tablist'
        >
            {tabs.map((tab) => (
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
};

export default React.memo(ResponseChangeTabs);

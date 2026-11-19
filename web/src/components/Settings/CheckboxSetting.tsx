import React, { type ReactNode } from 'react';

type CheckboxSettingProps = {
    name: string;
    label: string;
    checked: boolean;
    children: ReactNode;
};

const CheckboxSetting: React.FC<CheckboxSettingProps> = ({ name, label, checked, children }) => (
    <div className='setting'>
        <label className='checkbox'>
            <input
                type='checkbox'
                id={name}
                name={name}
                defaultChecked={checked}
            />
            {` ${label}`}
        </label>
        <p className='hint'>{children}</p>
    </div>
);

export default React.memo(CheckboxSetting);

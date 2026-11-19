import { GUIDANCE_MAX_LENGTH } from '../../lib/constants';
import React from 'react';

type GuidanceFieldsProps = {
    active: boolean;
    guidance: string | null;
    error: string | undefined;
};

const GuidanceFields: React.FC<GuidanceFieldsProps> = ({ active, guidance, error }) => (
    <fieldset
        role='tabpanel'
        hidden={!active}
        disabled={!active}
    >
        <div className='setting'>
            <label htmlFor='change-guidance'>Comment for the model</label>
            <textarea
                id='change-guidance'
                name='Guidance'
                rows={5}
                maxLength={GUIDANCE_MAX_LENGTH}
                placeholder='Answer as a Hikvision DS-2CD2032-I camera on firmware V5.4.0 build 160530'
                defaultValue={guidance ?? ''}
            />
            {error && <span className='error'>{error}</span>}
            <p className='hint'>Optional. The model gets it along with its usual instructions for this request.</p>
        </div>
    </fieldset>
);

export default React.memo(GuidanceFields);

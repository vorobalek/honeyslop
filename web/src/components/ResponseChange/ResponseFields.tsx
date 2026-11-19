import { RESPONSE_BODY_MAX_LENGTH } from '../../lib/constants';
import { EntryType } from '../../types/entryType';
import { headerLines } from './responseChangeData';
import React from 'react';

type ResponseFieldsProps = {
    active: boolean;
    current: EntryType | null;
    errors: Record<string, string>;
};

const HEADERS_PLACEHOLDER = 'Content-Type: application/xml\nServer: App-webs/';

const ResponseFields: React.FC<ResponseFieldsProps> = ({ active, current, errors }) => (
    <fieldset
        role='tabpanel'
        hidden={!active}
        disabled={!active}
    >
        <div className='setting'>
            <label htmlFor='change-status'>Status</label>
            <input
                type='number'
                id='change-status'
                name='Status'
                min='200'
                max='599'
                step='1'
                defaultValue={current?.responseStatus ?? 200}
            />
            {errors.Status && <span className='error'>{errors.Status}</span>}
        </div>
        <div className='setting'>
            <label htmlFor='change-headers'>Headers</label>
            <textarea
                id='change-headers'
                name='Headers'
                className='mono'
                rows={4}
                spellCheck={false}
                placeholder={HEADERS_PLACEHOLDER}
                defaultValue={headerLines(current?.responseHeaders ?? [])}
            />
            {errors.Headers && <span className='error'>{errors.Headers}</span>}
            <p className='hint'>
                One per line as Name: value. Without a Content-Type the body goes out as text/html; charset=utf-8.
            </p>
        </div>
        <div className='setting'>
            <label htmlFor='change-body'>Body</label>
            <textarea
                id='change-body'
                name='Body'
                className='mono'
                rows={14}
                spellCheck={false}
                maxLength={RESPONSE_BODY_MAX_LENGTH}
                defaultValue={current?.responseBody ?? ''}
            />
            {errors.Body && <span className='error'>{errors.Body}</span>}
        </div>
    </fieldset>
);

export default React.memo(ResponseFields);

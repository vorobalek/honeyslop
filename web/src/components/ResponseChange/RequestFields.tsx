import { REQUEST_METHODS } from '../../lib/constants';
import React from 'react';

type RequestFieldsProps = {
    disabled: boolean;
    errors: Record<string, string>;
};

const RequestFields: React.FC<RequestFieldsProps> = ({ disabled, errors }) => (
    <fieldset disabled={disabled}>
        <div className='request-line-fields'>
            <div className='setting'>
                <label htmlFor='change-method'>Method</label>
                <select
                    id='change-method'
                    name='Method'
                    defaultValue='GET'
                >
                    {REQUEST_METHODS.map((method) => (
                        <option
                            key={method}
                            value={method}
                        >
                            {method}
                        </option>
                    ))}
                </select>
                {errors.Method && <span className='error'>{errors.Method}</span>}
            </div>
            <div className='setting'>
                <label htmlFor='change-host'>Host</label>
                <input
                    type='text'
                    id='change-host'
                    name='Host'
                    className='mono'
                    spellCheck={false}
                    placeholder='example.com'
                />
                {errors.Host && <span className='error'>{errors.Host}</span>}
            </div>
            <div className='setting target-field'>
                <label htmlFor='change-target'>Path</label>
                <input
                    type='text'
                    id='change-target'
                    name='Target'
                    className='mono'
                    spellCheck={false}
                    placeholder='/path?query'
                />
                {errors.Target && <span className='error'>{errors.Target}</span>}
            </div>
        </div>
        <p className='hint'>
            Method, host and path are the request as the trap receives it, the response is cached under them.
        </p>
        <div className='setting'>
            <label htmlFor='change-request-headers'>Request headers</label>
            <textarea
                id='change-request-headers'
                name='RequestHeaders'
                className='mono'
                rows={3}
                spellCheck={false}
                placeholder={'User-Agent: Mozilla/5.0\nAccept: */*'}
            />
            {errors.RequestHeaders && <span className='error'>{errors.RequestHeaders}</span>}
        </div>
        <div className='setting'>
            <label htmlFor='change-request-body'>Request body</label>
            <textarea
                id='change-request-body'
                name='RequestBody'
                className='mono'
                rows={3}
                spellCheck={false}
            />
            {errors.RequestBody && <span className='error'>{errors.RequestBody}</span>}
            <p className='hint'>Optional. Headers and body only tell the model more about the request.</p>
        </div>
    </fieldset>
);

export default React.memo(RequestFields);

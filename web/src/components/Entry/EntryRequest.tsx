import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import React from 'react';

type EntryRequestProps = {
    watch: Watch<EntryViewType>;
};

const EntryRequest: React.FC<EntryRequestProps> = ({ watch }) => {
    const text = useWatched(watch, ({ entry }) =>
        [
            `${entry.method} ${entry.target} HTTP/1.1`,
            `Host: ${entry.host}`,
            entry.requestHeaders ?? '',
            entry.requestBody ?? '',
        ].join('\n'),
    );
    return (
        <>
            <h2>Request that triggered the generation</h2>
            <pre className='code'>{text}</pre>
        </>
    );
};

export default React.memo(EntryRequest);

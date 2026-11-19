import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import React from 'react';

type EntryResponseProps = {
    watch: Watch<EntryViewType>;
};

const EntryResponse: React.FC<EntryResponseProps> = ({ watch }) => {
    const id = useWatched(watch, (view) => view.entry.id);
    const status = useWatched(watch, (view) => view.entry.responseStatus);
    const headers = useWatched(watch, (view) => view.entry.responseHeaders);
    const body = useWatched(watch, (view) => view.entry.responseBody);
    const canPreview = useWatched(watch, (view) => view.canPreview);
    if (status === null) return null;
    return (
        <>
            <h2>Response headers</h2>
            <div className='table-scroll'>
                <table className='headers'>
                    <tbody>
                        {headers.map((header, index) => (
                            <tr key={index}>
                                <th className='mono'>{header.name}</th>
                                <td className='mono'>{header.value}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
            <h2>Response body</h2>
            <pre className='code body'>{body}</pre>
            {canPreview && (
                <>
                    <h2>Preview</h2>
                    <iframe
                        className='preview'
                        sandbox=''
                        title='Rendered response body'
                        src={`/entries/${id}/preview`}
                    />
                </>
            )}
        </>
    );
};

export default React.memo(EntryResponse);

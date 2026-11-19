import { useWatched } from '../../hooks/useWatched';
import { Watch } from '../../lib/watchStore';
import { EntryViewType } from '../../types/entryViewType';
import React, { useLayoutEffect, useRef, type UIEvent } from 'react';

type EntryOutputProps = {
    watch: Watch<EntryViewType>;
};

const EntryOutput: React.FC<EntryOutputProps> = ({ watch }) => {
    const output = useWatched(watch, (view) => view.output);
    const pre = useRef<HTMLPreElement>(null);
    const following = useRef(true);

    useLayoutEffect(() => {
        if (pre.current !== null && following.current) pre.current.scrollTop = pre.current.scrollHeight;
    }, [output]);

    const onScroll = (event: UIEvent<HTMLPreElement>) => {
        const element = event.currentTarget;
        following.current = element.scrollTop + element.clientHeight >= element.scrollHeight - 4;
    };

    return (
        <>
            <h2>Output so far</h2>
            <div>
                {output === null ? (
                    <p className='muted'>Waiting for the first token…</p>
                ) : (
                    <>
                        <p className='muted'>
                            {`${output.tokens} ${output.tokens === 1 ? 'token' : 'tokens'} so far${output.truncated ? ', only the tail is shown' : ''}`}
                        </p>
                        <pre
                            className='code body'
                            ref={pre}
                            onScroll={onScroll}
                        >
                            {`${output.truncated ? '…' : ''}${output.content}`}
                        </pre>
                    </>
                )}
            </div>
        </>
    );
};

export default React.memo(EntryOutput);

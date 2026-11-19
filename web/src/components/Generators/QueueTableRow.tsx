import { duration } from '../../lib/time';
import { EntryRowType } from '../../types/entryRowType';
import Link from '../Common/Link';
import { queuedFor, requestedAgo } from './entryTimes';
import React from 'react';

type QueueTableRowProps = {
    entry: EntryRowType;
    asOf: string;
};

const QueueTableRow: React.FC<QueueTableRowProps> = ({ entry, asOf }) => {
    const requested = requestedAgo(entry, asOf);
    return (
        <tr>
            <td>
                <Link to={`/entries/${entry.id}`}>{entry.id}</Link>
            </td>
            <td className='mono'>{entry.method}</td>
            <td className='mono host'>{entry.host}</td>
            <td className='mono target'>{entry.target}</td>
            <td className='number'>{duration(queuedFor(entry, asOf))}</td>
            <td className='number'>{requested === null ? '—' : `${duration(requested)} ago`}</td>
        </tr>
    );
};

export default React.memo(QueueTableRow);

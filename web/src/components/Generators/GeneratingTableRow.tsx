import { duration } from '../../lib/time';
import { EntryRowType } from '../../types/entryRowType';
import Link from '../Common/Link';
import { runningFor, waitedBefore } from './entryTimes';
import React from 'react';

type GeneratingTableRowProps = {
    entry: EntryRowType;
    asOf: string;
};

const GeneratingTableRow: React.FC<GeneratingTableRowProps> = ({ entry, asOf }) => (
    <tr>
        <td>
            <Link to={`/entries/${entry.id}`}>{entry.id}</Link>
        </td>
        <td className='mono'>{entry.method}</td>
        <td className='mono host'>{entry.host}</td>
        <td className='mono target'>{entry.target}</td>
        <td className='mono nowrap'>{entry.generator ?? <span className='muted'>waiting for a slot</span>}</td>
        <td className='number'>{entry.attempts + 1}</td>
        <td className='number'>{duration(runningFor(entry, asOf))}</td>
        <td className='number'>{duration(waitedBefore(entry, asOf))}</td>
    </tr>
);

export default React.memo(GeneratingTableRow);

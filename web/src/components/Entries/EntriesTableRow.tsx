import { entryStateBadge } from '../../lib/badges';
import { size } from '../../lib/numbers';
import { SelectionStore } from '../../lib/selectionStore';
import { display, displayOrDash } from '../../lib/time';
import { EntryRowType } from '../../types/entryRowType';
import Link from '../Common/Link';
import RowCheckbox from './RowCheckbox';
import React from 'react';

type EntriesTableRowProps = {
    entry: EntryRowType;
    selection: SelectionStore;
    disabled: boolean;
};

function stateTitle(entry: EntryRowType): string | undefined {
    if (entry.state !== 'Generating') return entry.error ?? undefined;
    const where = entry.generator ? `on ${entry.generator}` : 'waiting for a slot';
    return `${where} since ${displayOrDash(entry.claimedAt)} UTC`;
}

const EntriesTableRow: React.FC<EntriesTableRowProps> = ({ entry, selection, disabled }) => (
    <tr>
        <td className='check'>
            <RowCheckbox
                id={entry.id}
                selection={selection}
                disabled={disabled}
            />
        </td>
        <td>
            <span
                className={entryStateBadge(entry.state)}
                title={stateTitle(entry)}
            >
                {entry.state.toLowerCase()}
            </span>
        </td>
        <td className='mono'>{entry.method}</td>
        <td className='mono host'>{entry.host}</td>
        <td className='mono target'>
            <Link to={`/entries/${entry.id}`}>{entry.target}</Link>
        </td>
        <td className='number mono'>{entry.responseStatus}</td>
        <td className='number'>{size(entry.bodyLength)}</td>
        <td className='number'>{entry.hits}</td>
        <td className='time'>{display(entry.createdAt)}</td>
        <td className='time'>{displayOrDash(entry.lastHitAt)}</td>
    </tr>
);

export default React.memo(EntriesTableRow);

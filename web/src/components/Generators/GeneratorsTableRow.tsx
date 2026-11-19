import { displayOrDash } from '../../lib/time';
import { GeneratorType } from '../../types/generatorType';
import SlotsMeter from './SlotsMeter';
import { GENERATOR_COLUMNS, generatorState, modelDetails } from './generatorColumns';
import React from 'react';

type GeneratorsTableRowProps = {
    generator: GeneratorType;
};

const cell = (index: number, generator: GeneratorType) => GENERATOR_COLUMNS[index].value(generator, undefined);

const GeneratorsTableRow: React.FC<GeneratorsTableRowProps> = ({ generator }) => {
    const status = generator.status;
    const state = generatorState(generator);
    const details = modelDetails(generator);
    return (
        <tr>
            <td className='mono nowrap'>
                {generator.name}
                {generator.address && generator.address !== generator.name && (
                    <span className='sub'>{generator.address}</span>
                )}
            </td>
            <td>
                <span className={state.badge}>{state.label}</span>
            </td>
            <td className='mono nowrap'>
                {status ? status.model : '—'}
                {details.length > 0 && <span className='sub'>{details.join(', ')}</span>}
            </td>
            <td className='number nowrap'>{status && <SlotsMeter status={status} />}</td>
            <td className='number'>{cell(4, generator)}</td>
            <td className='number'>{cell(5, generator)}</td>
            <td className='number'>{cell(6, generator)}</td>
            <td className='number'>{cell(7, generator)}</td>
            <td className='number'>{generator.generated}</td>
            <td className='number'>{cell(9, generator)}</td>
            <td className='number'>{cell(10, generator)}</td>
            <td className='time'>{displayOrDash(generator.lastGeneratedAt)}</td>
        </tr>
    );
};

export default React.memo(GeneratorsTableRow);

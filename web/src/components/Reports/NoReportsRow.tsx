import React from 'react';

type NoReportsRowProps = {
    columns: number;
};

const NoReportsRow: React.FC<NoReportsRowProps> = ({ columns }) => (
    <tr>
        <td
            colSpan={columns}
            className='empty'
        >
            No abuse reports yet.
        </td>
    </tr>
);

export default React.memo(NoReportsRow);

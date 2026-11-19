import { countOf } from '../../lib/numbers';
import { display } from '../../lib/time';
import { BouncedContactType } from '../../types/bouncedContactType';
import Link from '../Common/Link';
import React from 'react';

type ContactBouncesProps = {
    bounce: BouncedContactType;
};

const ContactBounces: React.FC<ContactBouncesProps> = ({ bounce }) => (
    <Link
        to={`/reports/${bounce.lastReportId}`}
        className='error'
        title={[bounce.lastStatus, bounce.lastDiagnostic, `${display(bounce.lastBouncedAt)} UTC`]
            .filter(Boolean)
            .join(' · ')}
    >
        {countOf(bounce.bounces, 'bounce')}
    </Link>
);

export default React.memo(ContactBounces);

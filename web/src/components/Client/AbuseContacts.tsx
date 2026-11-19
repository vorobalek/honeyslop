import { href } from '../../lib/listQuery';
import Link from '../Common/Link';
import BlockContactButton from './BlockContactButton';
import React from 'react';

type AbuseContactsProps = {
    recipients: readonly string[];
    blocked: readonly string[];
};

const AbuseContacts: React.FC<AbuseContactsProps> = ({ recipients, blocked }) => (
    <span className='abuse-contacts'>
        {recipients.map((contact) => (
            <span key={contact}>
                <span className='mono'>{contact}</span> <BlockContactButton contact={contact} />
            </span>
        ))}
        {blocked.map((contact) => (
            <span key={contact}>
                <span className='mono'>{contact}</span>{' '}
                <Link to={href('/reports/blocked', { Search: contact.slice(contact.indexOf('@')) })}>blocked</Link>
            </span>
        ))}
    </span>
);

export default React.memo(AbuseContacts);

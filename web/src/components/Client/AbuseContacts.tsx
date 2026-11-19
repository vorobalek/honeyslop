import { href } from '../../lib/listQuery';
import { BouncedContactType } from '../../types/bouncedContactType';
import Link from '../Common/Link';
import BlockContactButton from './BlockContactButton';
import ContactBounces from './ContactBounces';
import React from 'react';

type AbuseContactsProps = {
    recipients: readonly string[];
    blocked: readonly string[];
    bounces: readonly BouncedContactType[];
};

function bounceOf(bounces: readonly BouncedContactType[], contact: string): BouncedContactType | undefined {
    const address = contact.trim().toLowerCase();
    return bounces.find((x) => x.contact === address);
}

const AbuseContacts: React.FC<AbuseContactsProps> = ({ recipients, blocked, bounces }) => (
    <span className='abuse-contacts'>
        {recipients.map((contact) => {
            const bounce = bounceOf(bounces, contact);
            return (
                <span key={contact}>
                    <span className='mono'>{contact}</span>
                    {bounce && (
                        <>
                            {' '}
                            <ContactBounces bounce={bounce} />
                        </>
                    )}{' '}
                    <BlockContactButton contact={contact} />
                </span>
            );
        })}
        {blocked.map((contact) => {
            const bounce = bounceOf(bounces, contact);
            return (
                <span key={contact}>
                    <span className='mono'>{contact}</span>
                    {bounce && (
                        <>
                            {' '}
                            <ContactBounces bounce={bounce} />
                        </>
                    )}{' '}
                    <Link to={href('/reports/blocked', { Search: contact.slice(contact.indexOf('@')) })}>blocked</Link>
                </span>
            );
        })}
    </span>
);

export default React.memo(AbuseContacts);

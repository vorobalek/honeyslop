import { ContactsChangeType } from '../types/contactsChangeType';
import { confirmAction } from './confirmation';
import { invokeHub } from './hubConnection';
import { postNotice } from './noticeStore';

function count(changed: number): string {
    return `${changed} ${changed === 1 ? 'contact' : 'contacts'}`;
}

export async function blockContacts(contacts: string[], note: string | null): Promise<ContactsChangeType | null> {
    const subject = contacts.length === 1 ? contacts[0] : `${contacts.length} contacts`;
    const confirmed = await confirmAction({
        question: `Block ${subject}?`,
        detail: 'Abuse reports never go there again, the ones already queued included.',
        button: 'Block',
    });
    if (!confirmed) return null;
    const change = await invokeHub<ContactsChangeType>('blockContacts', contacts, note);
    if (change.done)
        postNotice(
            change.changed === 0 ? 'Every one of them was blocked already.' : `Blocked ${count(change.changed)}.`,
        );
    return change;
}

export async function unblockContact(contact: string): Promise<void> {
    const confirmed = await confirmAction({
        question: `Unblock ${contact}?`,
        detail: 'Abuse reports may go there again.',
        button: 'Unblock',
    });
    if (!confirmed) return;
    const change = await invokeHub<ContactsChangeType>('unblockContacts', [contact]);
    postNotice(change.changed === 0 ? `${contact} was not blocked.` : `Unblocked ${contact}.`);
}

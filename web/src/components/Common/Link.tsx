import { navigate } from '../../lib/navigation';
import React, { type AnchorHTMLAttributes, type MouseEvent } from 'react';

type LinkProps = AnchorHTMLAttributes<HTMLAnchorElement> & {
    to: string;
};

function opensElsewhere(event: MouseEvent<HTMLAnchorElement>): boolean {
    return event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey;
}

const Link: React.FC<LinkProps> = ({ to, children, ...rest }) => {
    const onClick = (event: MouseEvent<HTMLAnchorElement>) => {
        if (event.defaultPrevented || opensElsewhere(event)) return;
        event.preventDefault();
        navigate(to);
    };
    return (
        <a
            href={to}
            onClick={onClick}
            {...rest}
        >
            {children}
        </a>
    );
};

export default React.memo(Link);

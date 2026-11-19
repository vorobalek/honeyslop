import { usePath, useSearch } from '../../hooks/useLocation';
import { signInHref } from '../../lib/navigation';
import React from 'react';

const SignInLink: React.FC = () => {
    const path = usePath();
    const search = useSearch();
    return <a href={signInHref({ path, search })}>Sign in</a>;
};

export default React.memo(SignInLink);

import { currentLocation, listenToLocation, takeScrollRequest } from '../../lib/navigation';
import { PathContext, SearchContext } from './locationContexts';
import React, { useEffect, useState, type ReactNode } from 'react';

type RouterProps = {
    children: ReactNode;
};

const Router: React.FC<RouterProps> = ({ children }) => {
    const [location, setLocation] = useState(currentLocation);

    useEffect(() => listenToLocation(setLocation), []);
    useEffect(() => {
        if (takeScrollRequest()) window.scrollTo(0, 0);
    }, [location]);

    return (
        <PathContext.Provider value={location.path}>
            <SearchContext.Provider value={location.search}>{children}</SearchContext.Provider>
        </PathContext.Provider>
    );
};

export default Router;

import ConfirmDialog from './components/Layout/ConfirmDialog';
import Footer from './components/Layout/Footer';
import Header from './components/Layout/Header';
import Main from './components/Layout/Main';
import React, { Suspense } from 'react';

const App: React.FC = () => (
    <Suspense fallback={null}>
        <Header />
        <Main />
        <ConfirmDialog />
        <Footer />
    </Suspense>
);

export default App;

import App from './App';
import Router from './components/Router/Router';
import { startHubConnection } from './lib/hubConnection';
import './styles/site.css';
import React from 'react';
import { createRoot } from 'react-dom/client';

const root = document.getElementById('root');
if (root !== null) {
    startHubConnection();
    createRoot(root).render(
        <React.StrictMode>
            <Router>
                <App />
            </Router>
        </React.StrictMode>,
    );
}

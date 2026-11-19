import React from 'react';

const Footer: React.FC = () => (
    <footer className='footer'>
        <a
            href='https://db-ip.com'
            target='_blank'
            rel='noopener'
        >
            IP Geolocation by DB-IP
        </a>
    </footer>
);

export default React.memo(Footer);

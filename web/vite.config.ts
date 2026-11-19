import react from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

const admin = process.env.HONEYSLOP_ADMIN_URL ?? 'http://localhost:8080';

export default defineConfig(({ mode }) => ({
    base: process.env.HONEYSLOP_BASE_URL ?? '/',
    define: {
        'import.meta.env.HONEYSLOP_HUB_URL': JSON.stringify(process.env.HONEYSLOP_HUB_URL ?? '/hub'),
    },
    plugins: [react()],
    build: {
        outDir: '../src/Honeyslop.Admin/wwwroot',
        emptyOutDir: true,
        minify: mode !== 'audit',
    },
    server: {
        proxy: {
            '/hub': { target: admin, ws: true },
            '/signin': admin,
            '/signout': admin,
            '/signin-oidc': admin,
            '^/clients/[^/]+/archive$': admin,
            '^/entries/\\d+/preview$': admin,
        },
    },
}));

import { defineConfig } from '@playwright/test';

export const AUDIT_URL = process.env.HONEYSLOP_AUDIT_URL ?? 'http://localhost:8094';

export default defineConfig({
    testDir: './tests',
    timeout: 3 * 60_000,
    expect: { timeout: 15_000 },
    fullyParallel: false,
    workers: 1,
    forbidOnly: !!process.env.CI,
    reporter: [['list']],
    use: {
        baseURL: AUDIT_URL,
        trace: 'retain-on-failure',
        viewport: { width: 1280, height: 900 },
    },
});

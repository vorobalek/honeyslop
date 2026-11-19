import { installReactRenderProfiler } from './reactRenderProfiler';
import type { Browser, BrowserContext, Page } from '@playwright/test';

export class AdminApp {
    private constructor(
        readonly context: BrowserContext,
        readonly page: Page,
        private readonly origin: string,
    ) {}

    static async launch(browser: Browser, origin: string, operator: boolean): Promise<AdminApp> {
        const context = await browser.newContext({ viewport: { width: 1280, height: 900 } });
        const page = await context.newPage();
        await page.addInitScript(installReactRenderProfiler);
        const app = new AdminApp(context, page, origin);
        if (operator) await app.signIn();
        return app;
    }

    async close(): Promise<void> {
        await this.context.close();
    }

    async open(path: string): Promise<void> {
        await this.page.goto(`${this.origin}${path}`);
        await this.ready();
    }

    async ready(): Promise<void> {
        await this.page.locator('[data-live-status].online').waitFor();
        await this.page.locator('main:not([aria-busy=true])').waitFor();
    }

    async navigateTo(label: string, path: string): Promise<void> {
        await this.page.locator('nav.nav a', { hasText: label }).click();
        await this.page.waitForURL((url) => url.pathname === path);
        await this.ready();
    }

    async followLink(text: string, matches: (url: URL) => boolean): Promise<void> {
        await this.page.locator('main a', { hasText: text }).first().click();
        await this.page.waitForURL(matches);
        await this.ready();
    }

    async typeInto(selector: string, text: string): Promise<void> {
        await this.page.locator(selector).pressSequentially(text);
    }

    async waitForQuery(name: string, value: string | null): Promise<void> {
        await this.page.waitForURL((url) => url.searchParams.get(name) === value);
        await this.ready();
    }

    private async signIn(): Promise<void> {
        await this.page.goto(`${this.origin}/signin?returnUrl=/`);
        await this.page.locator('input[name=username]').fill('operator');
        await this.page.locator('input[type=submit], button[type=submit]').first().click();
        await this.page.waitForURL((url) => url.toString().startsWith(this.origin));
        await this.ready();
    }
}

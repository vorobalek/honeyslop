import { AUDIT_URL } from '../playwright.config';
import { AdminApp } from './support/adminApp';
import { hitEntry, recordRequest, sql, streamOutput } from './support/database';
import {
    assertProfilerAttached,
    collectRenders,
    formatReport,
    measureRenders,
    type MeasureOptions,
    type RenderReport,
} from './support/reactRenderProfiler';
import { expect, test, type Page, type TestInfo } from '@playwright/test';

type Budget = {
    idle?: boolean;
    renders?: number;
    forbidden?: string[];
    max?: Record<string, number>;
    maxInstances?: Record<string, number>;
};

const SHELL = ['App', 'Header', 'Navigation', 'LiveIndicator', 'Account', 'Footer', 'ConfirmDialog'];
const STILL = [...SHELL, 'Main', 'Routes'];
const STILL_BUT_DIALOG = STILL.filter((component) => component !== 'ConfirmDialog');
const SHELL_BUT_DIALOG = SHELL.filter((component) => component !== 'ConfirmDialog');
const TYPING: MeasureOptions = { quietMs: 50 };
const ENTRY_LIST = ['EntriesPage', 'EntryFilters', 'EntriesActions', 'EntriesTable', 'EntriesTableRow'];
const ENTRY_PAGE = ['EntryDetails', 'EntryHeading', 'EntryMeta', 'EntryRequest', 'EntryResponse', 'EntryActions'];
const CHANGE_FIELDS = ['ResponseChangeTabs', 'GuidanceFields', 'ResponseFields'];

const BUDGETS: Record<string, Budget> = {
    'cold load of the entries': { renders: 220, max: { App: 1, Header: 1, EntriesPage: 1, EntriesTableRow: 50 } },
    'type into the host filter': { idle: true },
    'apply the host filter': { renders: 60, forbidden: SHELL, max: { EntriesPage: 1, EntryFilters: 1 } },
    'reset the filter': { renders: 170, forbidden: SHELL, max: { EntriesPage: 1, EntryFilters: 2 } },
    'select an entry': {
        renders: 4,
        forbidden: [...STILL, ...ENTRY_LIST, 'EntriesStats', 'ListPager'],
        max: { RowCheckbox: 1, ChangeSelectedButton: 1 },
    },
    'ask to expire the selected entry': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, ...ENTRY_LIST, 'ExpireSelectedButton', 'RowCheckbox'],
        max: { ConfirmDialog: 1 },
    },
    'keep the selected entry': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, ...ENTRY_LIST, 'ExpireSelectedButton', 'RowCheckbox'],
        max: { ConfirmDialog: 1 },
    },
    'open the change of the selected entry': {
        renders: 5,
        forbidden: [...STILL, ...ENTRY_LIST, 'ExpireSelectedButton', 'RowCheckbox'],
        max: { ChangeSelectedButton: 1, ResponseChangeDialog: 1 },
    },
    'close the change of the selected entry': {
        renders: 1,
        forbidden: [...STILL, ...ENTRY_LIST, 'ExpireSelectedButton', 'RowCheckbox', 'ResponseChangeDialog'],
        max: { ChangeSelectedButton: 1 },
    },
    'open a new entry': {
        renders: 6,
        forbidden: [...STILL, ...ENTRY_LIST, 'ChangeSelectedButton', 'ExpireSelectedButton', 'RowCheckbox'],
        max: { NewEntryButton: 1, ResponseChangeDialog: 1, RequestFields: 1 },
    },
    'close the new entry': {
        renders: 1,
        forbidden: [...STILL, ...ENTRY_LIST, 'ResponseChangeDialog', 'RowCheckbox'],
        max: { NewEntryButton: 1 },
    },
    'select every entry': { renders: 53, forbidden: [...STILL, ...ENTRY_LIST, 'EntriesStats', 'ListPager'] },
    'clear the selection': { renders: 53, forbidden: [...STILL, ...ENTRY_LIST, 'EntriesStats', 'ListPager'] },
    'an entry is hit': {
        renders: 6,
        forbidden: [...STILL, 'EntriesPage', 'EntryFilters', 'EntriesActions', 'ListPager', 'RowCheckbox'],
        maxInstances: { EntriesTableRow: 1 },
    },
    'sort the entries by hits': { renders: 40, forbidden: [...SHELL, 'EntryFilters'], max: { SortHeader: 2 } },
    'go to the requests': { renders: 620, forbidden: SHELL },
    'a request arrives': {
        renders: 8,
        forbidden: [...STILL, 'RequestsPage', 'RequestFilters', 'RequestsPanel', 'SortHeader', 'ListPager'],
        maxInstances: { RequestsTableRow: 1 },
    },
    'go to the generators': { renders: 40, forbidden: SHELL, max: { ClientSortHeader: 3 } },
    'open an entry being generated': { renders: 22, forbidden: SHELL },
    'the output streams': {
        renders: 2,
        forbidden: [...STILL, 'EntryDetails', 'EntryHeading', 'EntryMeta', 'EntryRequest', 'EntryResponse'],
        max: { EntryOutput: 1 },
    },
    'open the response change': {
        renders: 5,
        forbidden: [...STILL, ...ENTRY_PAGE, 'EntryExpireForm', 'EntryOutput'],
        max: { EntryChangeButton: 1, ResponseChangeDialog: 1 },
    },
    'type a comment for the model': { idle: true },
    'switch to writing the response': {
        renders: 4,
        forbidden: [...STILL, ...ENTRY_PAGE, 'EntryChangeButton', 'EntryExpireForm', 'EntryOutput'],
        max: { ResponseChangeDialog: 1, ...Object.fromEntries(CHANGE_FIELDS.map((component) => [component, 1])) },
    },
    'ask to replace the response': {
        renders: 2,
        forbidden: [...STILL_BUT_DIALOG, ...ENTRY_PAGE, ...CHANGE_FIELDS, 'EntryChangeButton', 'EntryOutput'],
        max: { ConfirmDialog: 1, ResponseChangeDialog: 1 },
    },
    'replace the response': {
        renders: 40,
        forbidden: SHELL_BUT_DIALOG,
        max: { ConfirmDialog: 1, EntryDetails: 2, EntryChangeButton: 2 },
    },
    'go to the clients': { renders: 30, forbidden: SHELL },
    'sort the clients by requests': { renders: 85, forbidden: SHELL },
    'open a client': { renders: 200, forbidden: SHELL, max: { WhoisPanel: 2 } },
    'type a sender name': { idle: true },
    'open the confirmation': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, 'ClientPage', 'AbuseReport', 'SendReportForm', 'RequestsTableRow'],
        max: { ConfirmDialog: 1 },
    },
    'cancel the confirmation': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, 'ClientPage', 'AbuseReport', 'SendReportForm', 'RequestsTableRow'],
        max: { ConfirmDialog: 1 },
    },
    'go to the settings': { renders: 23, forbidden: SHELL },
    'type into a setting': { idle: true },
    'ask to save the settings': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, 'SettingsPage', 'SettingsForm', 'PersonalSettingsForm'],
        max: { ConfirmDialog: 1 },
    },
    'keep the settings': {
        renders: 1,
        forbidden: [...STILL_BUT_DIALOG, 'SettingsPage', 'SettingsForm', 'PersonalSettingsForm'],
        max: { ConfirmDialog: 1 },
    },
};

async function record(testInfo: TestInfo, name: string, report: RenderReport): Promise<void> {
    const formatted = formatReport(name, report);
    console.log(formatted);
    await testInfo.attach(`renders -- ${name}`, { body: formatted, contentType: 'text/plain' });

    const budget = BUDGETS[name] ?? {};
    if (budget.idle) {
        expect.soft(report.total, `"${name}" must not re-render anything\n${formatted}`).toBe(0);
        return;
    }
    expect
        .soft(report.commits.length, `"${name}" recorded no commits -- the step is not measuring what it claims`)
        .toBeGreaterThan(0);
    if (budget.renders !== undefined) {
        expect
            .soft(report.total, `"${name}" exceeded its render budget\n${formatted}`)
            .toBeLessThanOrEqual(budget.renders);
    }
    for (const component of budget.forbidden ?? []) {
        expect
            .soft(report.byComponent[component] ?? 0, `"${name}" must not re-render ${component}\n${formatted}`)
            .toBe(0);
    }
    for (const [component, ceiling] of Object.entries(budget.max ?? {})) {
        expect
            .soft(report.byComponent[component] ?? 0, `"${name}" rendered ${component} too often\n${formatted}`)
            .toBeLessThanOrEqual(ceiling);
    }
    for (const [component, ceiling] of Object.entries(budget.maxInstances ?? {})) {
        const instances = Object.keys(report.byInstance).filter((label) => label.startsWith(`${component}#`)).length;
        expect
            .soft(instances, `"${name}" rendered too many distinct ${component} instances\n${formatted}`)
            .toBeLessThanOrEqual(ceiling);
    }
}

async function step(
    testInfo: TestInfo,
    page: Page,
    name: string,
    action: () => Promise<void>,
    options?: MeasureOptions,
): Promise<void> {
    await test.step(name, async () => {
        await record(testInfo, name, await measureRenders(page, action, options));
    });
}

test('render audit', async ({ browser }, testInfo) => {
    const app = await AdminApp.launch(browser, AUDIT_URL, true);
    const { page } = app;
    const firstEntry = Number(sql('select id from entry order by id desc limit 1;'));
    const generating = Number(sql('select id from entry where status = 0 and claimed_by is not null limit 1;'));

    try {
        await app.open('/');
        await assertProfilerAttached(page);
        await record(testInfo, 'cold load of the entries', await collectRenders(page));

        await step(
            testInfo,
            page,
            'type into the host filter',
            () => app.typeInto('form.filters input[name=Host]', 'ap'),
            TYPING,
        );
        await step(
            testInfo,
            page,
            'apply the host filter',
            async () => {
                await page.locator('form.filters input[name=Host]').press('Enter');
                await app.waitForQuery('Host', 'ap');
            },
            TYPING,
        );
        await step(testInfo, page, 'reset the filter', async () => {
            await page.locator('form.filters a', { hasText: 'Reset' }).click();
            await app.waitForQuery('Host', null);
        });

        await step(testInfo, page, 'select an entry', () => page.locator('tbody .check input').first().check());
        await step(testInfo, page, 'ask to expire the selected entry', async () => {
            await page.locator('form#bulk button').click();
            await page.locator('dialog.confirm[open]').waitFor();
        });
        await step(testInfo, page, 'keep the selected entry', async () => {
            await page.locator('dialog.confirm button[value=cancel]').click();
            await page.locator('dialog.confirm:not([open])').waitFor({ state: 'attached' });
        });
        await step(testInfo, page, 'open the change of the selected entry', async () => {
            await page.locator('.actions button', { hasText: 'Change 1 selected' }).click();
            await page.locator('dialog.change-response[open]').waitFor();
        });
        await step(testInfo, page, 'close the change of the selected entry', async () => {
            await page.locator('dialog.change-response button', { hasText: 'Cancel' }).click();
            await page.locator('dialog.change-response').waitFor({ state: 'detached' });
        });
        await step(testInfo, page, 'open a new entry', async () => {
            await page.locator('.actions button', { hasText: 'New entry' }).click();
            await page.locator('dialog.change-response[open]').waitFor();
        });
        await step(testInfo, page, 'close the new entry', async () => {
            await page.locator('dialog.change-response button', { hasText: 'Cancel' }).click();
            await page.locator('dialog.change-response').waitFor({ state: 'detached' });
        });
        await step(testInfo, page, 'select every entry', () => page.locator('thead .check input').check());
        await step(testInfo, page, 'clear the selection', () => page.locator('thead .check input').uncheck());

        const hits = page.locator(`tbody tr:has(a[href="/entries/${firstEntry}"]) td:nth-child(8)`);
        const before = await hits.innerText();
        await step(testInfo, page, 'an entry is hit', async () => {
            hitEntry(firstEntry);
            await expect(hits).not.toHaveText(before);
        });

        await step(testInfo, page, 'sort the entries by hits', async () => {
            await page.locator('thead th a', { hasText: 'Hits' }).click();
            await app.waitForQuery('Sort', 'Hits');
        });

        await step(testInfo, page, 'go to the requests', () => app.navigateTo('Requests', '/requests'));
        const count = page.locator('.actions .count');
        const counted = await count.innerText();
        await step(testInfo, page, 'a request arrives', async () => {
            recordRequest('192.0.2.10', '/render-audit');
            await expect(count).not.toHaveText(counted);
        });

        await step(testInfo, page, 'go to the generators', () => app.navigateTo('Generators', '/generators'));
        await step(testInfo, page, 'open an entry being generated', () =>
            app.followLink(String(generating), (url) => url.pathname === `/entries/${generating}`),
        );
        await step(testInfo, page, 'the output streams', async () => {
            streamOutput(generating, 'streamed by the render audit');
            await expect(page.locator('pre.code.body')).toContainText('streamed by the render audit');
        });

        await step(testInfo, page, 'open the response change', async () => {
            await page.locator('main .actions button', { hasText: 'Change response' }).click();
            await page.locator('dialog.change-response[open]').waitFor();
        });
        await step(
            testInfo,
            page,
            'type a comment for the model',
            () => app.typeInto('#change-guidance', 'camera'),
            TYPING,
        );
        await step(testInfo, page, 'switch to writing the response', async () => {
            await page.locator('dialog [role=tab]', { hasText: 'Write the response' }).click();
            await page.locator('#change-body').waitFor();
        });
        await page
            .locator('#change-body')
            .fill('<?xml version="1.0"?><userCheck><statusValue>200</statusValue></userCheck>');
        await page.locator('#change-headers').fill('Content-Type: application/xml');
        await step(testInfo, page, 'ask to replace the response', async () => {
            await page.locator('dialog.change-response button[type=submit]').click();
            await page.locator('dialog.confirm[open]').waitFor();
        });
        await step(testInfo, page, 'replace the response', async () => {
            await page.locator('dialog.confirm button[value=confirm]').click();
            await page.waitForURL(
                (url) => url.pathname.startsWith('/entries/') && url.pathname !== `/entries/${generating}`,
            );
            await app.ready();
            await expect(page.locator('h1 .badge')).toHaveText('ready');
            await expect(page.locator('pre.code.body')).toContainText('<userCheck>');
        });

        await step(testInfo, page, 'go to the clients', () => app.navigateTo('Clients', '/clients'));
        await step(testInfo, page, 'sort the clients by requests', async () => {
            await page.locator('select[name=Days]').selectOption('0');
            await app.waitForQuery('Days', '0');
            await page.locator('thead th a', { hasText: 'Requests' }).click();
            await app.waitForQuery('Sort', 'Requests');
        });
        await step(testInfo, page, 'open a client', () =>
            app.followLink('45.95.147.236', (url) => url.pathname.startsWith('/clients/45.95.147.236')),
        );
        await page.locator('#sender-name').waitFor();
        await step(testInfo, page, 'type a sender name', () => app.typeInto('#sender-name', ' audit'), TYPING);
        await step(testInfo, page, 'open the confirmation', async () => {
            await page.locator('form.send-report button').click();
            await page.locator('dialog.confirm[open]').waitFor();
        });
        await step(testInfo, page, 'cancel the confirmation', async () => {
            await page.locator('dialog.confirm button[value=cancel]').click();
            await page.locator('dialog.confirm:not([open])').waitFor({ state: 'attached' });
        });

        await step(testInfo, page, 'go to the settings', () => app.navigateTo('Settings', '/settings'));
        await step(testInfo, page, 'type into a setting', () => app.typeInto('#UserAgent', ' audit'), TYPING);
        await step(testInfo, page, 'ask to save the settings', async () => {
            await page.locator('form > fieldset.settings > button[type=submit].danger').click();
            await page.locator('dialog.confirm[open]').waitFor();
        });
        await step(testInfo, page, 'keep the settings', async () => {
            await page.locator('dialog.confirm button[value=cancel]').click();
            await page.locator('dialog.confirm:not([open])').waitFor({ state: 'attached' });
        });
    } finally {
        await app.close();
    }
});

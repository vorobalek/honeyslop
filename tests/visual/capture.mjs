import { mkdir, writeFile } from "node:fs/promises";
import { join } from "node:path";
import { chromium } from "playwright";
import { normalize, pages, stabilize, viewports } from "./pages.mjs";

const [base, out, ...only] = process.argv.slice(2);
if (!base || !out) {
  console.error("usage: node capture.mjs <base url> <output directory> [page names...]");
  process.exit(2);
}

const roles = ["anonymous", "operator"];
const browser = await chromium.launch();

async function signIn(context) {
  const page = await context.newPage();
  await page.goto(`${base}/signin?returnUrl=/`);
  await page.locator("input[name=username]").fill("operator");
  await page.locator("input[type=submit], button[type=submit]").first().click();
  await page.waitForURL(url => url.toString().startsWith(base));
  await page.close();
}

async function settle(page) {
  await page.waitForLoadState("networkidle");
  await page.locator("[data-live-status].online").waitFor({ timeout: 5000 }).catch(() => {});
  await page.waitForFunction(() => !document.body.innerText.includes("Looking it up"), null, { timeout: 15000 }).catch(() => {});
  await page.waitForLoadState("networkidle");
  await page.evaluate(() => document.fonts.ready);
}

for (const role of roles) {
  for (const [viewportName, viewport] of Object.entries(viewports)) {
    const context = await browser.newContext({ viewport, deviceScaleFactor: 1, reducedMotion: "reduce", colorScheme: "light" });
    if (role === "operator") {
      await signIn(context);
    }
    const dir = join(out, role, viewportName);
    await mkdir(dir, { recursive: true });
    for (const target of pages.filter(x => only.length === 0 || only.includes(x.name))) {
      const page = await context.newPage();
      const response = await page.goto(base + target.path);
      await settle(page);
      await page.evaluate(stabilize);
      await page.screenshot({ path: join(dir, `${target.name}.png`), fullPage: true, animations: "disabled", caret: "hide" });
      const main = await page.locator("body").evaluate(x => x.outerHTML);
      await writeFile(join(dir, `${target.name}.html`), normalize(main));
      console.log(`${role}/${viewportName}/${target.name}: ${response?.status()}`);
      await page.close();
    }
    await context.close();
  }
}

await browser.close();

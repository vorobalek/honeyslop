import { spawn } from "node:child_process";
import { writeFile } from "node:fs/promises";
import { chromium } from "playwright";

const [base, out, connection] = process.argv.slice(2);
if (!base || !out || !connection) {
  console.error("usage: node cls.mjs <base url> <output json> <connection string of the seeded database>");
  process.exit(2);
}

const seconds = 15;
const targets = [
  { name: "requests", path: "/requests" },
  { name: "client-reported", path: "/clients/176.9.38.39" },
  { name: "clients-all", path: "/clients?Days=0" },
  { name: "entries", path: "/" }
];
const scrolls = [0, 1500];

function live() {
  return new Promise((resolve, reject) => {
    const child = spawn("dotnet", ["run", "Live.cs", "--", connection, String(seconds), "700"], { cwd: new URL("./seed/", import.meta.url).pathname, stdio: ["ignore", "pipe", "inherit"] });
    let output = "";
    child.stdout.on("data", chunk => output += chunk);
    child.on("exit", code => code === 0 ? resolve(output.trim()) : reject(new Error(`Live.cs exited with ${code}`)));
  });
}

const browser = await chromium.launch();
const context = await browser.newContext({ viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1 });
const signIn = await context.newPage();
await signIn.goto(`${base}/signin?returnUrl=/`);
await signIn.locator("input[name=username]").fill("operator");
await signIn.locator("input[type=submit], button[type=submit]").first().click();
await signIn.waitForURL(url => url.toString().startsWith(base));
await signIn.close();
await context.addInitScript(() => {
  window.__shifts = [];
  new PerformanceObserver(list => {
    for (const entry of list.getEntries()) {
      if (!entry.hadRecentInput) {
        window.__shifts.push({ value: entry.value, at: entry.startTime });
      }
    }
  }).observe({ type: "layout-shift", buffered: false });
  window.__watch = () => {
    const middle = window.innerHeight / 2;
    const anchor = Array.from(document.querySelectorAll("tbody tr a[href]"))
      .find(x => x.getBoundingClientRect().top >= middle);
    const key = anchor?.getAttribute("href");
    const state = { key, start: anchor?.getBoundingClientRect().top ?? null, last: null, moved: 0, maxOffset: 0, missing: 0, frames: 0, replaced: 0, node: anchor };
    const sample = () => {
      if (!state.running) {
        return;
      }
      state.frames++;
      const current = key ? document.querySelector(`tbody tr a[href="${CSS.escape(key)}"]`) : null;
      if (!current) {
        state.missing++;
      } else {
        if (current !== state.node) {
          state.replaced++;
          state.node = current;
        }
        const top = current.getBoundingClientRect().top;
        if (state.last !== null) {
          state.moved += Math.abs(top - state.last);
        }
        state.maxOffset = Math.max(state.maxOffset, Math.abs(top - state.start));
        state.last = top;
      }
      requestAnimationFrame(sample);
    };
    state.running = true;
    requestAnimationFrame(sample);
    window.__anchor = state;
  };
});

const results = [];
for (const target of targets) {
  for (const scroll of scrolls) {
    const page = await context.newPage();
    await page.goto(base + target.path);
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(1500);
    await page.evaluate(y => window.scrollTo(0, y), scroll);
    await page.waitForTimeout(500);
    await page.evaluate(() => { window.__shifts = []; window.__scroll = window.scrollY; window.__watch(); });
    const inserted = await live();
    await page.waitForTimeout(2000);
    const result = await page.evaluate(() => {
      window.__anchor.running = false;
      const { key, moved, maxOffset, missing, frames, replaced } = window.__anchor;
      return {
        cls: window.__shifts.reduce((sum, x) => sum + x.value, 0),
        shifts: window.__shifts.length,
        scrollMoved: Math.abs(window.scrollY - window.__scroll),
        anchor: key,
        anchorMoved: Math.round(moved),
        anchorMaxOffset: Math.round(maxOffset),
        anchorMissingFrames: missing,
        anchorReplaced: replaced,
        frames
      };
    });
    results.push({ page: target.name, scroll, inserted, ...result });
    console.log(`${target.name} @${scroll}px: anchor ${result.anchor} moved ${result.anchorMoved}px in total, ${result.anchorMaxOffset}px at most, missing in ${result.anchorMissingFrames}/${result.frames} frames, rebuilt ${result.anchorReplaced} times; CLS ${result.cls.toFixed(4)} (${inserted})`);
    await page.close();
  }
}

await writeFile(out, JSON.stringify(results, null, 2));
await browser.close();

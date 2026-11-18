import { mkdir, readdir, readFile, writeFile } from "node:fs/promises";
import { dirname, join, relative } from "node:path";
import pixelmatch from "pixelmatch";
import { PNG } from "pngjs";
import { normalize } from "./pages.mjs";

const [before, after, out] = process.argv.slice(2);
if (!before || !after || !out) {
  console.error("usage: node compare.mjs <before directory> <after directory> <diff directory>");
  process.exit(2);
}

async function files(dir) {
  const entries = await readdir(dir, { recursive: true, withFileTypes: true });
  return entries.filter(x => x.isFile()).map(x => relative(dir, join(x.parentPath, x.name))).sort();
}

function crop(png, width, height) {
  const result = new PNG({ width, height });
  PNG.bitblt(png, result, 0, 0, width, height, 0, 0);
  return result;
}

const rows = [];
for (const file of (await files(before)).filter(x => x.endsWith(".png"))) {
  const a = PNG.sync.read(await readFile(join(before, file)));
  let b;
  try {
    b = PNG.sync.read(await readFile(join(after, file)));
  } catch {
    rows.push({ file, status: "missing" });
    continue;
  }
  const width = Math.min(a.width, b.width);
  const height = Math.min(a.height, b.height);
  const diff = new PNG({ width, height });
  const pixels = pixelmatch(crop(a, width, height).data, crop(b, width, height).data, diff.data, width, height, { threshold: 0.1 });
  const sized = a.width === b.width && a.height === b.height;
  const htmlFile = file.replace(/\.png$/, ".html");
  const html = await Promise.all([readFile(join(before, htmlFile), "utf8"), readFile(join(after, htmlFile), "utf8")])
    .then(([x, y]) => normalize(x) === normalize(y))
    .catch(() => null);
  if (pixels > 0 || !sized) {
    const target = join(out, file);
    await mkdir(dirname(target), { recursive: true });
    await writeFile(target, PNG.sync.write(diff));
  }
  rows.push({
    file,
    status: pixels === 0 && sized ? "same" : "different",
    pixels,
    size: sized ? `${a.width}x${a.height}` : `${a.width}x${a.height} → ${b.width}x${b.height}`,
    html: html === null ? "n/a" : html ? "same" : "different"
  });
}

const different = rows.filter(x => x.status !== "same");
for (const row of different) {
  console.log(`${row.status.padEnd(9)} ${row.file}  ${row.pixels ?? ""} px  ${row.size ?? ""}  html ${row.html ?? ""}`);
}
console.log(`${rows.length - different.length} of ${rows.length} screenshots match pixel for pixel; ${rows.filter(x => x.html === "different").length} html differ`);
await writeFile(join(out, "summary.json"), JSON.stringify(rows, null, 2)).catch(async () => {
  await mkdir(out, { recursive: true });
  await writeFile(join(out, "summary.json"), JSON.stringify(rows, null, 2));
});
process.exit(different.length === 0 ? 0 : 1);

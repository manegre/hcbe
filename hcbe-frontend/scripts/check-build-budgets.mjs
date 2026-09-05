import { readFileSync, statSync } from 'node:fs';
import { gzipSync } from 'node:zlib';
import { join } from 'node:path';

const output = join(process.cwd(), 'out');
const manifest = JSON.parse(readFileSync(join(output, '.vite', 'manifest.json'), 'utf8'));
const entries = Object.values(manifest).filter((item) => item.isEntry);
if (entries.length === 0) throw new Error('No application entry was found in the Vite manifest.');

const visited = new Set();
const initialFiles = new Set();
function collect(item) {
  if (!item || visited.has(item.file)) return;
  visited.add(item.file);
  initialFiles.add(item.file);
  for (const css of item.css ?? []) initialFiles.add(css);
  for (const imported of item.imports ?? []) collect(manifest[imported]);
}
for (const entry of entries) collect(entry);

const bytes = (file) => statSync(join(output, file)).size;
const gzipBytes = (file) => gzipSync(readFileSync(join(output, file))).length;
const allJavaScript = Object.values(manifest).map((item) => item.file).filter((file, index, files) => file.endsWith('.js') && files.indexOf(file) === index);
const initialJavaScript = [...initialFiles].filter((file) => file.endsWith('.js'));
const initialCss = [...initialFiles].filter((file) => file.endsWith('.css'));

const totals = {
  initialJsGzip: initialJavaScript.reduce((sum, file) => sum + gzipBytes(file), 0),
  initialCssGzip: initialCss.reduce((sum, file) => sum + gzipBytes(file), 0),
  largestJsGzip: Math.max(...allJavaScript.map(gzipBytes)),
};
const limits = { initialJsGzip: 240 * 1024, initialCssGzip: 60 * 1024, largestJsGzip: 130 * 1024 };
const format = (value) => `${(value / 1024).toFixed(1)} KiB`;

console.log(`Initial JavaScript: ${format(totals.initialJsGzip)} / ${format(limits.initialJsGzip)}`);
console.log(`Initial CSS: ${format(totals.initialCssGzip)} / ${format(limits.initialCssGzip)}`);
console.log(`Largest JavaScript chunk: ${format(totals.largestJsGzip)} / ${format(limits.largestJsGzip)}`);
console.log(`Initial assets: ${[...initialFiles].map((file) => `${file} (${format(bytes(file))} raw)`).join(', ')}`);

const failures = Object.entries(limits)
  .filter(([key, limit]) => totals[key] > limit)
  .map(([key, limit]) => `${key} is ${format(totals[key])}; budget is ${format(limit)}`);
if (failures.length) {
  console.error(`Performance budget exceeded:\n- ${failures.join('\n- ')}`);
  process.exit(1);
}

import { readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { chromium } from '@playwright/test';

const root = resolve(import.meta.dirname, '..');
const publicDirectory = resolve(root, 'public');
const canadaFlag = await readFile(resolve(root, 'src', 'assets', 'flags', 'canada.png'));
const canadaFlagData = `data:image/png;base64,${canadaFlag.toString('base64')}`;

const logoSvg = (maskable) => `
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <clipPath id="app-shape"><rect width="512" height="512" rx="${maskable ? 0 : 112}"/></clipPath>
    <filter id="flag-shadow" x="-20%" y="-30%" width="140%" height="160%"><feDropShadow dx="0" dy="4" stdDeviation="5" flood-opacity=".22"/></filter>
    <pattern id="grid" width="64" height="64" patternUnits="userSpaceOnUse"><path d="M64 0H0V64" fill="none" stroke="#fff" stroke-opacity=".045"/></pattern>
  </defs>
  <g clip-path="url(#app-shape)">
    <rect width="512" height="512" fill="#0d4524"/>
    <rect width="512" height="512" fill="url(#grid)"/>
    <circle cx="455" cy="48" r="118" fill="none" stroke="#ffcd00" stroke-width="36" opacity=".13"/>
    <circle cx="64" cy="486" r="100" fill="none" stroke="#fff" stroke-width="30" opacity=".04"/>
    <g transform="translate(28 222)" filter="url(#flag-shadow)">
      <rect width="68" height="44" rx="4" fill="#009e49"/>
      <path d="M0 0h68v22H0z" fill="#ef2b2d"/>
      <path d="m34 11.5 2.5 7.5h8l-6.5 4.7 2.5 7.6-6.5-4.7-6.5 4.7 2.5-7.6-6.5-4.7h8z" fill="#fcd116"/>
    </g>
    <text x="112" y="263" font-family="Arial, sans-serif" font-size="52" font-weight="800" letter-spacing="-2" fill="#fff">HCBE</text>
    <path d="m254 242 10 10-10 10-10-10z" fill="#ffcd00"/>
    <text x="274" y="263" font-family="Arial, sans-serif" font-size="39" font-weight="750" letter-spacing="-1.2" fill="#fff">Canada</text>
    <image href="${canadaFlagData}" x="420" y="226" width="72" height="36" preserveAspectRatio="xMidYMid slice" filter="url(#flag-shadow)"/>
    <rect x="112" y="292" width="288" height="12" rx="6" fill="#ffcd00"/>
  </g>
</svg>`.trim();

const sources = {
  'hcbe-app-icon.svg': logoSvg(false),
  'hcbe-app-icon-maskable.svg': logoSvg(true),
};

for (const [fileName, svg] of Object.entries(sources)) {
  await writeFile(resolve(publicDirectory, fileName), `${svg}\n`, 'utf8');
}

const icons = [
  { source: 'hcbe-app-icon.svg', target: 'hcbe-app-icon-180.png', size: 180 },
  { source: 'hcbe-app-icon.svg', target: 'hcbe-app-icon-192.png', size: 192 },
  { source: 'hcbe-app-icon.svg', target: 'hcbe-app-icon-512.png', size: 512 },
  { source: 'hcbe-app-icon-maskable.svg', target: 'hcbe-app-icon-maskable-512.png', size: 512 },
];

const browser = await chromium.launch({ headless: true });
try {
  for (const icon of icons) {
    const svg = sources[icon.source];
    const page = await browser.newPage({ viewport: { width: icon.size, height: icon.size } });
    await page.setContent(`<style>html,body{margin:0;width:100%;height:100%;overflow:hidden}svg{display:block;width:100%;height:100%}</style>${svg}`);
    await page.locator('svg').screenshot({ path: resolve(publicDirectory, icon.target), omitBackground: true });
    await page.close();
  }
} finally {
  await browser.close();
}

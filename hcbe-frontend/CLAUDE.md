# HCBE Canada — frontend

Notes a session can't derive by reading the code. Structure, dependencies and npm scripts are
already in the repo — look there instead of documenting them here.

## Traps that have cost hours

### The Tailwind config must stay `tailwind.config.js`, not `.ts`

Tailwind's PostCSS plugin does **not** load a TypeScript config in Vite's dev pipeline. It fails
silently, falls back to the default config, and every custom class stops resolving — `@apply
bg-background` in `src/index.css` then throws `class does not exist` and the dev server 500s on CSS
while `vite build` resolves the same tokens fine. The defect is invisible until something references
a custom token, so it can sit dormant for a long time. Keep the config as `.js`.

### Tailwind config changes do not hot-reload

After editing `tailwind.config.js`, restart the dev server or you will debug stale CSS. The browser
will report the *old* font stack and sizes while the file on disk shows the new ones.

### A stale Vite process can survive `pkill -f vite`

On Windows the npm-spawned node process often outlives `pkill`, keeps port 3000, and serves whatever
it cached — including an **empty module transform** for a file that was mid-write when it read it.
Symptom: the app is blank and the console says a real, present export "is not provided" by its
module. Kill it by PID and clear the cache:

```bash
netstat -ano | grep ":3000.*LISTENING"   # take the PID from the last column
taskkill //F //PID <pid>
rm -rf node_modules/.vite
```

### Locale files merge by spread — a duplicate key silently wins

`src/i18n/local/index.ts` globs every `./<lang>/*.ts` and merges them with object spread in
alphabetical order. Defining the same key in two files does not error: the alphabetically later file
overwrites the earlier one **everywhere on the site**. This has already shipped a wrong string once.
Before adding any key:

```bash
grep -rn "public.some.key" src/i18n/local/
```

Confirm zero existing definitions, then add it to **both** `fr/` and `en/` in the same commit.

## Conventions that differ from the defaults

### Imports are injected — do not add them

`unplugin-auto-import` (see `vite.config.ts`, declarations in `auto-imports.d.ts`) injects `useState`,
`useEffect`, `useMemo`, `useRef`, `useTranslation`, `Trans`, `Link`, `NavLink`, `Navigate`,
`useNavigate`, `useLocation`, `useParams` and friends at transform time. Files using them with no
import statement are **correct**. Reviewers repeatedly flag this as a missing import; it is not.

### Design tokens only

The institutional design system lives in `tailwind.config.js`. Use its tokens and nothing else:

- **Never** `emerald-*`, `amber-*`, `blue-*`, `purple-*`, `orange-*`, `teal-*`, `indigo-*`
- **Never** `bg-gradient-*`, `shadow-*`, or `rounded-(lg|xl|2xl|3xl)` — depth is 1px borders and
  `background` vs `surface` layering. `rounded-full` is allowed only on circular controls
  (carousel dots, avatars, the back-to-top button).
- Radius is **only for things you click**. `rounded-control` (6px) on buttons, inputs, selects,
  textareas, tags, status chips and icon buttons; `rounded-control-sm` (3px) on checkboxes, where
  6px on a 20px box goes blobby. Cards, panels, tables, dropdowns, media and every section stay at
  radius 0 — that squareness is what keeps the hairline grid reading as institutional, and rounding
  a card is the change that quietly turns this into a generic SaaS layout. No arbitrary radii:
  if a control needs a different value, add a token.
- `gold` (`#FFCD00`) is legible only on green grounds. On white or off-white use `gold-ink`
  (`#735C00`) — plain `gold` on light is roughly 1.6:1 and fails AA.

Two greps gate this; both must return nothing:

```bash
grep -rE "bg-gradient|shadow-(sm|md|lg|xl)|rounded-(sm|md|lg|xl|2xl|3xl|\[)" src
grep -rE "(emerald|amber|blue|purple|orange|indigo|teal)-[0-9]" src
```

### Shared UI primitives

Compose `src/components/ui/` (`Button`, `ArrowLink`, `Card`, `PageHeader`, `SectionHeading`,
`StatBar`, `Tag`, `StatusChip`, `EmptyState`, `Field`/`inputClasses`, `DataTable`/`Td`, `Reveal`) and
the admin gabarits (`AdminListPage`, `AdminFormLayout`, `AdminDetailLayout`) rather than writing new
markup. If a page needs an affordance the gabarit has no slot for, **add the slot** — do not drop the
affordance.

### All copy goes through i18n

No hard-coded French in a component. Every new string lands in both `fr/` and `en/` in the same
commit, subject to the duplicate-key rule above.

## Verification

There is **no test framework**, and none is being added. A change is verified by:

1. `npm run build` exits 0 — note this does **not** typecheck the router, so a missing export can
   take down every route while the build stays green.
2. Loading the changed routes at 390px and 1440px.
3. Checking the empty and error states **with the backend stopped** — that is the site's normal
   state today, so those branches must render deliberate bordered blocks, never bare sentences.

## Content rules

The design mocks under `stitch_hcbe_canada_institutional_portal/` (gitignored) carry invented data.
Use them for layout only:

- Delegates are **Mâ Ouédraogo Diallo** (Zone 1, suppléant Ismaël Ratouissanmda Zeba) and
  **Aziz Ismaël Daboné** (Zone 2, suppléant Ahmed Arnaud Dao).
- Contact is **contact@hcbecanada.org**, country **Canada** — not the mocks' `info@hcbecanada.org`
  or "Ottawa, ON".
- Membership is **free**. There is no dues metric; do not build one.
- The service descriptions are the HCBE's own. The mocks' passport and consular copy describes the
  embassy's remit, not this organisation's.
- Never re-add development credentials to `/admin/login`. This must stay empty:
  `grep -rn "hcbe@2025\|test@hcbe\|admin@hcbe" src/`

The hero photographs (`src/assets/hero/`) came from the design mock and are placeholders.

The partners marquee (`src/components/brand/PartnerLogos.tsx`) renders eight **fictional** wordmark
SVGs — invented companies, no real logos. Replace them with real partner marks (and confirmed
partnership agreements) before this site is published.

## Backend

The API is a separate .NET service. In dev, `getApiBaseUrl()` returns `''` and Vite proxies `/api`
and `/uploads` to `http://localhost:8080` (override with `VITE_PROXY_TARGET`). With no backend
running the site still works — every list shows its empty or error state.

## Where the design decisions live

- `docs/superpowers/specs/2026-08-18-refonte-institutionnelle-design.md` — the token system,
  component anatomy, page inventory and the rules above, with reasoning
- `docs/superpowers/plans/2026-08-18-refonte-institutionnelle.md` — the 39-task migration plan

## Repo etiquette

Commit messages are in French, Conventional Commits prefix. Work on a feature branch; `main` is the
default branch and is not committed to directly.

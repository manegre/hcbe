# Refonte institutionnelle — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the site's gradient/pill/pastel-card visual language with the approved institutional design system (sharp geometry, 1px borders, Newsreader + Public Sans, Burkina tricolore as functional UI colour) across all 20 public screens and the admin.

**Architecture:** Tokens land once in `tailwind.config.ts`, a set of primitives in `src/components/ui/` encodes every recurring pattern, then pages are migrated family by family by composing those primitives. No page is "done" while it still carries ad-hoc colour utilities, gradients, shadows or non-zero radii.

**Tech Stack:** React 19, TypeScript 5.8, Vite 7, Tailwind CSS 3.4, react-i18next, react-router-dom 7, Remix Icon (CDN). Auto-imports are on (`unplugin-auto-import`) — `useState`, `useEffect`, `Link`, `useTranslation` etc. need no import statement.

**Spec:** `docs/superpowers/specs/2026-08-18-refonte-institutionnelle-design.md`

## Global Constraints

- **No test framework exists in this repo, and adding one is out of scope.** The spec defers Vitest deliberately. Every task therefore replaces the write-failing-test cycle with the **verification cycle** defined below. Do not add test files.
- **Verification cycle** (the "test" step of every task):
  1. `npm run build` — must exit 0 with no TypeScript errors.
  2. `npm run dev`, then load each changed route at **390px** and **1440px** viewport.
  3. Compare against the mock in `stitch_hcbe_canada_institutional_portal/<screen>/screen.png` where one exists; otherwise against this plan's composition recipe.
  4. With the backend **stopped**, confirm the page's empty/error state renders deliberately (this is the site's normal state today).
- **Palette — use tokens only.** `green #14532D`, `green-deep #003B1B`, `green-dim #96D5A3`, `gold #FFCD00`, `gold-dim #F0C100`, `gold-ink #735C00`, `red #EF3340`, `red-link #C1121F`, `error #BA1A1A`, `paper #FAFAF9`, `background #F8F9FA`, `surface #FFFFFF`, `surface-container #EDEEEF`, `line #C0C9BE`, `outline #717970`, `ink #111827`, `ink-variant #404941`. No `emerald-*`, `amber-*`, `blue-*`, `purple-*`, `orange-*`, `teal-*`, `indigo-*` utility may remain in a migrated file.
- **Geometry:** radius 0 everywhere (`rounded-full` only for avatars/seals), no `shadow-*`, no `bg-gradient-*`. Depth is 1px borders and `background` vs `surface` layering.
- **Type:** Newsreader 600/700 for headlines (`font-display`), Public Sans 400/600 for everything else (`font-sans`). `label-md` (14/20, 600, uppercase, +0.05em) is the only uppercase style.
- **Copy:** every string goes through `src/i18n/local/fr/*.ts` **and** `src/i18n/local/en/*.ts` in the same commit. Never hard-code French in a component.
- **Content:** mock screens carry invented data. Delegates are **Mâ Ouédraogo Diallo** (Zone 1, suppléant Ismaël Ratouissanmda Zeba) and **Aziz Ismaël Daboné** (Zone 2, suppléant Ahmed Arnaud Dao); figures are **2 zones, 11 provinces**; contact is **contact@hcbecanada.org**, country **Canada**. Never copy `info@hcbecanada.org`, "Ottawa, ON", "10k membres", "5+ zones", "Cotisations perçues".
- **Imagery:** heroes stay flat green. Do not add photographic or AI-generated hero images.
- **Accessibility:** `red-link`/`gold-ink` for text on light grounds (never raw `red`/`gold`); focus ring `outline outline-2 outline-green outline-offset-2` on every interactive element; status never by colour alone; 44px minimum tap targets.
- **Commits:** one commit per task, French message, Conventional Commits prefix, `Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>` on the last line.

---

## File Structure

**Created**

| File | Responsibility |
|---|---|
| `src/components/ui/Button.tsx` | `Button` + `ArrowLink` — every action affordance |
| `src/components/ui/Tag.tsx` | `Tag` + `StatusChip` — bordered labels and status |
| `src/components/ui/Card.tsx` | `Card` — bordered white surface |
| `src/components/ui/PageHeader.tsx` | `PageHeader` — hero and interior page headers |
| `src/components/ui/SectionHeading.tsx` | `SectionHeading` — headline + bottom rule |
| `src/components/ui/StatBar.tsx` | `StatBar` — divided figure row |
| `src/components/ui/EmptyState.tsx` | `EmptyState` — empty and error blocks |
| `src/components/ui/Field.tsx` | `Field`, `TextInput`, `Select`, `Textarea` |
| `src/components/ui/DataTable.tsx` | `DataTable` — admin/table listings |
| `src/components/ui/index.ts` | barrel export |
| `src/components/admin/AdminListPage.tsx` | list gabarit (11 admin sections) |
| `src/components/admin/AdminFormLayout.tsx` | create/edit gabarit |
| `src/components/admin/AdminDetailLayout.tsx` | detail/review gabarit |

**Modified (major)**

`tailwind.config.ts`, `index.html`, `src/index.css`, `src/components/brand/HcbeLogo.tsx`, `src/components/feature/{Navbar,Footer,NewsletterSignup}.tsx`, `src/components/admin/Layout.tsx`, then every file under `src/pages/`.

---

# Phase 1 — Foundation

### Task 1: Tokens, fonts and base layer

**Files:**
- Modify: `tailwind.config.ts` (whole file)
- Modify: `index.html:28` (font stylesheet block)
- Modify: `src/index.css` (whole file)

**Interfaces:**
- Produces: Tailwind utilities `bg-green`, `text-red-link`, `border-line`, `font-display`, `text-headline-xl|xl-m|lg|md`, `text-body-lg|md`, `text-label-md`, `px-margin-mobile`, `px-margin-desktop`, `max-w-container`, the `.container-page` component class, and `rounded` = `0px`. Every later task consumes these.

**Two consequences to know before you start:**
- `theme.extend` **merges** with Tailwind's defaults, so `shadow-sm`, `rounded-lg` and friends still compile. They are removed by hand, task by task, and the final greps in Task 38 are what prove they are gone.
- Defining `colors.red` as an object **replaces** Tailwind's default red scale: `text-red-600` and similar stop resolving. Unmigrated pages may therefore lose a red error tint between phases. That is expected and is fixed as each page is migrated — do not re-add the default scale.

- [ ] **Step 1: Replace `tailwind.config.ts`**

```ts
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        green: { DEFAULT: '#14532D', deep: '#003B1B', dim: '#96D5A3' },
        gold: { DEFAULT: '#FFCD00', dim: '#F0C100', ink: '#735C00' },
        red: { DEFAULT: '#EF3340', link: '#C1121F' },
        error: '#BA1A1A',
        paper: '#FAFAF9',
        background: '#F8F9FA',
        surface: { DEFAULT: '#FFFFFF', container: '#EDEEEF' },
        line: '#C0C9BE',
        outline: '#717970',
        ink: { DEFAULT: '#111827', variant: '#404941' },
      },
      fontFamily: {
        display: ['Newsreader', 'Georgia', 'serif'],
        sans: ['"Public Sans"', 'Helvetica', 'Arial', 'sans-serif'],
      },
      fontSize: {
        'headline-xl': ['48px', { lineHeight: '52px', letterSpacing: '-0.02em', fontWeight: '700' }],
        'headline-xl-m': ['32px', { lineHeight: '36px', letterSpacing: '-0.01em', fontWeight: '700' }],
        'headline-lg': ['32px', { lineHeight: '40px', letterSpacing: '-0.01em', fontWeight: '600' }],
        'headline-md': ['24px', { lineHeight: '32px', fontWeight: '600' }],
        'body-lg': ['18px', { lineHeight: '28px' }],
        'body-md': ['16px', { lineHeight: '24px' }],
        'label-md': ['14px', { lineHeight: '20px', letterSpacing: '0.05em', fontWeight: '600' }],
      },
      borderRadius: { DEFAULT: '0px', none: '0px', full: '9999px' },
      boxShadow: { none: 'none' },
      spacing: { gutter: '24px', 'margin-mobile': '16px', 'margin-desktop': '64px' },
      maxWidth: { container: '1200px' },
    },
  },
  plugins: [],
};
```

- [ ] **Step 2: Swap the font links in `index.html`**

Replace the single Remix Icon `<link>` line with:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Newsreader:wght@600;700&family=Public+Sans:wght@400;600&display=swap" rel="stylesheet">
<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/remixicon@4.0.0/fonts/remixicon.css">
```

- [ ] **Step 3: Set the base layer in `src/index.css`**

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  html { -webkit-font-smoothing: antialiased; }
  body {
    @apply bg-background text-ink font-sans text-body-md;
  }
  h1, h2, h3 { @apply font-display text-green; }
  :focus-visible { @apply outline outline-2 outline-green outline-offset-2; }
}

@layer components {
  .container-page { @apply mx-auto w-full max-w-container px-margin-mobile md:px-margin-desktop; }
}
```

- [ ] **Step 4: Verify**

Run: `npm run build`
Expected: exit 0.
Then `npm run dev` and load `/` — the page still renders (unstyled in places is fine at this stage); confirm in devtools that `body` computed `font-family` is Public Sans and a `<h1>` is Newsreader.

- [ ] **Step 5: Commit**

```bash
git add tailwind.config.ts index.html src/index.css
git commit -m "feat(design): système de tokens institutionnel

Palette tricolore, échelle typographique Newsreader/Public Sans,
géométrie sans rayon ni ombre, grille 1200/64/24.

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Button and ArrowLink primitives

**Files:**
- Create: `src/components/ui/Button.tsx`
- Create: `src/components/ui/index.ts`

**Interfaces:**
- Consumes: tokens from Task 1.
- Produces:
  - `Button({ variant?: 'primary'|'secondary'|'tertiary'|'destructive', as?: 'button'|'link', to?: string, type?, disabled?, onClick?, className?, children })`
  - `ArrowLink({ to: string, tone?: 'red'|'green'|'gold'|'white', className?, children })`

- [ ] **Step 1: Write `src/components/ui/Button.tsx`**

```tsx
import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

type Variant = 'primary' | 'secondary' | 'tertiary' | 'destructive';

const variants: Record<Variant, string> = {
  primary: 'bg-gold text-green hover:bg-gold-dim border border-transparent',
  secondary: 'bg-transparent text-green border-2 border-green hover:bg-green hover:text-white',
  tertiary: 'bg-transparent text-red-link hover:text-green border-0 px-0 py-0',
  destructive: 'bg-error text-white hover:bg-[#93000A] border border-transparent',
};

interface ButtonProps {
  variant?: Variant;
  to?: string;
  href?: string;
  type?: 'button' | 'submit';
  disabled?: boolean;
  onClick?: () => void;
  className?: string;
  children: ReactNode;
}

export const Button = ({
  variant = 'primary',
  to,
  href,
  type = 'button',
  disabled = false,
  onClick,
  className = '',
  children,
}: ButtonProps) => {
  const base =
    'inline-flex min-h-[44px] items-center justify-center gap-2 px-6 py-3 text-label-md uppercase transition-colors disabled:opacity-50 disabled:pointer-events-none';
  const classes = `${base} ${variants[variant]} ${className}`;

  if (to) return <Link to={to} className={classes}>{children}</Link>;
  if (href) return <a href={href} className={classes}>{children}</a>;
  return (
    <button type={type} disabled={disabled} onClick={onClick} className={classes}>
      {children}
    </button>
  );
};

const tones = {
  red: 'text-red-link hover:text-green',
  green: 'text-green hover:text-green-deep',
  gold: 'text-gold hover:text-white',
  white: 'text-white hover:text-gold',
};

interface ArrowLinkProps {
  to: string;
  tone?: keyof typeof tones;
  className?: string;
  children: ReactNode;
}

export const ArrowLink = ({ to, tone = 'red', className = '', children }: ArrowLinkProps) => (
  <Link
    to={to}
    className={`inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase transition-colors ${tones[tone]} ${className}`}
  >
    {children}
    <i className="ri-arrow-right-line text-base" aria-hidden="true"></i>
  </Link>
);
```

- [ ] **Step 2: Create the barrel `src/components/ui/index.ts`**

```ts
export { Button, ArrowLink } from './Button';
```

- [ ] **Step 3: Verify**

Run: `npm run build` — exit 0.
Temporarily render all four variants plus an `ArrowLink` in `src/pages/home/page.tsx`, load `/` at 1440px, confirm: gold fill with green uppercase text, sharp corners, no shadow, visible focus ring on Tab. Remove the temporary render before committing.

- [ ] **Step 4: Commit**

```bash
git add src/components/ui/Button.tsx src/components/ui/index.ts
git commit -m "feat(ui): primitives Button et ArrowLink

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Tag, StatusChip, Card, SectionHeading

**Files:**
- Create: `src/components/ui/Tag.tsx`
- Create: `src/components/ui/Card.tsx`
- Create: `src/components/ui/SectionHeading.tsx`
- Modify: `src/components/ui/index.ts`

**Interfaces:**
- Produces:
  - `Tag({ children, className? })`
  - `StatusChip({ status: 'pending'|'approved'|'rejected'|'draft'|'published'|'past', label: string })`
  - `Card({ hover?: 'red'|'gold'|'green'|'none', className?, children })`
  - `SectionHeading({ title: string, description?: string, action?: ReactNode })`

- [ ] **Step 1: Write `src/components/ui/Tag.tsx`**

```tsx
import type { ReactNode } from 'react';

export const Tag = ({ children, className = '' }: { children: ReactNode; className?: string }) => (
  <span className={`inline-flex items-center border border-line px-3 py-1 text-body-md text-ink-variant ${className}`}>
    {children}
  </span>
);

const statuses = {
  pending: 'border-gold text-gold-ink',
  approved: 'border-green text-green',
  published: 'border-green text-green',
  rejected: 'border-error text-error',
  draft: 'border-outline text-ink-variant',
  past: 'border-outline text-ink-variant',
} as const;

interface StatusChipProps {
  status: keyof typeof statuses;
  label: string;
}

export const StatusChip = ({ status, label }: StatusChipProps) => (
  <span className={`inline-flex items-center border px-3 py-1 text-label-md uppercase ${statuses[status]}`}>
    {label}
  </span>
);
```

- [ ] **Step 2: Write `src/components/ui/Card.tsx`**

```tsx
import type { ReactNode } from 'react';

const hovers = {
  red: 'hover:border-red',
  gold: 'hover:border-gold',
  green: 'hover:border-green',
  none: '',
};

interface CardProps {
  hover?: keyof typeof hovers;
  className?: string;
  children: ReactNode;
}

export const Card = ({ hover = 'none', className = '', children }: CardProps) => (
  <div className={`border border-line bg-surface p-8 transition-colors ${hovers[hover]} ${className}`}>
    {children}
  </div>
);
```

- [ ] **Step 3: Write `src/components/ui/SectionHeading.tsx`**

```tsx
import type { ReactNode } from 'react';

interface SectionHeadingProps {
  title: string;
  description?: string;
  action?: ReactNode;
}

export const SectionHeading = ({ title, description, action }: SectionHeadingProps) => (
  <div className="mb-12 border-b border-line pb-4">
    <div className="flex flex-wrap items-end justify-between gap-4">
      <h2 className="font-display text-headline-lg text-green">{title}</h2>
      {action}
    </div>
    {description && <p className="mt-4 max-w-3xl text-body-md text-ink-variant">{description}</p>}
  </div>
);
```

- [ ] **Step 4: Extend the barrel**

```ts
export { Button, ArrowLink } from './Button';
export { Tag, StatusChip } from './Tag';
export { Card } from './Card';
export { SectionHeading } from './SectionHeading';
```

- [ ] **Step 5: Verify**

Run: `npm run build` — exit 0. Render one of each temporarily on `/`, check at 1440px: card border 1px `#C0C9BE`, no radius, no shadow, hover changes border colour only. Remove the temporary render.

- [ ] **Step 6: Commit**

```bash
git add src/components/ui
git commit -m "feat(ui): primitives Tag, StatusChip, Card et SectionHeading

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: PageHeader, StatBar, EmptyState

**Files:**
- Create: `src/components/ui/PageHeader.tsx`
- Create: `src/components/ui/StatBar.tsx`
- Create: `src/components/ui/EmptyState.tsx`
- Modify: `src/components/ui/index.ts`

**Interfaces:**
- Produces:
  - `PageHeader({ title, description?, variant?: 'hero'|'interior', actions?, aside? })`
  - `StatBar({ items: { value: string; label: string }[] })`
  - `EmptyState({ icon?: string, title: string, description?: string, action?, tone?: 'empty'|'error' })`

- [ ] **Step 1: Write `src/components/ui/PageHeader.tsx`**

```tsx
import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  description?: string;
  variant?: 'hero' | 'interior';
  actions?: ReactNode;
  aside?: ReactNode;
}

export const PageHeader = ({
  title,
  description,
  variant = 'interior',
  actions,
  aside,
}: PageHeaderProps) => {
  if (variant === 'hero') {
    return (
      <section className="border-b border-green bg-green py-16 md:py-24">
        <div className="container-page grid grid-cols-1 gap-gutter lg:grid-cols-12">
          <div className="lg:col-span-7">
            <h1 className="font-display text-headline-xl-m text-white md:text-headline-xl">{title}</h1>
            {description && (
              <p className="mt-6 max-w-2xl border-l-2 border-red pl-6 text-body-lg text-green-dim">
                {description}
              </p>
            )}
            {actions && <div className="mt-10 flex flex-wrap items-center gap-6">{actions}</div>}
          </div>
          {aside && <div className="lg:col-span-5 lg:col-start-8">{aside}</div>}
        </div>
      </section>
    );
  }

  return (
    <section className="border-b border-line bg-background py-12">
      <div className="container-page">
        <h1 className="font-display text-headline-lg text-green md:text-headline-xl">{title}</h1>
        {description && <p className="mt-4 max-w-3xl text-body-lg text-ink-variant">{description}</p>}
        {actions && <div className="mt-8 flex flex-wrap items-center gap-6">{actions}</div>}
      </div>
    </section>
  );
};
```

- [ ] **Step 2: Write `src/components/ui/StatBar.tsx`**

```tsx
interface StatBarProps {
  items: { value: string; label: string }[];
}

export const StatBar = ({ items }: StatBarProps) => (
  <section className="border-b border-line bg-surface">
    <div className="container-page">
      <div className="grid grid-cols-2 divide-x divide-line border-x border-line md:grid-cols-4">
        {items.map((item) => (
          <div key={item.label} className="p-6 text-center">
            <p className="font-display text-headline-md text-green">{item.value}</p>
            <p className="mt-1 text-label-md uppercase text-ink-variant">{item.label}</p>
          </div>
        ))}
      </div>
    </div>
  </section>
);
```

- [ ] **Step 3: Write `src/components/ui/EmptyState.tsx`**

```tsx
import type { ReactNode } from 'react';

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: ReactNode;
  tone?: 'empty' | 'error';
}

export const EmptyState = ({
  icon = 'ri-inbox-line',
  title,
  description,
  action,
  tone = 'empty',
}: EmptyStateProps) => (
  <div className={`border bg-surface px-6 py-16 text-center ${tone === 'error' ? 'border-error' : 'border-line'}`}>
    <span
      className={`mx-auto mb-6 flex h-14 w-14 items-center justify-center border bg-surface-container text-2xl ${
        tone === 'error' ? 'border-error text-error' : 'border-line text-ink-variant'
      }`}
    >
      <i className={icon} aria-hidden="true"></i>
    </span>
    <p className={`font-display text-headline-md ${tone === 'error' ? 'text-error' : 'text-green'}`}>{title}</p>
    {description && <p className="mx-auto mt-3 max-w-xl text-body-md text-ink-variant">{description}</p>}
    {action && <div className="mt-8 flex justify-center">{action}</div>}
  </div>
);
```

- [ ] **Step 4: Extend the barrel**

```ts
export { Button, ArrowLink } from './Button';
export { Tag, StatusChip } from './Tag';
export { Card } from './Card';
export { SectionHeading } from './SectionHeading';
export { PageHeader } from './PageHeader';
export { StatBar } from './StatBar';
export { EmptyState } from './EmptyState';
```

- [ ] **Step 5: Verify**

Run: `npm run build` — exit 0. Temporarily render `PageHeader` (hero), `StatBar` with the four real home figures, and both `EmptyState` tones on `/`; check at 390px and 1440px that the stat grid is 2 columns on mobile and 4 on desktop, and that the hero standfirst carries the 2px red left rule. Remove the temporary render.

- [ ] **Step 6: Commit**

```bash
git add src/components/ui
git commit -m "feat(ui): primitives PageHeader, StatBar et EmptyState

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Field and DataTable primitives

**Files:**
- Create: `src/components/ui/Field.tsx`
- Create: `src/components/ui/DataTable.tsx`
- Modify: `src/components/ui/index.ts`

**Interfaces:**
- Produces:
  - `Field({ label, htmlFor, required?, error?, hint?, children })`
  - `inputClasses: string` — shared class string for `<input>`, `<select>`, `<textarea>`
  - `DataTable({ columns: { key: string; label: string; align?: 'left'|'right' }[], children })` where `children` are `<tr>` rows
  - `Th`, `Td` helpers

- [ ] **Step 1: Write `src/components/ui/Field.tsx`**

```tsx
import type { ReactNode } from 'react';

export const inputClasses =
  'w-full min-h-[44px] border border-outline bg-surface px-4 py-2 text-body-md text-ink placeholder:text-ink-variant/60 focus:border-green focus:border-2 focus:outline-none';

interface FieldProps {
  label: string;
  htmlFor: string;
  required?: boolean;
  error?: string;
  hint?: string;
  children: ReactNode;
}

export const Field = ({ label, htmlFor, required, error, hint, children }: FieldProps) => (
  <div className="flex flex-col gap-2">
    <label htmlFor={htmlFor} className="text-label-md uppercase text-ink-variant">
      {label}
      {required && <span className="ml-1 text-red-link">*</span>}
    </label>
    {children}
    {hint && !error && <p className="text-body-md text-ink-variant">{hint}</p>}
    {error && <p className="text-body-md text-error">{error}</p>}
  </div>
);
```

- [ ] **Step 2: Write `src/components/ui/DataTable.tsx`**

```tsx
import type { ReactNode } from 'react';

interface Column {
  key: string;
  label: string;
  align?: 'left' | 'right';
}

interface DataTableProps {
  columns: Column[];
  children: ReactNode;
}

export const DataTable = ({ columns, children }: DataTableProps) => (
  <div className="overflow-x-auto border border-line bg-surface">
    <table className="w-full min-w-[720px] border-collapse text-left">
      <thead>
        <tr className="border-b-2 border-red bg-green text-white">
          {columns.map((column) => (
            <th
              key={column.key}
              scope="col"
              className={`px-6 py-4 text-label-md uppercase ${column.align === 'right' ? 'text-right' : 'text-left'}`}
            >
              {column.label}
            </th>
          ))}
        </tr>
      </thead>
      <tbody className="divide-y divide-line">{children}</tbody>
    </table>
  </div>
);

export const Td = ({
  children,
  align = 'left',
  className = '',
}: {
  children: ReactNode;
  align?: 'left' | 'right';
  className?: string;
}) => (
  <td className={`px-6 py-5 text-body-md text-ink-variant ${align === 'right' ? 'text-right' : ''} ${className}`}>
    {children}
  </td>
);
```

- [ ] **Step 3: Extend the barrel**

```ts
export { Button, ArrowLink } from './Button';
export { Tag, StatusChip } from './Tag';
export { Card } from './Card';
export { SectionHeading } from './SectionHeading';
export { PageHeader } from './PageHeader';
export { StatBar } from './StatBar';
export { EmptyState } from './EmptyState';
export { Field, inputClasses } from './Field';
export { DataTable, Td } from './DataTable';
```

- [ ] **Step 4: Verify**

Run: `npm run build` — exit 0. Render a `DataTable` with the four document rows from the Documents mock plus a `Field` with an error, temporarily on `/`. Confirm at 1440px: green header row, white uppercase labels, 2px red rule under the header, 1px row dividers, no zebra fill; and at 390px the table scrolls horizontally inside its own container while the page does not. Remove the temporary render.

- [ ] **Step 5: Commit**

```bash
git add src/components/ui
git commit -m "feat(ui): primitives Field et DataTable

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Tricolore wordmark

**Files:**
- Modify: `src/components/brand/HcbeLogo.tsx` (whole file)

**Interfaces:**
- Consumes: tokens.
- Produces: `HcbeLogoMark({ size?: 'sm'|'md'|'lg', className? })` — the flag-wordmark-flag lockup; `HcbeLogo({ size?, showWordmark?, subtitle?, tone?: 'light'|'dark', className? })`. The existing prop names `size`, `showWordmark`, `subtitle`, `className` are kept so no call site breaks; `titleClassName`/`subtitleClassName` are replaced by `tone`.

- [ ] **Step 1: Rewrite `src/components/brand/HcbeLogo.tsx`**

```tsx
type HcbeLogoSize = 'sm' | 'md' | 'lg';

const wordmarkSize: Record<HcbeLogoSize, string> = {
  sm: 'text-base',
  md: 'text-xl',
  lg: 'text-2xl',
};

const flagSize: Record<HcbeLogoSize, string> = {
  sm: 'h-4 w-6',
  md: 'h-5 w-8',
  lg: 'h-6 w-9',
};

const BurkinaFlag = ({ className }: { className: string }) => (
  <svg viewBox="0 0 24 16" className={className} aria-hidden="true">
    <rect width="24" height="8" fill="#EF3340" />
    <rect y="8" width="24" height="8" fill="#14532D" />
    <path d="M12 4.4l1.1 2.8 3 .2-2.3 1.9.8 2.9L12 10.5 9.4 12.2l.8-2.9L7.9 7.4l3-.2z" fill="#FFCD00" />
  </svg>
);

const CanadaFlag = ({ className }: { className: string }) => (
  <svg viewBox="0 0 24 16" className={className} aria-hidden="true">
    <rect width="24" height="16" fill="#FFFFFF" />
    <rect width="6" height="16" fill="#D52B1E" />
    <rect x="18" width="6" height="16" fill="#D52B1E" />
    <path d="M12 4l1.1 2.6 2.2-.7-1 2.4 1.5 1.1-2 .5.2 2-2-1.4-2 1.4.2-2-2-.5 1.5-1.1-1-2.4 2.2.7z" fill="#D52B1E" />
  </svg>
);

interface HcbeLogoMarkProps {
  size?: HcbeLogoSize;
  className?: string;
}

export const HcbeLogoMark = ({ size = 'md', className = '' }: HcbeLogoMarkProps) => (
  <span className={`inline-flex shrink-0 items-center gap-2 ${className}`}>
    <BurkinaFlag className={flagSize[size]} />
    <span className={`font-sans font-bold ${wordmarkSize[size]}`}>
      <span className="text-green">HC</span>
      <span className="text-gold">BE</span>
      <span className="ml-1 text-red">Canada</span>
    </span>
    <CanadaFlag className={flagSize[size]} />
  </span>
);

interface HcbeLogoProps {
  size?: HcbeLogoSize;
  showWordmark?: boolean;
  subtitle?: string;
  tone?: 'light' | 'dark';
  className?: string;
}

export const HcbeLogo = ({
  size = 'md',
  showWordmark = true,
  subtitle,
  tone = 'light',
  className = '',
}: HcbeLogoProps) => (
  <div className={`flex flex-col gap-1 ${className}`}>
    {showWordmark && <HcbeLogoMark size={size} />}
    {subtitle && (
      <span className={`text-body-md ${tone === 'dark' ? 'text-green-dim' : 'text-ink-variant'}`}>{subtitle}</span>
    )}
  </div>
);
```

- [ ] **Step 2: Fix call sites**

Run: `grep -rn "HcbeLogo" src --include=*.tsx` and remove any `titleClassName=` / `subtitleClassName=` props, replacing with `tone="dark"` where the logo sits on a green ground (footer, admin sidebar, admin login).

- [ ] **Step 3: Verify**

Run: `npm run build` — exit 0, no unused-prop errors. Load `/` and `/admin/login`; the wordmark reads `HC` green, `BE` gold, `Canada` red, flanked by both flags, no gradient chip anywhere.

- [ ] **Step 4: Commit**

```bash
git add src/components/brand/HcbeLogo.tsx src/components/feature src/components/admin src/pages
git commit -m "feat(brand): logotype tricolore sans dégradé

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Navbar and mobile panel

**Files:**
- Modify: `src/components/feature/Navbar.tsx` (whole file — keep the existing `navLinks` array and dropdown data verbatim)
- Modify: `src/components/feature/PublicLanguageSwitcher.tsx`

**Interfaces:**
- Consumes: `HcbeLogoMark`, `Button`.
- Produces: the site header consumed by every page.

- [ ] **Step 1: Restyle the desktop bar**

Keep all existing state, `navLinks`, dropdown and scroll logic. Replace only the class strings:

- `<header>`: `sticky top-0 z-50 border-b border-line bg-surface` — delete the scroll-dependent shadow/blur classes and the `isScrolled` styling branch (keep the state only if the dropdown logic uses it; otherwise delete `isScrolled` entirely).
- inner wrapper: `container-page flex h-16 items-center justify-between`
- nav link, at rest: `text-label-md uppercase text-ink-variant transition-colors hover:text-green`
- nav link, active: `text-label-md uppercase text-green border-b-[3px] border-gold pb-1`
- dropdown panel: `absolute left-0 top-full min-w-[260px] border border-line bg-surface py-2` with items `block px-4 py-3 text-body-md text-ink-variant hover:bg-surface-container hover:text-green`
- the `Devenir membre` CTA: `<Button to="/espace-membre" variant="primary">`

- [ ] **Step 2: Restyle the mobile panel**

The open mobile menu becomes a full-screen panel:

```tsx
<div className="fixed inset-0 z-50 flex flex-col bg-background lg:hidden">
  <div className="flex h-16 items-center justify-between border-b border-line bg-surface px-margin-mobile">
    <HcbeLogoMark size="sm" />
    <button type="button" onClick={() => setIsMobileMenuOpen(false)} aria-label={t('public.nav.closeMenu')} className="flex h-11 w-11 items-center justify-center">
      <i className="ri-close-line text-2xl text-ink" aria-hidden="true"></i>
    </button>
  </div>
  <nav className="flex-grow overflow-y-auto px-margin-mobile py-6">
    {/* each top-level item: */}
    <Link to={link.path} className="flex min-h-[56px] items-center border-t border-line font-display text-headline-md text-ink">
      {t(link.labelKey)}
    </Link>
    {/* sub-items indented, text-body-lg text-ink-variant, same border-t */}
  </nav>
  <div className="border-t border-line bg-surface p-margin-mobile">
    <PublicLanguageSwitcher />
    <Button to="/espace-membre" variant="primary" className="mt-4 w-full">{t('public.nav.members')}</Button>
  </div>
</div>
```

- [ ] **Step 3: Restyle `PublicLanguageSwitcher`**

Two buttons in a 1px `border-line` box, active one `bg-green text-white`, inactive `text-ink-variant`, both `text-label-md uppercase px-4 py-2 min-h-[44px]`. Remove any `rounded-full`.

- [ ] **Step 4: Verify**

Run: `npm run build` — exit 0. At 1440px: sticky bar, hairline bottom border, gold 3px underline on the active item, dropdowns square and bordered. At 390px: hamburger opens a full-screen panel, items are Newsreader, the language toggle and CTA sit at the bottom, no bottom tab bar. Tab through: focus ring visible on every item.

- [ ] **Step 5: Commit**

```bash
git add src/components/feature/Navbar.tsx src/components/feature/PublicLanguageSwitcher.tsx
git commit -m "feat(nav): en-tête institutionnel et panneau mobile plein écran

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 8: Footer and newsletter band

**Files:**
- Modify: `src/components/feature/Footer.tsx` (whole file)
- Modify: `src/components/feature/NewsletterSignup.tsx` (whole file)

**Interfaces:**
- Consumes: `HcbeLogo`, `Button`, `inputClasses`.

- [ ] **Step 1: Restyle the newsletter band**

```tsx
<section className="border-y border-line bg-green py-16">
  <div className="container-page flex flex-col gap-8 md:flex-row md:items-center md:justify-between">
    <div className="md:max-w-lg">
      <h2 className="font-display text-headline-md text-white">{t('public.newsletter.title')}</h2>
      <p className="mt-2 text-body-md text-green-dim">{t('public.newsletter.subtitle')}</p>
    </div>
    <form className="flex w-full max-w-xl flex-col gap-4 sm:flex-row">
      <input type="email" className={`${inputClasses} border-white/30 bg-white text-ink`} placeholder={t('public.newsletter.emailPlaceholder')} />
      <Button type="submit" variant="primary" className="shrink-0">{t('public.newsletter.submit')}</Button>
    </form>
  </div>
</section>
```

Keep the existing submit handler, consent checkbox and success/error messages; restyle the messages to `border border-line bg-surface p-4 text-body-md` (success) and `border border-error text-error` (error).

- [ ] **Step 2: Restyle the footer**

`<footer className="bg-green-deep text-green-dim">`, inner `container-page grid grid-cols-1 gap-gutter py-16 md:grid-cols-4`, column headings `text-label-md uppercase text-white`, links `text-body-md text-green-dim hover:text-gold min-h-[44px] flex items-center`, and a closing row `border-t border-white/20 py-6 text-body-md text-green-dim`. Logo uses `tone="dark"`. Keep the existing contact rows (`contact@hcbecanada.org`, Canada) and social links exactly as they are.

- [ ] **Step 3: Verify**

Run: `npm run build` — exit 0. Load `/` at both widths: footer is deep green, four columns on desktop, stacked with hairlines on mobile, no rounded controls, newsletter button gold. With the backend stopped, submit the newsletter form and confirm the error message renders inside a bordered block.

- [ ] **Step 4: Commit**

```bash
git add src/components/feature/Footer.tsx src/components/feature/NewsletterSignup.tsx
git commit -m "feat(footer): pied de page et bandeau infolettre institutionnels

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 2 — Accueil

**Mock: `accueil_hcbe_canada_carousel_logo_scroll/screen.png`** — chosen by the maintainer over the
`accueil_hcbe_canada_tricolore` variant. It is the same page plus two things: the hero carries a
three-slide background carousel, and a "Nos Partenaires" logo marquee sits between the hero and the
stat bar. Mobile reference stays `accueil_hcbe_canada_mobile_1/screen.png` and
`accueil_hcbe_canada_mobile_2/screen.png`.

### Task 9: Hero carousel and stat bar

**Files:**
- Create: `src/components/feature/HeroCarousel.tsx`
- Modify: `src/pages/home/components/HeroSection.tsx` (whole file)
- Modify: `src/i18n/local/fr/home.ts`, `src/i18n/local/en/home.ts`

**Interfaces:**
- Consumes: `PageHeader`, `StatBar`, `Button`, `ArrowLink` from `src/components/ui`.
- Produces: `HeroCarousel({ slides: { src: string; alt: string }[], children })` — renders the green
  hero ground with cross-fading grayscale backgrounds behind `children`.

- [ ] **Step 1: Write `src/components/feature/HeroCarousel.tsx`**

```tsx
import type { ReactNode } from 'react';

interface Slide {
  src: string;
  alt: string;
}

interface HeroCarouselProps {
  slides: Slide[];
  children: ReactNode;
}

const SLIDE_MS = 7000;

export const HeroCarousel = ({ slides, children }: HeroCarouselProps) => {
  const [active, setActive] = useState(0);
  const [paused, setPaused] = useState(false);
  const { t } = useTranslation();

  const reducedMotion =
    typeof window !== 'undefined' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  useEffect(() => {
    if (slides.length < 2 || paused || reducedMotion) return;
    const id = window.setInterval(() => setActive((i) => (i + 1) % slides.length), SLIDE_MS);
    return () => window.clearInterval(id);
  }, [slides.length, paused, reducedMotion]);

  return (
    <section
      className="relative isolate border-b border-green bg-green"
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
      onFocusCapture={() => setPaused(true)}
      onBlurCapture={() => setPaused(false)}
    >
      {slides.map((slide, index) => (
        <img
          key={slide.src}
          src={slide.src}
          alt=""
          aria-hidden="true"
          className={`pointer-events-none absolute inset-0 h-full w-full object-cover grayscale mix-blend-overlay transition-opacity duration-1000 ${
            index === active ? 'opacity-30' : 'opacity-0'
          }`}
        />
      ))}

      <div className="relative z-10">{children}</div>

      {slides.length > 1 && (
        <div className="relative z-10 flex justify-center gap-3 pb-8">
          {slides.map((slide, index) => (
            <button
              key={slide.src}
              type="button"
              onClick={() => setActive(index)}
              aria-label={t('public.home.hero.slide', { index: index + 1 })}
              aria-current={index === active}
              className="flex h-11 w-11 items-center justify-center"
            >
              <span
                className={`block h-3 w-3 rounded-full transition-colors ${
                  index === active ? 'bg-gold' : 'bg-white/50 hover:bg-white'
                }`}
              ></span>
            </button>
          ))}
        </div>
      )}
    </section>
  );
};
```

Notes that are requirements, not suggestions:
- `slides` may be **empty**. With no slides the component renders the flat green hero and no dots —
  that is the shipping state until the team supplies real photography (see Step 5).
- `prefers-reduced-motion: reduce` disables autoplay; the dots still switch slides on click.
- The dot's 44px hit area wraps a 12px visual dot — do not shrink the button.

- [ ] **Step 2: Replace the hero body in `HeroSection.tsx`**

```tsx
<HeroCarousel slides={heroSlides}>
  <PageHeader
    variant="hero"
    title={t('public.home.hero.title')}
    description={t('public.home.hero.subtitle')}
    actions={
      <>
        <Button to="/services" variant="primary">{t('public.home.hero.cta.services')}</Button>
        <ArrowLink to="/espace-membre" tone="gold">{t('public.home.hero.cta.member')}</ArrowLink>
      </>
    }
  />
</HeroCarousel>
<StatBar
  items={[
    { value: '11', label: t('public.home.stats.provinces') },
    { value: '2', label: t('public.home.stats.zones') },
    { value: '15', label: t('public.home.stats.associations') },
    { value: '—', label: t('public.home.stats.freeMembership') },
  ]}
/>
```

- [ ] **Step 2: Change the hero strings in both locale files**

FR `public.home.hero.title`: `"Haut Conseil des Burkinabè de l'Extérieur — Canada"`
EN: `"High Council of Burkinabè Abroad — Canada"`
FR `public.home.hero.subtitle`: `"Représentation officielle de la diaspora burkinabè au Canada. Services, documents officiels, associations et vie communautaire."`
EN: `"Official representation of the Burkinabè diaspora in Canada. Services, official documents, associations and community life."`

- [ ] **Step 3: Add the stat and carousel keys to `fr/home.ts` and `en/home.ts`**

```ts
'public.home.stats.provinces': 'Provinces et territoires',
'public.home.stats.zones': 'Zones de représentation',
'public.home.stats.associations': 'Associations répertoriées',
'public.home.stats.freeMembership': 'Adhésion gratuite',
'public.home.hero.slide': 'Diapositive {{index}}',
```

EN: `'public.home.hero.slide': 'Slide {{index}}'`.

- [ ] **Step 4: Declare the slide source in `HeroSection.tsx`**

```tsx
// Photographies institutionnelles du HCBE Canada.
// Vide tant que l'équipe n'a pas fourni de photos réelles: le hero reste vert uni.
// Pour activer le carrousel, déposer les images dans src/assets/hero/ et les importer ici.
const heroSlides: { src: string; alt: string }[] = [];
```

**Ship it empty.** The mock's three hero photographs are AI-generated and must not be used (Global
Constraints). The carousel is built, tested and dormant; adding real photographs to this array is the
only change needed to turn it on. Do not substitute stock imagery, and do not re-enable the mock's
image URLs.

- [ ] **Step 5: Delete the old hero**

Remove the gradient wrapper, the radial blob divs, the white bottom fade, the
`public.home.hero.badge` pill, and the whole "Par où commencer ?" card with its three numbered
steps. Leave the `public.home.hero.steps.*` keys in place — they are unused after this task and are
deleted in Task 10 together with the other orphans.

- [ ] **Step 6: Verify**

Run `npm run build` — exit 0. Load `/` at 1440px: flat green hero (no slides configured yet),
left-aligned Newsreader headline, 2px red rule left of the standfirst, gold `NOS SERVICES` button,
four divided figures beneath, and **no dot row** while `heroSlides` is empty. At 390px the headline is
32px and the stat grid is 2×2. Switch to EN — no raw keys. Then temporarily push two throwaway local
images into `heroSlides` to confirm the cross-fade, the dots, the 44px hit areas and autoplay pause on
hover — and remove them again before committing.

- [ ] **Step 7: Commit**

```bash
git add src/components/feature/HeroCarousel.tsx src/pages/home/components/HeroSection.tsx src/i18n/local
git commit -m "feat(accueil): hero carrousel institutionnel et bandeau de chiffres

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 9B: Bandeau partenaires

Numbered `9B` so tasks 10–38 keep their numbers. Mock: the "Nos Partenaires" strip in
`accueil_hcbe_canada_carousel_logo_scroll/screen.png`, between the hero and the stat bar.

**Files:**
- Create: `src/components/feature/PartnersMarquee.tsx`
- Modify: `src/pages/home/page.tsx` (section order)
- Modify: `src/i18n/local/fr/home.ts`, `src/i18n/local/en/home.ts`

**Interfaces:**
- Produces: `PartnersMarquee({ partners: { name: string; logo: string; url?: string }[] })` — renders
  nothing at all when `partners` is empty.

- [ ] **Step 1: Write `src/components/feature/PartnersMarquee.tsx`**

```tsx
interface Partner {
  name: string;
  logo: string;
  url?: string;
}

export const PartnersMarquee = ({ partners }: { partners: Partner[] }) => {
  const { t } = useTranslation();

  if (partners.length === 0) return null;

  const track = [...partners, ...partners];

  return (
    <section className="overflow-hidden border-b border-line bg-surface py-12">
      <div className="container-page mb-6 text-center">
        <h2 className="font-display text-headline-md text-green">{t('public.home.partners.title')}</h2>
      </div>
      <div className="relative flex h-20 w-full items-center overflow-hidden">
        <ul className="marquee-track flex w-[200%] items-center">
          {track.map((partner, index) => (
            <li key={`${partner.name}-${index}`} className="flex w-1/2 justify-around">
              <img
                src={partner.logo}
                alt={partner.name}
                aria-hidden={index >= partners.length}
                className="h-16 w-auto object-contain opacity-70 grayscale transition-opacity hover:opacity-100"
              />
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
};
```

- [ ] **Step 2: Add the marquee animation to `src/index.css`**

Inside the existing `@layer components` block:

```css
.marquee-track {
  animation: marquee-scroll 40s linear infinite;
}

@keyframes marquee-scroll {
  0% { transform: translateX(-50%); }
  100% { transform: translateX(0); }
}

@media (prefers-reduced-motion: reduce) {
  .marquee-track { animation: none; transform: none; }
}
```

The reduced-motion branch is a requirement, not a nicety: an infinite marquee with no way to stop it
is a WCAG 2.2.2 failure.

- [ ] **Step 3: Mount it in `src/pages/home/page.tsx`**

Between `HeroSection` and the rest, driven by an empty array with the same comment convention as the
hero slides:

```tsx
// Logos des partenaires officiels du HCBE Canada.
// Vide tant que l'équipe n'a pas fourni les logos et confirmé les partenariats:
// la section ne s'affiche pas du tout.
const partners: { name: string; logo: string; url?: string }[] = [];
```

**Ship it empty.** The mock's logo strip shows real third-party company marks the HCBE has no stated
partnership with; rendering them would misrepresent the organisation. The section stays invisible
until the team supplies logos it is entitled to display.

- [ ] **Step 4: Add the key to both locale files**

```ts
'public.home.partners.title': 'Nos partenaires',
```

EN: `'public.home.partners.title': 'Our partners'`.

- [ ] **Step 5: Verify**

`npm run build` exits 0. With `partners` empty, `/` renders no partners section and no empty gap
between hero and stat bar. Temporarily add two local placeholder images to confirm the strip scrolls
seamlessly, that the duplicated half is `aria-hidden`, and that the animation stops under
`prefers-reduced-motion: reduce` (toggle it in devtools → Rendering → Emulate CSS media feature).
Remove the placeholders before committing.

- [ ] **Step 6: Commit**

```bash
git add src/components/feature/PartnersMarquee.tsx src/pages/home/page.tsx src/index.css src/i18n/local
git commit -m "feat(accueil): bandeau partenaires défilant, masqué sans logos

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Domaines d'intervention

**Files:**
- Modify: `src/pages/home/components/MissionVisionSection.tsx` (whole file)
- Modify: `src/i18n/local/fr/home.ts`, `src/i18n/local/en/home.ts`

- [ ] **Step 1: Rebuild the section**

`<section className="bg-paper py-24">` wrapping `container-page`, a `SectionHeading` with
`title={t('public.home.mission.sectionTitle')}`, then
`grid grid-cols-1 gap-gutter md:grid-cols-3` of three cards:

```tsx
<Card hover="red">
  <span className="mb-6 flex h-12 w-12 items-center justify-center border border-red bg-surface-container text-2xl text-red">
    <i className="ri-file-text-line" aria-hidden="true"></i>
  </span>
  <h3 className="font-display text-headline-md text-ink">{t('public.home.mission.documents.title')}</h3>
  <p className="mb-8 mt-4 text-body-md text-ink-variant">{t('public.home.mission.documents.description')}</p>
  <ArrowLink to="/services/documents-officiels" tone="red">{t('public.common.learnMore')}</ArrowLink>
</Card>
```

Card two: `ri-team-line`, `border-gold`, `text-gold-ink`, `tone="gold"`, → `/services/comites`.
Card three: `ri-graduation-cap-line`, `border-green`, `text-green`, `tone="green"`, → `/services/bourses`.

- [ ] **Step 2: Repoint the copy at the real services**

Add to both locale files:

```ts
'public.home.mission.sectionTitle': "Domaines d'intervention",
'public.home.mission.documents.title': 'Documents officiels',
'public.home.mission.documents.description': 'Consultez et téléchargez les statuts, règlements et documents officiels du HCBE Canada.',
'public.home.mission.comites.title': 'Comités spécialisés',
'public.home.mission.comites.description': 'Quatre comités dédiés: juridique, ressources humaines, SONGRÉ et finances.',
'public.home.mission.bourses.title': 'Bourses et subventions',
'public.home.mission.bourses.description': 'Programmes de soutien financier pour vos projets éducatifs et entrepreneuriaux.',
```

Do **not** use the mock's copy about passports and consular cards — that is the embassy's remit,
not the HCBE's.

- [ ] **Step 3: Delete the old markup and orphaned keys**

Remove the `public.home.mission.badge` pill, the "Notre boussole" gradient band, and every
`rounded-[2rem]` / `shadow-sm` in the file. Delete from both locale files:
`mission.badge`, `mission.title`, `mission.subtitle`, `mission.welcome.*`, `mission.connect.*`,
`mission.represent.*`, `mission.compass.*`, `hero.badge`, `hero.card.*`, `hero.steps.*`.

- [ ] **Step 4: Verify**

`npm run build` exits 0. `/` at 1440px: three bordered white cards, square icon frames in
red / gold / green, uppercase arrow links, no pill above the heading; at 390px they stack.
`grep -nE "gradient|rounded-|shadow-" src/pages/home/components/MissionVisionSection.tsx` → no hits.
Switch to EN — no raw keys anywhere on `/`.

- [ ] **Step 5: Commit**

```bash
git add src/pages/home/components/MissionVisionSection.tsx src/i18n/local
git commit -m "feat(accueil): section Domaines d'intervention

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: Zones section

**Files:**
- Modify: `src/pages/home/components/ZonesSection.tsx` (whole file, keeping the `zones` data array)

- [ ] **Step 1: Keep the data, replace the presentation**

The `zones` array — names, photo imports, region lists, delegate and deputy names — stays exactly as
it is; it holds the real delegates. Delete only the `accent: 'from-emerald-600 to-emerald-800'` and
`accent: 'from-amber-500 to-orange-600'` fields and every use of them.

Shell: `<section className="bg-background py-24">` + `container-page` + `SectionHeading` with
`title={t('public.home.zones.title')}` and `description={t('public.home.zones.subtitle')}`, then
`grid grid-cols-1 gap-gutter lg:grid-cols-2` of:

```tsx
<article className="border border-line bg-surface">
  <div className="flex items-baseline justify-between border-b border-line bg-green px-6 py-4">
    <span className="text-label-md uppercase text-white">{zone.name}</span>
    <span className="text-body-md text-green-dim">
      {t('public.home.zones.territories', { count: zone.regions.length })}
    </span>
  </div>
  <div className="flex gap-6 p-6">
    <img src={zone.delegate.photo} alt={zone.delegate.name} className="h-24 w-24 shrink-0 border border-line object-cover" />
    <div>
      <p className="text-label-md uppercase text-ink-variant">{t('public.home.zones.delegate')}</p>
      <p className="font-display text-headline-md text-ink">{zone.delegate.name}</p>
      <p className="mt-2 text-body-md text-ink-variant">
        {t('public.home.zones.deputy')} · {zone.deputy.name}
      </p>
    </div>
  </div>
  <p className="mx-6 border-l-2 border-gold pl-4 text-body-md text-ink-variant">{t(zone.welcomeKey)}</p>
  <div className="mt-6 border-t border-line p-6">
    <p className="mb-3 text-label-md uppercase text-ink-variant">{t('public.home.zones.regions')}</p>
    <div className="flex flex-wrap gap-2">
      {zone.regions.map((region) => <Tag key={region}>{region}</Tag>)}
    </div>
  </div>
  <div className="border-t border-line px-6">
    <ArrowLink to="/contact" tone="red">{t('public.home.zones.cta')}</ArrowLink>
  </div>
</article>
```

- [ ] **Step 2: Verify**

`npm run build` exits 0. `/` at 1440px: two bordered cards side by side, green header strips, square
portraits, bordered region tags, no gradient and no `rounded-[2rem]`. At 390px they stack. Both real
delegates and both suppléants appear with their real names.

- [ ] **Step 3: Commit**

```bash
git add src/pages/home/components/ZonesSection.tsx
git commit -m "feat(accueil): section zones sans dégradés

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 12: Home lists and closing band

**Files:**
- Modify: `src/pages/home/components/UpcomingEventsSection.tsx`
- Modify: `src/pages/home/components/RecentAnnouncementsSection.tsx`
- Modify: `src/pages/home/components/DocumentsSection.tsx`
- Modify: `src/pages/home/components/CTASection.tsx`

- [ ] **Step 1: Convert the three list sections to editorial rows**

Each keeps its existing fetch, loading and error logic. Presentation becomes a `SectionHeading` with
`action={<ArrowLink … >}` for "tout voir", then rows:

```tsx
<article className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]">
  <p className="text-label-md uppercase text-red-link">{formattedDate}</p>
  <div>
    <h3 className="font-display text-headline-md text-ink">{item.title}</h3>
    <p className="mt-2 text-body-md text-ink-variant">{item.summary}</p>
    <ArrowLink to={itemPath} tone="red" className="mt-4">{t('public.home.events.details')}</ArrowLink>
  </div>
</article>
```

Loading skeletons become `animate-pulse bg-surface-container` blocks of the same footprint with no
radius. Empty and error branches render through `EmptyState` (`tone="error"` for failures), reusing
the existing message keys.

- [ ] **Step 2: Rebuild the closing band in `CTASection.tsx`**

```tsx
<section className="border-y border-line bg-green py-16">
  <div className="container-page flex flex-col gap-8 md:flex-row md:items-center md:justify-between">
    <div className="md:max-w-2xl">
      <p className="text-label-md uppercase text-gold">{t('public.home.cta.label')}</p>
      <h2 className="mt-3 font-display text-headline-lg text-white">{t('public.home.cta.title')}</h2>
      <p className="mt-4 text-body-md text-green-dim">{t('public.home.cta.subtitle')}</p>
    </div>
    <div className="flex flex-wrap gap-4">
      <Button to="/espace-membre" variant="primary">{t('public.home.cta.member')}</Button>
      <Button to="/contact" variant="secondary" className="border-white text-white hover:bg-white hover:text-green">
        {t('public.home.cta.contact')}
      </Button>
    </div>
  </div>
</section>
```

- [ ] **Step 3: Verify**

`npm run build` exits 0. Load `/` end to end at 1440px and 390px with the backend **stopped**: every
list shows a bordered `EmptyState` or error block, never a bare sentence; the closing band is solid
green with a gold label.
`grep -nE "gradient|rounded-(lg|xl|2xl|3xl|\[2rem\])|shadow-" src/pages/home/components/*.tsx` → no hits.

- [ ] **Step 4: Commit**

```bash
git add src/pages/home
git commit -m "feat(accueil): listes éditoriales et bandeau de clôture

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 3 — Services

Mock for Task 14: `documents_officiels_hcbe_canada_tricolore/screen.png` and
`documents_officiels_hcbe_canada_mobile/screen.png`. Tasks 13, 15 and 16 have no mock and are
composed from the primitives.

### Task 13: Services hub

**Files:**
- Modify: `src/pages/services/page.tsx`
- Modify: `src/pages/services/components/ServicesHero.tsx`
- Modify: `src/i18n/local/fr/pages.ts`, `src/i18n/local/en/pages.ts`

- [ ] **Step 1: Rebuild**

`ServicesHero` becomes:

```tsx
<PageHeader
  variant="hero"
  title={t('public.services.hero.title')}
  description={t('public.services.hero.subtitle')}
/>
```

Set FR `public.services.hero.title` to sentence case: `"Nos services d'accompagnement"`.

The three destinations become full-width rows:

```tsx
<Link to={destination.path} className="grid grid-cols-1 gap-4 border-t border-line py-8 transition-colors hover:bg-surface md:grid-cols-[1fr_auto] md:items-center">
  <div>
    <h3 className="font-display text-headline-md text-ink">{t(destination.titleKey)}</h3>
    <p className="mt-2 max-w-3xl text-body-md text-ink-variant">{t(destination.descriptionKey)}</p>
  </div>
  <span className="flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link">
    {t('public.common.discover')}
    <i className="ri-arrow-right-line" aria-hidden="true"></i>
  </span>
</Link>
```

Reuse the existing `public.services.page.cards.*` keys; add a closing `border-t border-line` after
the last row.

- [ ] **Step 2: Delete** the badge pill, the three gradient/pastel cards and their icon tiles.

- [ ] **Step 3: Verify** — `npm run build` exits 0; `/services` at 1440px and 390px shows a green
hero and three hairline-separated rows; hover tints the row surface only.

- [ ] **Step 4: Commit**

```bash
git add src/pages/services/page.tsx src/pages/services/components/ServicesHero.tsx src/i18n/local
git commit -m "feat(services): accueil de section institutionnel

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 14: Documents officiels

**Files:**
- Modify: `src/pages/services/documents-officiels/page.tsx`

- [ ] **Step 1: Header and "À retenir"**

`PageHeader variant="hero"` with the existing title/subtitle keys, actions
`<Button href="#documents" variant="primary">{t('public.services.documents.cta.view')}</Button>` and
`<ArrowLink to="/contact" tone="gold">{t('public.services.documents.cta.ask')}</ArrowLink>`.
The "À retenir" panel becomes the `aside`: a `border border-white/25` block on the green ground, three
rows separated by `border-t border-white/15`, each label `text-label-md uppercase text-gold` and text
`text-body-md text-green-dim`.

- [ ] **Step 2: Replace the document list with `DataTable`**

Columns: `NOM DU DOCUMENT`, `CATÉGORIE`, `DATE`, `FORMAT`, `ACTION` (right-aligned).

```tsx
<tr className="hover:bg-surface-container">
  <Td className="text-ink">
    <span className="flex items-center gap-3">
      <i className="ri-file-text-line text-green" aria-hidden="true"></i>
      <span className="font-semibold text-green">{doc.title}</span>
    </span>
  </Td>
  <Td>{doc.category ?? t('public.services.documents.defaultCategory')}</Td>
  <Td>{formattedDate}</Td>
  <Td>{`${doc.format} (${doc.size})`}</Td>
  <Td align="right">
    <button
      type="button"
      onClick={() => handleDownload(doc)}
      className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link hover:text-green"
    >
      {t('public.services.documents.download')}
      <i className="ri-download-line" aria-hidden="true"></i>
    </button>
  </Td>
</tr>
```

Keep the existing download handler, search field and category filter; restyle both controls with
`inputClasses` inside a `flex flex-wrap items-center justify-between gap-4 border-y border-line py-6` row.

- [ ] **Step 3: States**

Empty → `EmptyState` with `emptyTitle` / `emptyText`. Load failure → `EmptyState tone="error"` with
`errorLoad`. Download failure keeps its inline message, restyled
`border border-error p-4 text-body-md text-error`.

- [ ] **Step 4: Verify**

`npm run build` exits 0. Compare `/services/documents-officiels` at 1440px against the mock: green
table header, 2px red rule beneath it, hairline rows. **The page ground must be `#F8F9FA` with legible
headings — the mock's black ground is a rendering artifact and must not be reproduced.** At 390px the
table scrolls inside its own container while the page does not. With the backend stopped, the error
block renders.

- [ ] **Step 5: Commit**

```bash
git add src/pages/services/documents-officiels/page.tsx
git commit -m "feat(services): bibliothèque de documents en tableau institutionnel

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 15: Comités spécialisés

**Files:**
- Modify: `src/pages/services/comites/page.tsx`
- Modify: `src/pages/services/components/ComitesSection.tsx`

- [ ] **Step 1: Rebuild**

`PageHeader variant="hero"` with `public.services.comites.title` / `subtitle`. Each committee:

```tsx
<article className="border border-line bg-surface p-8">
  <div className="flex flex-wrap items-center justify-between gap-4 border-b border-line pb-4">
    <h3 className="font-display text-headline-md text-green">{committee.name}</h3>
    <Tag>{committee.scope}</Tag>
  </div>
  <p className="mt-6 max-w-3xl text-body-md text-ink-variant">{committee.mission}</p>
  <ul className="mt-6 grid grid-cols-1 gap-2 md:grid-cols-2">
    {committee.topics.map((topic) => (
      <li key={topic} className="flex items-start gap-3 text-body-md text-ink-variant">
        <i className="ri-check-line mt-1 text-green" aria-hidden="true"></i>
        {topic}
      </li>
    ))}
  </ul>
  <ArrowLink to="/contact" tone="red" className="mt-6">{t('public.common.writeToUs')}</ArrowLink>
</article>
```

Keep whatever committee data the files already hold. Do not invent committees beyond the four named
in the copy: juridique, ressources humaines, SONGRÉ, finances.

- [ ] **Step 2: Delete** every pill badge, pastel icon tile, gradient and shadow in both files.

- [ ] **Step 3: Verify** — build exits 0; `/services/comites` at both widths shows four bordered
blocks with hairline heads; no colour outside the palette.

- [ ] **Step 4: Commit**

```bash
git add src/pages/services/comites/page.tsx src/pages/services/components/ComitesSection.tsx
git commit -m "feat(services): page comités institutionnelle

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Bourses et subventions

**Files:**
- Modify: `src/pages/services/bourses/page.tsx`
- Modify: `src/pages/services/components/BoursesSection.tsx`

- [ ] **Step 1: Rebuild**

`PageHeader variant="hero"` with `public.grants.heroTitle` / `heroSubtitle`, actions
`public.grants.cta.view` (primary) and `public.grants.cta.ask` (gold arrow link), and the three
`public.grants.remember.*` notes as the aside panel, same treatment as Task 14.

```tsx
<article className="border border-line bg-surface p-8">
  <h3 className="font-display text-headline-md text-ink">{grant.title}</h3>
  <p className="mt-4 max-w-3xl text-body-md text-ink-variant">{grant.description}</p>
  <dl className="mt-6 grid grid-cols-1 divide-y divide-line border-y border-line sm:grid-cols-2 sm:divide-x sm:divide-y-0">
    <div className="py-4 sm:pr-6">
      <dt className="text-label-md uppercase text-ink-variant">{t('public.grants.amount')}</dt>
      <dd className="font-display text-headline-md tabular-nums text-green">{grant.amount}</dd>
    </div>
    <div className="py-4 sm:pl-6">
      <dt className="text-label-md uppercase text-ink-variant">{t('public.grants.duration')}</dt>
      <dd className="font-display text-headline-md tabular-nums text-green">{grant.duration}</dd>
    </div>
  </dl>
  <p className="mt-6 text-label-md uppercase text-ink-variant">{t('public.grants.criteriaTitle')}</p>
  <ul className="mt-3 space-y-2">
    {grant.criteria.map((criterion) => (
      <li key={criterion} className="flex items-start gap-3 text-body-md text-ink-variant">
        <i className="ri-check-line mt-1 text-green" aria-hidden="true"></i>
        {criterion}
      </li>
    ))}
  </ul>
  <Button href={grant.applyUrl} variant="primary" className="mt-8">{t('public.grants.apply')}</Button>
</article>
```

- [ ] **Step 2: States** — empty → `EmptyState` with `emptyTitle` / `emptyText`; failure →
`EmptyState tone="error"` with `errorLoad`. Keep the `helpTitle` / `helpText` closing band, restyled
as a green band with `helpContact` and `helpBack` buttons.

- [ ] **Step 3: Verify** — build exits 0; `/services/bourses` at both widths; amounts and durations
render tabular; with the backend stopped the empty block renders inside a border.

- [ ] **Step 4: Commit**

```bash
git add src/pages/services/bourses/page.tsx src/pages/services/components/BoursesSection.tsx
git commit -m "feat(services): page bourses institutionnelle

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 4 — Actualités

Mock for Tasks 17–18: `actualit_s_et_v_nements_hcbe_canada_tricolore/screen.png` and
`actualit_s_hcbe_canada_mobile/screen.png`.

### Task 17: Actualités hub

**Files:**
- Modify: `src/pages/actualites/page.tsx`
- Modify: `src/pages/actualites/components/ActualitesHero.tsx`
- Modify: `src/pages/actualites/components/AgendaSection.tsx`
- Modify: `src/pages/actualites/components/AnnoncesExemples.tsx`
- Modify: `src/pages/actualites/components/GalerieSection.tsx`
- Modify: `src/i18n/local/fr/pages.ts`, `src/i18n/local/en/pages.ts`

- [ ] **Step 1: Replace the photographic hero**

`PageHeader variant="hero"` with `public.news.hero.title` (set FR to sentence case
`"Restez informé et connecté"`) and `public.news.hero.subtitle`; actions
`public.news.hero.cta.events` (primary) and `public.news.hero.cta.announcements` (gold arrow link).
**Delete the AI-generated hero image and the now-unused `public.news.hero.imageAlt` key from both
locale files.**

- [ ] **Step 2: Destinations as rows** — the three cards become hairline rows (Task 13 recipe)
pointing at `/actualites/evenements`, `/actualites/annonces`, `/actualites/souvenirs`, reusing
`public.news.page.cards.*`.

- [ ] **Step 3: Previews** — `AgendaSection` and `AnnoncesExemples` become two columns of editorial
rows (date · title · one line), each ending in an `ArrowLink`. `GalerieSection` becomes a
`grid grid-cols-1 gap-gutter md:grid-cols-2` photo grid with captions **below** the image on the page
ground — never overlaid, no scrim.

- [ ] **Step 4: Verify** — build exits 0; `/actualites` at both widths; no photographic hero; with the
backend stopped both preview columns show `EmptyState`.

- [ ] **Step 5: Commit**

```bash
git add src/pages/actualites/page.tsx src/pages/actualites/components src/i18n/local
git commit -m "feat(actualites): accueil de section sans image générée

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 18: Événements — liste

**Files:**
- Modify: `src/pages/actualites/evenements/page.tsx`

- [ ] **Step 1: Filters**

Replace the pill filters with a text-tab row: wrapper `border-b border-line`, each tab
`min-h-[44px] px-1 pb-3 text-label-md uppercase`, active
`text-green border-b-[3px] border-gold`, inactive `text-ink-variant hover:text-green`. Keep the three
existing keys `filter.current`, `filter.past`, `filter.all`.

- [ ] **Step 2: Rows**

```tsx
<article className="grid grid-cols-1 gap-6 border-b border-line py-10 md:grid-cols-[96px_200px_1fr]">
  <div>
    <p className="font-display text-headline-lg tabular-nums text-red-link">{day}</p>
    <p className="text-label-md uppercase text-ink-variant">{monthYear}</p>
  </div>
  <img src={coverUrl} alt="" className="h-[140px] w-full border border-line object-cover" />
  <div>
    <p className="flex items-center gap-2 text-label-md uppercase text-ink-variant">
      <i className="ri-map-pin-line text-gold-ink" aria-hidden="true"></i>
      {event.location}
    </p>
    <h3 className="mt-2 font-display text-headline-md text-green">{event.title}</h3>
    <p className="mt-3 max-w-3xl text-body-md text-ink-variant">{event.summary}</p>
    <div className="mt-4 flex flex-wrap items-center gap-4">
      <StatusChip status={chipStatus} label={t(statusKey)} />
      <ArrowLink to={`/actualites/evenements/${event.id}`} tone="red">
        {t('public.news.evenements.cta.details')}
      </ArrowLink>
    </div>
  </div>
</article>
```

Map the existing status values to `StatusChip` statuses: upcoming → `pending`, ongoing → `approved`,
past → `past`, virtual keeps its own `Tag`.

- [ ] **Step 3: States** — keep both existing branches, rendered through `EmptyState`
(`empty.title` plus the filter-specific description) and `EmptyState tone="error"`
(`error.unavailable` / `error.load`).

- [ ] **Step 4: Verify** — build exits 0; compare `/actualites/evenements` at 1440px against the mock
(large red day figure, gold pin icon, hairline rows); at 390px the date sits above the cover and
nothing scrolls sideways; backend stopped → empty state.

- [ ] **Step 5: Commit**

```bash
git add src/pages/actualites/evenements/page.tsx
git commit -m "feat(evenements): liste éditoriale et onglets soulignés

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 19: Événement — détail

**Files:**
- Modify: `src/pages/actualites/evenements/[id]/page.tsx`
- Modify: `src/components/events/EventMediaGallery.tsx`
- Modify: `src/components/media/ImageCarousel.tsx`

- [ ] **Step 1: Head** — a back link (`<i className="ri-arrow-left-line">` before the label,
`text-label-md uppercase text-green`, `min-h-[44px]`), then the title in
`font-display text-headline-xl-m md:text-headline-xl text-green`, a `StatusChip`, and a metadata row:

```tsx
<dl className="mt-8 grid grid-cols-1 divide-y divide-line border-y border-line sm:grid-cols-4 sm:divide-x sm:divide-y-0">
  {/* date/heure, lieu, type, organisateur — dt text-label-md uppercase text-ink-variant, dd text-body-md text-ink */}
</dl>
```

- [ ] **Step 2: Body** — cover image full content width with `border border-line`, no radius. Two
columns: prose left in `max-w-[65ch] text-body-md text-ink-variant`; a sticky
`border border-line bg-surface p-6` panel right repeating the practical details with the
`cta.register` primary button.

- [ ] **Step 3: Attachments and gallery** — attachments as hairline rows (name, format, size,
`Télécharger` arrow link) under an `SectionHeading`-style label using
`public.news.evenements.attachments`. Gallery grid `grid grid-cols-2 gap-4 md:grid-cols-4`, thumbnails
`border border-line`. Lightbox chrome: `bg-ink/95`, counter in `text-label-md uppercase text-white`,
square controls `border border-white/40` at ≥44px, close top-right. **Keep every existing keyboard
handler, index calculation and video branch untouched — this task changes classes only.**

- [ ] **Step 4: Past-event notice** — `border-l-2 border-gold bg-surface p-4 text-body-md text-ink-variant`
carrying `public.news.evenements.pastNotice`.

- [ ] **Step 5: Verify** — build exits 0; with the backend stopped confirm the not-found branch renders
through `EmptyState`; at 390px the sticky panel becomes a normal block and the gallery is 2 columns;
lightbox arrows, counter and Escape still work.

- [ ] **Step 6: Commit**

```bash
git add src/pages/actualites/evenements src/components/events src/components/media
git commit -m "feat(evenements): fiche détail et visionneuse institutionnelles

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 20: Annonces — liste

**Files:**
- Modify: `src/pages/actualites/annonces/page.tsx`

- [ ] **Step 1:** `PageHeader variant="hero"` with `public.news.annonces.title` / `subtitle`; the
`remember.*` block as the aside.

- [ ] **Step 2: Category filter** — buttons in a `flex flex-wrap gap-2` row, each
`min-h-[44px] border px-4 py-2 text-label-md uppercase`; active
`border-green bg-green text-white`; inactive `border-line text-ink-variant hover:border-green`.
Label the row with `filter.label` in `text-label-md uppercase text-ink-variant`.

- [ ] **Step 3: Entries** — hairline rows: date + category in `text-label-md uppercase text-ink-variant`;
pinned items carry `<StatusChip status="pending" label={t('public.news.annonces.pinned')} />`;
title `font-display text-headline-md text-green`; two-line summary; thumbnail right
`h-[110px] w-[160px] border border-line object-cover`; `ArrowLink` reading `readMore`.

- [ ] **Step 4: States** — `EmptyState` with `emptyCategory` / `emptyCategoryHint`;
`EmptyState tone="error"` with `errorLoad`. Keep the `cta.title` / `cta.description` closing band on green.

- [ ] **Step 5: Verify** — build exits 0; `/actualites/annonces` at both widths; pinned entries sort
first and are visibly marked; backend stopped → error block.

- [ ] **Step 6: Commit**

```bash
git add src/pages/actualites/annonces/page.tsx
git commit -m "feat(annonces): liste institutionnelle avec filtres bordés

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 21: Annonce — détail

**Files:**
- Modify: `src/pages/actualites/annonces/[id]/page.tsx`

- [ ] **Step 1:** Back link, then category + date in `text-label-md uppercase text-ink-variant`, then
the headline in `font-display text-headline-xl-m md:text-headline-xl text-green`.

- [ ] **Step 2:** Cover image full width with `border border-line`; body a single `max-w-[65ch]`
column; `attachments` and `photos` blocks reuse Task 19's treatment exactly, including the same
lightbox chrome and the `photos.counter` string.

- [ ] **Step 3:** Closing action row — `askQuestion` as `Button variant="secondary"` and `viewEvents`
as an `ArrowLink`. The not-found branch renders `EmptyState` with `notFound` and a `backToList` link.

- [ ] **Step 4: Verify** — build exits 0; the prose column measures ≤65ch at 1440px; at 390px images
fill the 16px-margin width; the not-found branch renders.

- [ ] **Step 5: Commit**

```bash
git add src/pages/actualites/annonces
git commit -m "feat(annonces): fiche communiqué en colonne mesurée

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 22: Souvenirs

**Files:**
- Modify: `src/pages/actualites/souvenirs/page.tsx`

- [ ] **Step 1:** `PageHeader variant="hero"` with `souvenirs.title` / `subtitle`; the `archivesIntro`
line under an `archivesBadge` label in `text-label-md uppercase text-gold-ink`.

- [ ] **Step 2: Albums** — `grid grid-cols-1 gap-gutter md:grid-cols-2`; each album an image with
`border border-line object-cover`, and **below the image on the page ground** the event title in
`font-display text-headline-md text-ink`, the date, and the counts as
`{photoCount} · {videoCount}` in `text-label-md uppercase text-ink-variant`. No overlaid text, no scrim.

- [ ] **Step 3: Lightbox** — chrome identical to Task 19; keep the existing video handling and the
`gallery.openExternal` affordance.

- [ ] **Step 4: States** — `EmptyState` with `empty.title` / `empty.description`;
`EmptyState tone="error"` with `empty.error`. Keep the `share.title` / `share.description` band,
restyled as a green closing band carrying the contact address.

- [ ] **Step 5: Verify** — build exits 0; `/actualites/souvenirs` at both widths; captions sit outside
images; backend stopped → empty block.

- [ ] **Step 6: Commit**

```bash
git add src/pages/actualites/souvenirs/page.tsx
git commit -m "feat(souvenirs): galeries avec légendes hors image

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 5 — Engagement

No mocks. Compose from the primitives.

### Task 23: Engagement hub

**Files:**
- Modify: `src/pages/engagement/page.tsx`
- Modify: `src/pages/engagement/components/EngagementHero.tsx`
- Modify: `src/i18n/local/fr/pages.ts`, `src/i18n/local/en/pages.ts`

- [ ] **Step 1:** `PageHeader variant="hero"` with `public.engagement.hero.title` (set FR to sentence
case `"Ensemble, construisons l'avenir"`) and `hero.subtitle`. Delete the photographic hero background.

- [ ] **Step 2:** Replace the three gradient cards with hairline rows (Task 13 recipe), each carrying
its figure — `cards.associations.stats`, `cards.projects.stats`, `cards.consultations.stats` — as a
`text-label-md uppercase text-gold-ink` line under the description, and the existing `features.*`
bullets as a `flex flex-wrap gap-x-6 gap-y-2` row of `ri-check-line` items.

- [ ] **Step 3:** Closing band = the green CTA recipe from Task 12 using `public.engagement.page.cta.*`.

- [ ] **Step 4: Verify** — build exits 0; `/engagement` at both widths;
`grep -nE "purple|blue|orange|from-" src/pages/engagement/page.tsx src/pages/engagement/components/EngagementHero.tsx` → no hits.

- [ ] **Step 5: Commit**

```bash
git add src/pages/engagement/page.tsx src/pages/engagement/components/EngagementHero.tsx src/i18n/local
git commit -m "feat(engagement): accueil de section sans cartes multicolores

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 24: Annuaire des associations

**Files:**
- Modify: `src/pages/engagement/annuaire/page.tsx`
- Modify: `src/pages/engagement/components/AnnuaireSection.tsx`

- [ ] **Step 1:** `PageHeader variant="interior"` with `annuaire.title` / `subtitle`.

- [ ] **Step 2: Filter bar** — search `input` and province `select` both using `inputClasses`, plus a
result count in `text-label-md uppercase tabular-nums text-ink-variant`, inside
`flex flex-col gap-4 border-y border-line py-6 md:flex-row md:items-center md:justify-between`.

- [ ] **Step 3: Cards** — `grid grid-cols-1 gap-gutter md:grid-cols-2` of `<Card hover="green">`:
name in `font-display text-headline-md text-green`; city/province, `founded` and `members` strings in
`text-body-md text-ink-variant`; domain `Tag`s; then a contact row of three `min-h-[44px]` links with
`ri-mail-line`, `ri-phone-line`, `ri-external-link-line`, all `text-label-md uppercase text-red-link`
using the existing `contactEmail` / `contactPhone` / `visitWebsite` keys.

- [ ] **Step 4: States** — `EmptyState` with `emptyTitle` plus `emptyAll` or `emptyFilter`;
`EmptyState tone="error"` with `errorLoad`. **This page currently shows a bare red sentence on
failure; it must become a bordered block.**

- [ ] **Step 5: Verify** — build exits 0; `/engagement/annuaire` with the backend stopped shows the
bordered error block; at 390px cards stack and every contact link is ≥44px.

- [ ] **Step 6: Commit**

```bash
git add src/pages/engagement/annuaire/page.tsx src/pages/engagement/components/AnnuaireSection.tsx
git commit -m "feat(annuaire): fiches associations bordées et états explicites

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 25: Projets — liste

**Files:**
- Modify: `src/pages/engagement/projets/page.tsx`
- Modify: `src/pages/engagement/components/ProjetsSection.tsx`

- [ ] **Step 1:** `PageHeader variant="interior"` with `projets.title` / `subtitle`; type and status
filter chips using the Task 20 chip recipe and the existing `type.*` / `status.*` keys.

- [ ] **Step 2: Rows**

```tsx
<article className="grid grid-cols-1 gap-6 border-b border-line py-10 md:grid-cols-[200px_1fr]">
  <img src={project.imageUrl} alt="" className="h-[140px] w-full border border-line object-cover" />
  <div>
    <div className="flex flex-wrap items-center gap-3">
      <StatusChip status={statusMap[project.status]} label={t(`public.engagement.projets.status.${project.status}`)} />
      <Tag>{t(`public.engagement.projets.type.${project.type}`)}</Tag>
    </div>
    <h3 className="mt-3 font-display text-headline-md text-green">{project.title}</h3>
    <p className="mt-2 max-w-3xl text-body-md text-ink-variant">{project.description}</p>
    <dl className="mt-6 grid grid-cols-2 gap-6 border-t border-line pt-6 md:grid-cols-4">
      {/* budget, raised, beneficiaries, period:
          dt text-label-md uppercase text-ink-variant
          dd font-display text-headline-md tabular-nums text-green */}
    </dl>
    <div className="mt-6 flex items-center gap-4">
      <div className="h-2 flex-grow border border-line bg-surface-container">
        <div className="h-full bg-green" style={{ width: `${project.progress}%` }}></div>
      </div>
      <span className="text-label-md tabular-nums text-green">{project.progress}%</span>
    </div>
  </div>
</article>
```

- [ ] **Step 3: States** — `EmptyState tone="error"` with `errorLoad`; empty list → `EmptyState`.

- [ ] **Step 4: Verify** — build exits 0; progress bars are square with a 1px border; every figure is
tabular; both widths clean.

- [ ] **Step 5: Commit**

```bash
git add src/pages/engagement/projets/page.tsx src/pages/engagement/components/ProjetsSection.tsx
git commit -m "feat(projets): liste avec chiffres tabulaires et barres carrées

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 26: Projet — détail

**Files:**
- Modify: `src/pages/projet/page.tsx`

- [ ] **Step 1:** Back link (`projets.back`), title, `StatusChip`, `Tag`, cover image `border border-line`.

- [ ] **Step 2:** Two columns. Left: `descriptionTitle` prose in `max-w-[65ch]`; `keyFigures` as a
`grid grid-cols-2 divide-x divide-y divide-line border border-line md:grid-cols-4` of figure cells;
`timeline` as a `border-y border-line py-6` row carrying `start` and `end` on a 1px horizontal rule;
`partners` as `Tag`s. Right: a sticky `border border-line bg-surface p-6` panel with the progress bar,
`raised` against `budget`, and `contributeCta` as a full-width primary button.

- [ ] **Step 3: States** — loading uses `animate-pulse bg-surface-container` blocks with the `loading`
string; not-found uses `EmptyState` with `notFound` plus a back link.

- [ ] **Step 4: Verify** — build exits 0; both widths; the sticky panel becomes a normal block below 1024px.

- [ ] **Step 5: Commit**

```bash
git add src/pages/projet/page.tsx
git commit -m "feat(projets): fiche détail institutionnelle

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 27: Consultations

**Files:**
- Modify: `src/pages/engagement/consultations/page.tsx`
- Modify: `src/pages/engagement/components/ConsultationsSection.tsx`
- Modify: `src/pages/engagement/components/BenevolatsSection.tsx`

- [ ] **Step 1:** `PageHeader variant="hero"` with `consultations.title` / `subtitle`.

- [ ] **Step 2:** Each consultation a `border border-line bg-surface p-8` block: title, open/closed
`StatusChip`, closing date, description, participation count in `tabular-nums`, and either a
`Participer` primary button or a `Voir les résultats` arrow link — add both strings to the FR and EN
locale files as `public.engagement.consultations.participate` and
`public.engagement.consultations.viewResults`.

- [ ] **Step 3: States** — `EmptyState` with `consultations.empty`; `EmptyState tone="error"` with
`errorLoad`. Closing band uses `ctaTitle` / `ctaSubtitle` / `ctaContact` / `ctaEvents` on green.

- [ ] **Step 4: Verify** — build exits 0; both widths; backend stopped → empty block; EN renders with
no raw keys.

- [ ] **Step 5: Commit**

```bash
git add src/pages/engagement/consultations/page.tsx src/pages/engagement/components src/i18n/local
git commit -m "feat(consultations): blocs bordés et bandeau de clôture

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 6 — Reste du site public

Mock for Task 28: `devenir_membre_hcbe_canada_tricolore/screen.png` and
`devenir_membre_hcbe_canada_mobile/screen.png`.

### Task 28: Espace membre

**Files:**
- Modify: `src/pages/espace-membre/page.tsx`
- Modify: `src/pages/espace-membre/components/MemberLoginForm.tsx`

- [ ] **Step 1:** `PageHeader variant="hero"` with `public.member.hero.title` / `subtitle` and the
`hero.card.*` note as the aside panel.

- [ ] **Step 2: Two columns**

Left: `advantages.title` under an `advantages.label` kicker, then the eight `advantages.items.*` as
hairline rows (`flex items-start gap-3 border-t border-line py-4` with `ri-check-line text-green`),
then the `help.*` block as `border border-line bg-surface p-6`.

Right: the form inside `border border-line bg-surface p-8`; the `form.label` kicker, `form.title`,
`form.intro`; groups `sections.contact` and `sections.professional` introduced by
`text-label-md uppercase text-ink-variant border-b border-line pb-3`; every input wrapped in `Field`
with `inputClasses`; the motivation textarea keeping its `charCount` counter; the `form.consent` line;
a full-width primary `form.submit.label`.

- [ ] **Step 3: States** — success `border border-green bg-surface p-6` with `form.success.*`; error
`border border-error bg-surface p-6` with `form.error.*`. `MemberLoginForm` gets the same `Field`
treatment and stays behind the `memberLoginEnabled` flag — **do not enable it**.

- [ ] **Step 4: Verify** — build exits 0; compare `/espace-membre` at 1440px against the mock; at 390px
fields are full width and ≥44px; submitting with the backend stopped shows the bordered error block.

- [ ] **Step 5: Commit**

```bash
git add src/pages/espace-membre
git commit -m "feat(membre): formulaire d'adhésion institutionnel

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 29: Contact

**Files:**
- Modify: `src/pages/contact/page.tsx`

- [ ] **Step 1:** `PageHeader variant="hero"` with `public.contact.hero.title` / `subtitle` and the
`hero.card.*` note as aside.

- [ ] **Step 2:** Left column: the form in a bordered panel, every control through `Field` +
`inputClasses`; the subject `select` keeps its seven existing option keys; the message textarea keeps
the counter and the `validation.messageTooLong` rule. Right column: three stacked
`border border-line bg-surface p-6` panels — `coordinates` (contact@hcbecanada.org, Canada),
`links` (the three external links with `ri-external-link-line`), `social`.

- [ ] **Step 3: States** — success and error blocks exactly as Task 28.

- [ ] **Step 4: Verify** — build exits 0; both widths; no placeholder phone number appears anywhere;
backend stopped → bordered error.

- [ ] **Step 5: Commit**

```bash
git add src/pages/contact/page.tsx
git commit -m "feat(contact): formulaire et panneaux institutionnels

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 30: Confidentialité and 404

**Files:**
- Modify: `src/pages/confidentialite/page.tsx`
- Modify: `src/pages/NotFound.tsx`

- [ ] **Step 1: Confidentialité** — a single `max-w-[65ch]` column; title
`font-display text-headline-xl text-green`; a "Dernière mise à jour" line in
`text-label-md uppercase text-ink-variant`; a numbered table of contents as a
`border border-line bg-surface p-6` list of anchor links; sections separated by
`mt-8 border-t border-line pt-8` with `font-display text-headline-md` subheads. Keep the existing
sections and text verbatim.

- [ ] **Step 2: 404** — keep header and footer; content left-aligned inside `container-page py-24`:
`text-label-md uppercase text-red-link` reading `Erreur 404`,
`font-display text-headline-xl text-green` with `public.notFound.title`, `public.notFound.subtitle`
beneath, a primary `public.notFound.cta` button, then a hairline list of four destinations
(services, actualités, annuaire, contact). No oversized decorative numeral.

- [ ] **Step 3: Verify** — build exits 0; `/confidentialite` and a nonsense URL at both widths; the
table-of-contents anchors jump correctly.

- [ ] **Step 4: Commit**

```bash
git add src/pages/confidentialite/page.tsx src/pages/NotFound.tsx
git commit -m "feat(public): pages confidentialité et 404 institutionnelles

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

# Phase 7 — Administration

Mock for Task 33: `administration_hcbe_canada_tricolore/screen.png`.

### Task 31: Admin login

**Files:**
- Modify: `src/pages/admin/login/page.tsx`

- [ ] **Step 1:** Two-panel split. Left `bg-green` panel: back link to the public site, kicker in
`text-label-md uppercase text-gold`, title in `font-display text-headline-xl text-white`, subtitle in
`text-green-dim`, and the "accès réservé" note inside `border border-white/25 p-6`. Right panel
`bg-background` with a centred `border border-line bg-surface p-8` card carrying `HcbeLogoMark`, the
title, two `Field`s and a full-width primary submit.

- [ ] **Step 2: Remove the development credentials block** currently rendered on the page
("Identifiants locaux (développement)"). It must not appear in any environment.

- [ ] **Step 3:** Auth failure renders `border border-error p-4 text-body-md text-error` above the
fields, using the existing error string.

- [ ] **Step 4: Verify** — build exits 0; `/admin/login` at both widths; searching the DOM for
`hcbe@2025` returns nothing; a failed sign-in shows the bordered error.

- [ ] **Step 5: Commit**

```bash
git add src/pages/admin/login/page.tsx
git commit -m "feat(admin): connexion institutionnelle sans identifiants affichés

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 32: Admin shell

**Files:**
- Modify: `src/components/admin/Layout.tsx`
- Modify: `src/components/admin/LanguageSwitcher.tsx`
- Modify: `src/components/admin/AdminBackButton.tsx`
- Modify: `src/components/admin/AdminLanguageTabs.tsx`

- [ ] **Step 1: Sidebar** — `w-[260px] bg-green text-green-dim`; `HcbeLogoMark size="sm"` plus
`Administration` in `font-display text-headline-md text-white` at the top. Nav items
`flex min-h-[48px] items-center gap-3 px-6 text-body-md`; active
`border-l-[3px] border-gold bg-green-deep text-white`; inactive `hover:text-white`. Group the existing
links under `text-label-md uppercase text-green-dim/70` headings: **Contenu** (tableau de bord,
actualités, événements, annonces, documents), **Communauté** (associations, projets, bourses,
consultations), **Membres** (liste des membres, demandes d'adhésion, infolettre),
**Administration** (utilisateurs, équipe). `Déconnexion` pinned at the bottom above `border-t border-white/20`.

- [ ] **Step 2: Top bar** — `h-16 border-b border-line bg-surface`, section name in
`font-display text-headline-md text-green`, then `LanguageSwitcher` and the administrator's name with
a square avatar (`h-9 w-9 border border-green bg-green text-white`).

- [ ] **Step 3: Tabs and back button** — `AdminLanguageTabs` becomes the underlined-tab pattern from
Task 18 (`FR` / `EN`, active `border-b-[3px] border-gold text-green`), with an
incomplete-translation marker as an `h-2 w-2 bg-gold` square after the label. `AdminBackButton`
becomes a left-arrow link in `text-label-md uppercase text-green`, `min-h-[44px]`.

- [ ] **Step 4: Verify** — build exits 0; sign in and load `/admin/dashboard`: sidebar groups render,
the active item carries the gold rule; at 390px the sidebar collapses behind a toggle and the content
stays readable.

- [ ] **Step 5: Commit**

```bash
git add src/components/admin
git commit -m "feat(admin): coque d'administration institutionnelle

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 33: Admin dashboard

**Files:**
- Modify: `src/pages/admin/dashboard/page.tsx`

- [ ] **Step 1: Metric tiles** — five `border border-line bg-surface p-6` cells in
`grid grid-cols-1 gap-gutter sm:grid-cols-2 xl:grid-cols-5`: label
`text-label-md uppercase text-ink-variant`, figure
`font-display text-headline-xl tabular-nums text-green`, sub-line `text-body-md text-ink-variant`.
Keep the five real metrics (`upcomingEvents`, `pendingApplications`, `members`, `publishedNews`,
`activeProjects`). **Do not add a "Cotisations perçues" tile — it does not exist and membership is free.**

- [ ] **Step 2: Inbox** — pending applications through `DataTable` (Nom, Date, Statut, Action) with a
`StatusChip` and an `Ouvrir` arrow link; empty → `EmptyState` with `inbox.empty`.

- [ ] **Step 3: Quick actions** — `flex flex-wrap gap-4` of `Button variant="secondary"` using the five
existing `createEvent`, `createNews`, `createProject`, `createDocument`, `reviewApplications` strings.

- [ ] **Step 4: Partial-failure band** — `border-l-2 border-gold bg-surface p-4` carrying
`partialError` and `partialErrorSources`.

- [ ] **Step 5: Verify** — build exits 0; compare `/admin/dashboard` at 1440px against the mock; with
the backend stopped both the partial-failure band and the empty inbox render.

- [ ] **Step 6: Commit**

```bash
git add src/pages/admin/dashboard/page.tsx
git commit -m "feat(admin): tableau de bord dense et tuiles tabulaires

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 34: List gabarit

**Files:**
- Create: `src/components/admin/AdminListPage.tsx`
- Modify: `src/pages/admin/events/page.tsx`
- Modify: `src/pages/admin/news/page.tsx`

**Interfaces:**
- Produces: `AdminListPage({ title, count?, createLabel?, createPath?, toolbar?, columns, children, isEmpty, emptyTitle, emptyDescription?, error? })` — `children` are `<tr>` rows.

- [ ] **Step 1: Write the gabarit**

```tsx
import type { ReactNode } from 'react';
import { Button, DataTable, EmptyState } from '../ui';

interface AdminListPageProps {
  title: string;
  count?: number;
  createLabel?: string;
  createPath?: string;
  toolbar?: ReactNode;
  columns: { key: string; label: string; align?: 'left' | 'right' }[];
  children: ReactNode;
  isEmpty: boolean;
  emptyTitle: string;
  emptyDescription?: string;
  error?: string;
}

export const AdminListPage = ({
  title,
  count,
  createLabel,
  createPath,
  toolbar,
  columns,
  children,
  isEmpty,
  emptyTitle,
  emptyDescription,
  error,
}: AdminListPageProps) => (
  <section className="flex flex-col gap-6">
    <div className="flex flex-wrap items-end justify-between gap-4 border-b border-line pb-4">
      <div>
        <h1 className="font-display text-headline-lg text-green">{title}</h1>
        {typeof count === 'number' && (
          <p className="mt-1 text-label-md uppercase tabular-nums text-ink-variant">{count}</p>
        )}
      </div>
      {createLabel && createPath && (
        <Button to={createPath} variant="primary">{createLabel}</Button>
      )}
    </div>

    {toolbar && <div className="flex flex-wrap items-center gap-4 border-b border-line pb-6">{toolbar}</div>}

    {error ? (
      <EmptyState tone="error" title={error} />
    ) : isEmpty ? (
      <EmptyState
        title={emptyTitle}
        description={emptyDescription}
        action={createLabel && createPath ? <Button to={createPath} variant="secondary">{createLabel}</Button> : undefined}
      />
    ) : (
      <DataTable columns={columns}>{children}</DataTable>
    )}
  </section>
);
```

- [ ] **Step 2: Apply it to events and news**, keeping every existing fetch, filter, delete handler and
confirmation dialog. Row actions become `min-h-[44px]` icon buttons: `ri-eye-line` (green),
`ri-edit-line` (green), `ri-delete-bin-line` (`text-error`). The delete confirmation dialog becomes
`border border-line bg-surface p-8` with a `Button variant="destructive"`.

- [ ] **Step 3: Verify** — build exits 0; `/admin/events` and `/admin/news` render the green-headed
table; with the backend stopped both show the error block.

- [ ] **Step 4: Commit**

```bash
git add src/components/admin/AdminListPage.tsx src/pages/admin/events/page.tsx src/pages/admin/news/page.tsx
git commit -m "feat(admin): gabarit de liste appliqué aux événements et actualités

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 35: List gabarit — remaining sections

**Files:**
- Modify: `src/pages/admin/associations/page.tsx`, `src/pages/admin/projects/page.tsx`,
  `src/pages/admin/documents/page.tsx`, `src/pages/admin/grants/page.tsx`,
  `src/pages/admin/consultations/page.tsx`, `src/pages/admin/members/page.tsx`,
  `src/pages/admin/membership-applications/page.tsx`, `src/pages/admin/team-members/page.tsx`,
  `src/pages/admin/users/page.tsx`

- [ ] **Step 1:** Convert each to `AdminListPage`, one file at a time, keeping its own columns, filters
and handlers. Do not unify their column sets — each section keeps the fields it already shows.

- [ ] **Step 2: Verify** — build exits 0; load all nine routes; each shows the green table head and,
with the backend stopped, its error block.
`grep -rE "(emerald|amber|blue|purple|orange|indigo|teal)-[0-9]" src/pages/admin/{associations,projects,documents,grants,consultations,members,membership-applications,team-members,users}/page.tsx` → no hits.

- [ ] **Step 3: Commit**

```bash
git add src/pages/admin
git commit -m "feat(admin): gabarit de liste sur les neuf sections restantes

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 36: Form gabarit

**Files:**
- Create: `src/components/admin/AdminFormLayout.tsx`
- Modify: `src/components/forms/EventForm.tsx`
- Modify: `src/pages/admin/news/NewsForm.tsx`
- Modify: `src/pages/admin/associations/AssociationForm.tsx`

**Interfaces:**
- Produces: `AdminFormLayout({ title, backPath, backLabel, languageTabs?, actions, main, aside?, isDirty?, dirtyLabel?, onCancel?, onSave? })`.

- [ ] **Step 1: Write the gabarit** — head `border-b border-line pb-4` carrying the back link,
`font-display text-headline-lg text-green` title and a right-aligned action cluster; `languageTabs`
directly beneath; body `grid grid-cols-1 gap-gutter lg:grid-cols-[1fr_320px]`; each aside panel
`border border-line bg-surface p-6`; and, when `isDirty`, a
`fixed inset-x-0 bottom-0 z-40 border-t border-line bg-surface p-4` bar carrying `dirtyLabel` and two
buttons (`secondary` cancel, `primary` save).

- [ ] **Step 2: Apply to the three form components** — every control through `Field` + `inputClasses`;
fieldset headings `text-label-md uppercase text-ink-variant border-b border-line pb-3`; required
markers `text-red-link`; validation messages `text-error`. **Keep all existing state, validation and
submit logic.** `EventGalleryManager` and `EventAttachmentsManager` move into the aside with
`border border-line` treatment and square thumbnails.

- [ ] **Step 3: Verify** — build exits 0; open `/admin/events/create` and `/admin/news/create`: the
FR/EN tabs switch content, a required field left empty shows its message, and the unsaved bar appears
after an edit.

- [ ] **Step 4: Commit**

```bash
git add src/components/admin/AdminFormLayout.tsx src/components/forms/EventForm.tsx src/pages/admin/news/NewsForm.tsx src/pages/admin/associations/AssociationForm.tsx
git commit -m "feat(admin): gabarit de formulaire bilingue

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 37: Form gabarit — remaining create/edit pages

**Files:**
- Modify: every `create/page.tsx`, `edit/page.tsx` and `[id]/edit/page.tsx` under `src/pages/admin/`
  not already covered: projects, grants, consultations, members, team-members, users, documents,
  and the events/associations create+edit wrappers.

- [ ] **Step 1:** Convert each to `AdminFormLayout`, one section at a time, keeping its fields and
handlers.

- [ ] **Step 2: Verify** — build exits 0; open each create route; every field is `Field`-wrapped and
≥44px; `grep -rE "(emerald|amber|blue|purple|orange)-[0-9]" src/pages/admin` → no hits.

- [ ] **Step 3: Commit**

```bash
git add src/pages/admin
git commit -m "feat(admin): gabarit de formulaire sur les sections restantes

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

### Task 38: Detail gabarit, membership review and newsletter

**Files:**
- Create: `src/components/admin/AdminDetailLayout.tsx`
- Modify: `src/pages/admin/membership-applications/[id]/page.tsx`
- Modify: every remaining admin detail page — `view/page.tsx` and `[id]/page.tsx` under events, news,
  associations, projects, grants, consultations, members, team-members
- Modify: `src/pages/admin/newsletter/page.tsx`

**Interfaces:**
- Produces: `AdminDetailLayout({ title, backPath, backLabel, status?, main, aside? })`.

- [ ] **Step 1: Write the gabarit** — head with back link, title and optional `StatusChip`; body two
columns; `main` renders a definition list `divide-y divide-line border-y border-line` with `dt` in
`text-label-md uppercase text-ink-variant` and `dd` in `text-body-md text-ink`.

- [ ] **Step 2: Membership review** — decision panel in the aside: `Approuver la demande` primary,
`Refuser` destructive, a `Motif` textarea shown when refusing, and the activity trail as hairline rows
(received, reviewed by, decided, each with a timestamp). Approval confirmation dialog
`border border-line bg-surface p-8` naming the applicant, with `Annuler` and `Approuver` buttons.

- [ ] **Step 3: Newsletter** — summary figures as four `border border-line p-6` cells, subscribers
through `DataTable`, `Exporter la liste` as a secondary button, a `Préparer un envoi` panel in the
aside, empty → `EmptyState`.

- [ ] **Step 4: Verify** — build exits 0; open each detail route; with the backend stopped every one
shows a bordered empty or error block rather than a blank panel. Then run the final sweep:

```bash
grep -rE "bg-gradient|shadow-(sm|md|lg|xl)|rounded-(lg|xl|2xl|3xl|\[2rem\])" src
grep -rE "(emerald|amber|blue|purple|orange|indigo|teal)-[0-9]" src
```

Both must return no hits.

- [ ] **Step 5: Commit**

```bash
git add src/components/admin/AdminDetailLayout.tsx src/pages/admin
git commit -m "feat(admin): gabarit de fiche, revue des demandes et infolettre

Co-Authored-By: Claude Opus 5 (1M context) <noreply@anthropic.com>"
```

---

## Done criteria

- `npm run build` exits 0.
- `grep -rE "bg-gradient|shadow-(sm|md|lg|xl)|rounded-(lg|xl|2xl|3xl|\[2rem\])" src` → no hits.
- `grep -rE "(emerald|amber|blue|purple|orange|indigo|teal)-[0-9]" src` → no hits.
- Every public and admin route renders its empty or error state inside a bordered block with the
  backend stopped.
- FR and EN both render with no raw translation keys on every migrated page.
- No development credentials appear anywhere in the built output.

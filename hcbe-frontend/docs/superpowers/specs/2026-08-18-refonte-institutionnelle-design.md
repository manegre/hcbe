# Design: Refonte institutionnelle du site public et de l'admin

**Date:** 2026-08-18
**Status:** Approved — planning

## Goal

Replace the current visual language — gradient heroes, pill badges, pastel icon-tile cards,
multicoloured section cards, centred Title Case headlines — with an institutional design system
that reads as an official body of the Burkinabè state in Canada.

Direction settled with the maintainer: **Institutional Minimalism, tricolore as designed**
(source: Stitch export `stitch_hcbe_canada_institutional_portal/l_honneur_et_la_patrie/DESIGN.md`
and 12 exported screens). Sharp geometry, no shadows, 1px structural borders, editorial serif
headlines, national colours used as functional UI colours.

Scope is presentation only. No route, API, data-model or i18n-key changes beyond strings added
for genuinely new UI (empty states, labels).

## Decisions

| Question | Decision |
|---|---|
| Visual register | Institutional and authoritative |
| Colour policy | Full tricolore: gold primary buttons, red links/dates/rules, green grounds |
| Admin semantics | Separate `error` red (`#BA1A1A`) for destructive actions so status colour stays unambiguous |
| Icons | Keep Remix Icon (352 usages); swapping icon sets is out of scope |
| Copy | Keeps flowing through `src/i18n/local/{fr,en}` — new strings added to both |
| Mock content | Not ported. The Stitch screens carry invented data (see Content rules) |
| Tests | No framework in repo; verification is build + browser check (see Verification) |

## Token system

Defined once in `tailwind.config.ts` under `theme.extend`, which is currently empty. Every colour
below replaces ad-hoc `emerald-*` / `amber-*` / `blue-*` / `purple-*` utilities.

### Colour

| Token | Hex | Role |
|---|---|---|
| `green` | `#14532D` | primary ground, section blocks, primary borders, headings |
| `green-deep` | `#003B1B` | footer ground, hover on green surfaces |
| `green-dim` | `#96D5A3` | body text on green grounds |
| `gold` | `#FFCD00` | primary button fill, active-tab underline (3px), star marks |
| `gold-dim` | `#F0C100` | gold hover |
| `gold-ink` | `#735C00` | gold-family text on light grounds (AA-safe) |
| `red` | `#EF3340` | flag mark, accent rules, card hover borders |
| `red-link` | `#C1121F` | link text, event dates — darkened from `#EF3340` for AA |
| `error` | `#BA1A1A` | destructive actions and error states (admin) |
| `paper` | `#FAFAF9` | stationery ground |
| `background` | `#F8F9FA` | page ground |
| `surface` | `#FFFFFF` | card and panel ground |
| `surface-container` | `#EDEEEF` | inset blocks, icon frames |
| `line` | `#C0C9BE` | 1px rules and borders |
| `outline` | `#717970` | stronger borders, form outlines at rest |
| `ink` | `#111827` | body text |
| `ink-variant` | `#404941` | secondary text, labels |

Chromatic colour is used as 1px structural border far more than as large fill; solid green blocks
are reserved for hero, newsletter band and footer.

### Type

Two families, loaded from Google Fonts in `index.html` (replacing the current Remix-Icon-only link
block — the Remix Icon stylesheet stays):

- **Newsreader** 600/700 — headlines
- **Public Sans** 400/600 — body, labels, all UI

| Scale | Size / line-height | Tracking |
|---|---|---|
| `headline-xl` | 48 / 52 (mobile 32 / 36) | -0.02em |
| `headline-lg` | 32 / 40 | -0.01em |
| `headline-md` | 24 / 32 | — |
| `body-lg` | 18 / 28 | — |
| `body-md` | 16 / 24 | — |
| `label-md` | 14 / 20, 600, uppercase | +0.05em |

All text left-aligned by default. Uppercase is reserved for `label-md`.

### Geometry

- `borderRadius.DEFAULT = 0`. Sharp corners on every control, field, card and chip.
  `rounded-full` survives only for avatars and seals.
- `boxShadow: none`. Depth comes from 1px borders and tonal layering (`background` vs `surface`).
- Grid: 1200px max container, 64px desktop margin, 16px mobile margin, 24px gutter, 4px base unit.
- Section rhythm: 48–64px between sections.

## Component primitives

New directory `src/components/ui/`. These replace repeated ad-hoc markup rather than sitting
beside it; a page is not "migrated" until it consumes them.

| Component | Anatomy |
|---|---|
| `Button` | `primary`: gold fill, green text, uppercase `label-md`, 24×12 padding. `secondary`: 2px green border, transparent. `tertiary`: uppercase text + trailing arrow, no box. `destructive`: `error` fill, white text |
| `ArrowLink` | uppercase `label-md`, colour by context (`red-link` public, `green` admin), trailing chevron |
| `Card` | `surface` ground, 1px `line` border, 32px padding, accent border on hover |
| `PageHeader` | headline + standfirst; on green ground for hero pages, on `background` with a bottom rule for interior pages |
| `SectionHeading` | `headline-lg` in green with a 1px bottom rule and 16px padding beneath |
| `StatBar` | equal columns divided by 1px rules, figure in `headline-md` green over uppercase `label-md` |
| `Tag` | 1px border, transparent fill, `label-md`, no radius |
| `StatusChip` | 1px border + text in the status colour: gold `En attente`, green `Approuvé`/`Publié`, `error` `Refusé`, `outline` `Brouillon` |
| `DataTable` | green header row with white uppercase labels and a 2px red top rule; 1px row dividers; no zebra fill |
| `EmptyState` | centred icon in a `surface-container` frame, `headline-md` green title, `body-md` explanation |
| `Field` | label above in `label-md`, 1px `outline` border, 2px `green` on focus |
| `Lightbox` | reuses existing gallery logic; chrome restyled only |

Global chrome, rebuilt on the primitives:

- **Navbar** — sticky, `surface` ground, 1px bottom rule, 64px tall. Wordmark is the tricolore
  lockup: Burkina flag, `HC` green / `BE` gold / `Canada` red, Canada flag. Active nav item carries
  a 3px gold bottom border. `Devenir membre` as a primary button. FR|EN toggle as `label-md`.
- **Footer** — `green-deep` ground, four columns (organisation, Navigation, Contact, Suivez-nous),
  1px rule above the copyright line.
- **Mobile nav** — full-screen panel, large left-aligned Newsreader items, 1px separators, language
  toggle and primary action pinned at the bottom. No bottom tab bar.

## Page inventory

Twenty public screens and the admin. Mocked screens are built against their PNG; unmocked ones are
extrapolated from this token system and the primitives.

| Page | Route | Mock |
|---|---|---|
| Accueil | `/` | yes — `accueil_hcbe_canada_carousel_logo_scroll` (desktop, retenue par le mainteneur) + 2 mobile |
| Services — hub | `/services` | no |
| Documents officiels | `/services/documents-officiels` | yes (desktop + mobile) |
| Comités spécialisés | `/services/comites` | no |
| Bourses et subventions | `/services/bourses` | no |
| Actualités — hub | `/actualites` | yes (desktop + mobile) |
| Événements — liste | `/actualites/evenements` | partial (in Actualités) |
| Événement — détail | `/actualites/evenements/:id` | no |
| Annonces — liste | `/actualites/annonces` | no |
| Annonce — détail | `/actualites/annonces/:id` | no |
| Souvenirs | `/actualites/souvenirs` | no |
| Engagement — hub | `/engagement` | no |
| Annuaire | `/engagement/annuaire` | no |
| Projets — liste | `/engagement/projets` | no |
| Projet — détail | `/projet/:id` | no |
| Consultations | `/engagement/consultations` | no |
| Espace membre | `/espace-membre` | yes (desktop + mobile) |
| Contact | `/contact` | no |
| Confidentialité | `/confidentialite` | no |
| Page introuvable | 404 | no |
| Admin — connexion | `/admin/login` | no |
| Admin — tableau de bord | `/admin/dashboard` | yes |
| Admin — liste ×11 | `/admin/{section}` | no (one gabarit) |
| Admin — formulaire | `/admin/{section}/create·edit` | no (one gabarit) |
| Admin — détail / revue | `/admin/{section}/:id` | no (one gabarit) |

The admin has thirteen sections. Eleven of them (actualités, événements, annonces, documents,
associations, projets, bourses, consultations, membres, utilisateurs, équipe) are pure
list + form + detail; `demandes d'adhésion` adds the review screen; `infolettre` is bespoke.
Building the gabarits as primitives (`AdminListPage`, `AdminFormLayout`, `AdminDetailLayout`) is
what makes phase 7 tractable.

## Migration phases

Each phase is an independent commit; the site stays shippable between them.

1. **Foundation** — tokens in `tailwind.config.ts`, fonts in `index.html`, `src/components/ui/`
   primitives, Navbar, Footer, mobile nav.
2. **Accueil** — hero, stat bar, `Domaines d'intervention`, zones section, newsletter band.
3. **Services** — hub, documents officiels, comités, bourses.
4. **Actualités** — hub, événements list + detail, annonces list + detail, souvenirs.
5. **Engagement** — hub, annuaire, projets list + detail, consultations.
6. **Reste public** — espace membre, contact, confidentialité, 404.
7. **Admin** — shell + dashboard, then the three gabarits applied across the thirteen sections.

## Content rules

The Stitch screens carry invented content. It is reference for **layout only**; every value comes
from the codebase or the API.

- Delegates are **Mâ Ouédraogo Diallo** (Zone 1, suppléant Ismaël Ratouissanmda Zeba) and
  **Aziz Ismaël Daboné** (Zone 2, suppléant Ahmed Arnaud Dao) — not the mock's names.
- Figures are 2 zones and 11 provinces/territories — not the mock's "5+ zones, 13 provinces,
  10k membres".
- Contact is `contact@hcbecanada.org`, country Canada — not `info@hcbecanada.org` / "Ottawa, ON".
- Membership is free; the admin's "Cotisations perçues" tile does not exist.
- Hero imagery stays a flat green ground. The mock's hero photograph is AI-generated; a
  photographic hero waits for real photography from the team.
- `15+ associations` and `8 projets actifs` are currently hard-coded strings. They stay as they are
  in this work, and are flagged for a follow-up that reads them from the API.
- The retained home mock (`accueil_hcbe_canada_carousel_logo_scroll`) adds a three-slide hero carousel
  and a partners logo marquee. Both are **built and shipped empty**: the carousel's slide array and the
  marquee's partner array start `[]`, so the hero stays flat green and the partners section does not
  render. The mock's hero photographs are AI-generated, and its partner strip shows third-party company
  marks the HCBE has no stated partnership with — neither may be used. The team supplies real
  photography and real partner logos to turn each on.

## Accessibility

- Every token pair used for text meets WCAG AA (4.5:1 body, 3:1 large). `red-link` `#C1121F` and
  `gold-ink` `#735C00` exist specifically because the raw flag colours fail on light grounds.
- Gold on green and white on green are verified for the hero and newsletter bands.
- Status is never carried by colour alone — every `StatusChip` carries its label.
- Focus states are visible on every interactive element: 2px green outline, offset 2px.
- Tap targets stay at 44px minimum on mobile.

## Verification

The repo has no test framework and no lint script, so each phase is verified by:

1. `npm run build` completes without errors.
2. The changed pages are loaded at 390px and 1440px and compared against their mock (or, for
   unmocked pages, against the primitives and this spec).
3. Empty and error states are checked with the backend stopped — these are the states the site is
   in today and they must look deliberate.
4. Token pairs used for new text are contrast-checked when introduced.

Adding Vitest + Testing Library was raised and deliberately deferred; it is separate work.

## Out of scope

- Icon set migration (Remix Icon stays)
- Route, API or data-model changes
- Replacing hard-coded figures with API counts
- Photography sourcing
- Automated test coverage
- The `l_honneur_et_la_patrie` export directory itself, which stays as design reference

## Risks

- **Volume.** 1,124 hard-coded colour utilities across 109 `.tsx` files, plus 225 `rounded-full`,
  155 large radii, 138 shadows and 58 gradients. The phase split exists to keep each diff
  reviewable; a mechanical sweep across all files at once is explicitly rejected.
- **Unmocked pages drifting.** Fifteen public screens and three admin gabarits have no mock. The
  primitives are the guard: a page that only composes primitives cannot drift far.
- **Bilingual copy.** Every new string lands in both `fr` and `en` files in the same commit, or the
  English site silently shows keys.

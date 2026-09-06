import { useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { getAdminPermissionLabel } from '../../../lib/adminPermissions';
import { helpArticles, helpCategories, type HelpCategoryId, type HelpLocale } from './content';

const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();

const copy = {
  fr: {
    eyebrow: 'Manuel opérationnel', title: 'Centre d’aide administrateur',
    subtitle: 'Trouvez une procédure, comprenez un module et accédez directement au bon outil.',
    searchLabel: 'Rechercher dans la documentation', searchPlaceholder: 'Ex. : remboursement, événement, supprimer mes données…',
    all: 'Tous les sujets', results: 'résultats', result: 'résultat', clear: 'Effacer la recherche',
    noResult: 'Aucun guide ne correspond à votre recherche.', noResultHint: 'Essayez un terme plus général ou retirez le filtre.',
    open: 'Ouvrir cette fonctionnalité', steps: 'Procédure recommandée', tips: 'Points de vigilance',
    permission: 'Accès requis', updated: 'Documentation intégrée', updatedHint: 'Conçue pour accompagner les opérations quotidiennes du HCBE.',
    select: 'Sélectionnez un guide pour consulter la procédure.', sensitive: 'Avant une action sensible',
    sensitiveText: 'Vérifiez l’identité, le destinataire et l’impact. Ne partagez jamais de mot de passe, code OTP ou clé API.',
  },
  en: {
    eyebrow: 'Operations handbook', title: 'Administrator help centre',
    subtitle: 'Find a procedure, understand a module, and go directly to the right tool.',
    searchLabel: 'Search the documentation', searchPlaceholder: 'E.g. refund, event, delete my data…',
    all: 'All topics', results: 'results', result: 'result', clear: 'Clear search',
    noResult: 'No guide matches your search.', noResultHint: 'Try a broader term or remove the filter.',
    open: 'Open this feature', steps: 'Recommended procedure', tips: 'Things to watch',
    permission: 'Required permission', updated: 'Built-in documentation', updatedHint: 'Designed to support HCBE’s day-to-day operations.',
    select: 'Select a guide to view its procedure.', sensitive: 'Before a sensitive action',
    sensitiveText: 'Verify the identity, recipient, and impact. Never share a password, OTP, or API key.',
  },
} as const;

const AdminHelpPage = () => {
  const { i18n } = useTranslation();
  const locale: HelpLocale = i18n.resolvedLanguage?.startsWith('en') ? 'en' : 'fr';
  const c = copy[locale];
  const [searchParams, setSearchParams] = useSearchParams();
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState<HelpCategoryId | 'all'>('all');
  const requestedArticle = searchParams.get('article');
  const selectedId = helpArticles.some((article) => article.id === requestedArticle) ? requestedArticle! : helpArticles[0].id;

  const selectArticle = (articleId: string) => {
    const next = new URLSearchParams(searchParams);
    next.set('article', articleId);
    setSearchParams(next, { replace: true });
  };

  const filtered = useMemo(() => {
    const needle = normalize(query.trim());
    return helpArticles.filter((article) => {
      if (category !== 'all' && article.category !== category) return false;
      if (!needle) return true;
      const haystack = [
        article.title[locale], article.summary[locale], article.permission ?? '',
        ...article.steps[locale], ...article.tips[locale], ...article.keywords[locale],
      ].join(' ');
      return normalize(haystack).includes(needle);
    });
  }, [category, locale, query]);

  const selected = filtered.find((article) => article.id === selectedId) ?? filtered[0] ?? null;

  return (
    <div className="space-y-6 pb-10" data-testid="admin-help-page">
      <header className="relative overflow-hidden rounded-[22px] bg-green-deep px-5 py-7 text-white shadow-[0_22px_50px_rgba(0,59,27,.16)] sm:px-8 sm:py-9">
        <div className="pointer-events-none absolute -right-16 -top-24 h-72 w-72 rounded-full border-[48px] border-gold/[0.09]" aria-hidden="true" />
        <div className="pointer-events-none absolute inset-0 opacity-[0.08] [background-image:linear-gradient(to_right,currentColor_1px,transparent_1px),linear-gradient(to_bottom,currentColor_1px,transparent_1px)] [background-size:44px_44px]" aria-hidden="true" />
        <div className="relative grid gap-7 lg:grid-cols-[minmax(0,1fr)_310px] lg:items-end">
          <div>
            <p className="flex items-center gap-3 text-[10px] font-bold uppercase tracking-[0.22em] text-gold">
              <span className="h-px w-8 bg-gold" aria-hidden="true" />{c.eyebrow}
            </p>
            <h1 className="mt-3 max-w-3xl font-display text-[34px] font-bold leading-[1.02] tracking-[-0.03em] text-white sm:text-[46px]">{c.title}</h1>
            <p className="mt-3 max-w-2xl text-[15px] leading-6 text-green-dim sm:text-base">{c.subtitle}</p>
          </div>
          <div className="rounded-2xl border border-white/15 bg-white/[0.07] p-4 backdrop-blur-sm">
            <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-gold">{c.updated}</p>
            <p className="mt-2 text-sm leading-5 text-green-dim">{c.updatedHint}</p>
            <p className="mt-4 font-display text-3xl font-bold tabular-nums">{helpArticles.length}</p>
            <p className="text-xs text-green-dim">{locale === 'fr' ? 'guides disponibles' : 'available guides'}</p>
          </div>
        </div>
      </header>

      <section className="rounded-[20px] border border-line/60 bg-surface p-4 shadow-[0_14px_40px_rgba(0,59,27,.06)] sm:p-5" aria-label={c.searchLabel}>
        <label htmlFor="admin-help-search" className="mb-2 block text-[10px] font-bold uppercase tracking-[0.17em] text-green-deep">{c.searchLabel}</label>
        <div className="relative">
          <i className="ri-search-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-xl text-green" aria-hidden="true" />
          <input
            id="admin-help-search" type="text" inputMode="search" value={query} onChange={(event) => setQuery(event.target.value)}
            placeholder={c.searchPlaceholder}
            className="min-h-[54px] w-full rounded-xl border border-line bg-background pl-12 pr-12 text-[15px] text-ink outline-none transition focus:border-green focus:ring-4 focus:ring-green/10"
          />
          {query && <button type="button" onClick={() => setQuery('')} aria-label={c.clear} className="absolute right-2 top-1/2 flex h-10 w-10 -translate-y-1/2 items-center justify-center rounded-lg text-ink-variant hover:bg-green/5 hover:text-green"><i className="ri-close-line text-lg" aria-hidden="true" /></button>}
        </div>
        <div className="mt-4 flex gap-2 overflow-x-auto pb-1 sm:flex-wrap sm:overflow-visible" aria-label={locale === 'fr' ? 'Filtrer les guides' : 'Filter guides'}>
          <button type="button" onClick={() => setCategory('all')} aria-pressed={category === 'all'} className={`shrink-0 rounded-full border px-4 py-2 text-xs font-bold transition ${category === 'all' ? 'border-green bg-green text-white' : 'border-line bg-background text-ink-variant hover:border-green/40'}`}>{c.all}</button>
          {helpCategories.map((item) => <button key={item.id} type="button" onClick={() => setCategory(item.id)} aria-pressed={category === item.id} className={`inline-flex shrink-0 items-center gap-2 rounded-full border px-4 py-2 text-xs font-bold transition ${category === item.id ? 'border-green bg-green text-white' : 'border-line bg-background text-ink-variant hover:border-green/40'}`}><i className={item.icon} aria-hidden="true" />{item.label[locale]}</button>)}
        </div>
      </section>

      <div className="grid items-start gap-5 lg:grid-cols-[minmax(280px,.74fr)_minmax(0,1.35fr)]">
        <section className="overflow-hidden rounded-[20px] border border-line/60 bg-surface shadow-[0_14px_38px_rgba(0,59,27,.055)]" aria-label={locale === 'fr' ? 'Résultats de recherche' : 'Search results'}>
          <div className="flex items-center justify-between border-b border-line/60 px-5 py-4">
            <p className="text-[10px] font-bold uppercase tracking-[0.17em] text-ink-variant">{filtered.length} {filtered.length === 1 ? c.result : c.results}</p>
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-gold/15 text-green"><i className="ri-book-open-line" aria-hidden="true" /></span>
          </div>
          <div className="max-h-[680px] overflow-y-auto p-2" aria-live="polite">
            {filtered.length === 0 ? (
              <div className="px-5 py-12 text-center"><i className="ri-search-eye-line text-3xl text-green/45" aria-hidden="true" /><h2 className="mt-3 font-display text-xl font-bold text-green-deep">{c.noResult}</h2><p className="mt-2 text-sm text-ink-variant">{c.noResultHint}</p></div>
            ) : filtered.map((article) => {
              const active = selected?.id === article.id;
              return <button key={article.id} type="button" onClick={() => selectArticle(article.id)} aria-pressed={active} className={`group flex w-full items-start gap-3 rounded-xl px-3 py-3.5 text-left transition ${active ? 'bg-green text-white shadow-[0_9px_20px_rgba(0,59,27,.13)]' : 'hover:bg-green/5'}`}>
                <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${active ? 'bg-white/10 text-gold' : 'bg-green/8 text-green'}`}><i className={`${article.icon} text-lg`} aria-hidden="true" /></span>
                <span className="min-w-0 flex-1"><span className={`block font-display text-[17px] font-bold leading-5 ${active ? 'text-white' : 'text-green-deep'}`}>{article.title[locale]}</span><span className={`mt-1 line-clamp-2 block text-xs leading-5 ${active ? 'text-green-dim' : 'text-ink-variant'}`}>{article.summary[locale]}</span></span>
                <i className={`ri-arrow-right-s-line mt-2 text-lg ${active ? 'text-gold' : 'text-line group-hover:text-green'}`} aria-hidden="true" />
              </button>;
            })}
          </div>
        </section>

        <section className="min-h-[540px] overflow-hidden rounded-[20px] border border-line/60 bg-surface shadow-[0_14px_38px_rgba(0,59,27,.055)]" aria-live="polite">
          {selected ? <>
            <div className="relative overflow-hidden border-b border-line/60 bg-background px-5 py-6 sm:px-7">
              <span className="absolute right-5 top-5 font-display text-[72px] font-bold leading-none text-green/[0.035]" aria-hidden="true">i</span>
              <div className="relative flex items-start gap-4">
                <span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-[14px] bg-green text-gold shadow-[0_9px_22px_rgba(0,59,27,.15)]"><i className={`${selected.icon} text-xl`} aria-hidden="true" /></span>
                <div><p className="text-[9px] font-bold uppercase tracking-[0.18em] text-red-link">{helpCategories.find((item) => item.id === selected.category)?.label[locale]}</p><h2 className="mt-1 font-display text-[27px] font-bold leading-tight text-green-deep sm:text-[32px]">{selected.title[locale]}</h2><p className="mt-2 max-w-2xl text-sm leading-6 text-ink-variant">{selected.summary[locale]}</p></div>
              </div>
            </div>
            <div className="space-y-7 px-5 py-6 sm:px-7">
              <div><h3 className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.17em] text-green-deep"><span className="h-2 w-2 rounded-full bg-gold" aria-hidden="true" />{c.steps}</h3><ol className="mt-4 space-y-4">{selected.steps[locale].map((step, index) => <li key={step} className="grid grid-cols-[34px_1fr] gap-3 text-sm leading-6 text-ink"><span className="flex h-8 w-8 items-center justify-center rounded-full border border-green/20 bg-green/5 text-xs font-bold text-green">{String(index + 1).padStart(2, '0')}</span><span>{step}</span></li>)}</ol></div>
              <div className="rounded-2xl border border-gold/35 bg-gold/[0.07] p-4"><h3 className="flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.17em] text-green-deep"><i className="ri-lightbulb-flash-line text-base text-gold-dark" aria-hidden="true" />{c.tips}</h3><ul className="mt-3 space-y-2">{selected.tips[locale].map((tip) => <li key={tip} className="flex gap-2 text-sm leading-5 text-ink-variant"><span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-gold" aria-hidden="true" />{tip}</li>)}</ul></div>
              <div className="flex flex-wrap items-center justify-between gap-4 border-t border-line/60 pt-5">
                <div>{selected.permission && <><p className="text-[9px] font-bold uppercase tracking-[0.16em] text-ink-variant">{c.permission}</p><span title={selected.permission} className="mt-1 inline-flex min-h-8 items-center rounded-full border border-green/15 bg-green/[0.055] px-3 text-xs font-semibold text-green-deep">{getAdminPermissionLabel(selected.permission, locale)}</span></>}</div>
                <Link to={selected.path} className="inline-flex min-h-[46px] items-center justify-center gap-2 rounded-xl bg-gold px-5 text-[11px] font-bold uppercase tracking-[0.12em] text-green-deep shadow-[0_8px_18px_rgba(255,205,0,.18)] transition hover:-translate-y-0.5 hover:shadow-[0_12px_22px_rgba(255,205,0,.24)]">{c.open}<i className="ri-arrow-right-up-line text-base" aria-hidden="true" /></Link>
              </div>
            </div>
          </> : <div className="flex min-h-[540px] items-center justify-center px-8 text-center text-sm text-ink-variant">{c.select}</div>}
        </section>
      </div>

      <aside className="flex flex-col gap-4 rounded-[18px] border border-red/15 bg-red/[0.035] p-5 sm:flex-row sm:items-center">
        <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full bg-red/10 text-red"><i className="ri-shield-flash-line text-xl" aria-hidden="true" /></span>
        <div><h2 className="font-display text-lg font-bold text-green-deep">{c.sensitive}</h2><p className="mt-1 text-sm leading-5 text-ink-variant">{c.sensitiveText}</p></div>
      </aside>
    </div>
  );
};

export default AdminHelpPage;

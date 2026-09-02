import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, inputClasses } from '../../../components/ui';
import { messages } from '../../../i18n/local';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import { siteContentApi } from '../../../lib/api/site-content';
import type {
  CmsContentItemDto,
  CmsContentRevisionDto,
  CmsContentType,
  UpsertCmsContentRequest,
} from '../../../lib/api/types';

interface CatalogEntry {
  key: string;
  page: string;
  section: string;
  contentType: CmsContentType;
  label: string;
  fallbackFr: string;
  fallbackEn: string;
}

const globalSections = new Set(['nav', 'footer', 'brand', 'theme', 'cookies', 'newsletter', 'common', 'lang', 'backToTop', 'notFound']);
const pageLabels: Record<string, { fr: string; en: string; path: string }> = {
  global: { fr: 'Éléments globaux', en: 'Global elements', path: '/' },
  home: { fr: 'Accueil', en: 'Home', path: '/' },
  services: { fr: 'Services', en: 'Services', path: '/services' },
  news: { fr: 'Actualités', en: 'News', path: '/actualites' },
  engagement: { fr: 'Engagement', en: 'Engagement', path: '/engagement' },
  contact: { fr: 'Contact', en: 'Contact', path: '/contact' },
  member: { fr: 'Espace membre', en: 'Member space', path: '/espace-membre' },
  grants: { fr: 'Bourses', en: 'Grants', path: '/services/bourses' },
  privacy: { fr: 'Confidentialité', en: 'Privacy', path: '/confidentialite' },
};

const extraCatalog: CatalogEntry[] = [
  ...['slide1', 'slide2', 'slide3', 'slide4'].map((slide, index) => ({
    key: `media.home.hero.${slide}`,
    page: 'home',
    section: 'hero',
    contentType: 'image' as const,
    label: `Image ${index + 1} du carrousel`,
    fallbackFr: '',
    fallbackEn: '',
  })),
  ...['zone1.delegate', 'zone1.deputy', 'zone2.delegate', 'zone2.deputy'].map((person) => ({
    key: `media.home.zones.${person}`,
    page: 'home',
    section: 'zones',
    contentType: 'image' as const,
    label: `Portrait ${person.replace('.', ' · ')}`,
    fallbackFr: '',
    fallbackEn: '',
  })),
  {
    key: 'seo.global.title', page: 'global', section: 'seo', contentType: 'seo', label: 'Titre général du site',
    fallbackFr: "HCBE Canada — Haut Conseil des Burkinabè de l'Extérieur", fallbackEn: 'HCBE Canada — High Council of Burkinabè Abroad',
  },
  {
    key: 'seo.global.description', page: 'global', section: 'seo', contentType: 'seo', label: 'Description générale du site',
    fallbackFr: 'Services, actualités et communauté des Burkinabè au Canada.', fallbackEn: 'Services, news and community for Burkinabè people in Canada.',
  },
  ...['home', 'services', 'news', 'engagement', 'contact', 'member'].flatMap((page) => [
    { key: `seo.${page}.title`, page, section: 'seo', contentType: 'seo' as const, label: `SEO · ${page} · titre`, fallbackFr: '', fallbackEn: '' },
    { key: `seo.${page}.description`, page, section: 'seo', contentType: 'seo' as const, label: `SEO · ${page} · description`, fallbackFr: '', fallbackEn: '' },
  ]),
];

const inferEntry = (key: string, fallbackFr: string, fallbackEn: string): CatalogEntry => {
  const segments = key.split('.');
  const root = segments[1] || 'global';
  const page = globalSections.has(root) ? 'global' : root === 'actualites' ? 'news' : root;
  const section = segments[page === 'global' ? 1 : 2] || 'general';
  const longForm = /description|subtitle|content|body|welcome|privacy|intro|mission|vision|message/i.test(key)
    || Math.max(fallbackFr.length, fallbackEn.length) > 120;
  return {
    key,
    page,
    section,
    contentType: longForm ? 'richtext' : 'text',
    label: segments.slice(2).join(' · ').replace(/([a-z])([A-Z])/g, '$1 $2'),
    fallbackFr,
    fallbackEn,
  };
};

const catalog = [
  ...Object.keys(messages.fr?.translation || {})
    .filter((key) => key.startsWith('public.'))
    .map((key) => inferEntry(key, messages.fr.translation[key] || '', messages.en?.translation[key] || '')),
  ...extraCatalog,
].sort((left, right) => left.page.localeCompare(right.page) || left.key.localeCompare(right.key));

const copy = {
  fr: {
    eyebrow: 'Studio de publication', title: 'Pilotez tout le site public',
    description: 'Modifiez les textes, médias et métadonnées bilingues. Une publication est diffusée instantanément aux visiteurs connectés.',
    search: 'Rechercher un texte, une section ou une clé…', all: 'Tout le site', fields: 'champs éditables',
    published: 'publiés', pending: 'brouillons', publishAll: 'Publier tous les brouillons', openPage: 'Voir la page',
    french: 'Français', english: 'Anglais', current: 'Version actuellement publiée', saveDraft: 'Enregistrer le brouillon',
    savePublish: 'Enregistrer et publier', history: 'Historique', noHistory: 'Aucune publication précédente.',
    rollback: 'Restaurer', saved: 'Brouillon enregistré.', live: 'Publié en direct.', allLive: 'Tous les changements sont en ligne.',
    reset: 'Rétablir la valeur intégrée', resetConfirm: 'Supprimer ce contenu CMS et rétablir la valeur intégrée au site ?', resetDone: 'La valeur intégrée a été rétablie.',
    upload: 'Importer une image', uploading: 'Importation…', inherited: 'Valeur intégrée au site', overridden: 'Géré par le CMS',
    empty: 'Aucun champ ne correspond à cette recherche.', editorHint: 'Sélectionnez un élément dans la liste pour le modifier.',
    seo: 'Référencement', media: 'Médias', content: 'Contenu', status: 'Diffusion en direct', error: 'Une erreur est survenue.',
  },
  en: {
    eyebrow: 'Publishing studio', title: 'Control the entire public website',
    description: 'Edit bilingual copy, media and metadata. Publishing is pushed instantly to connected visitors.',
    search: 'Search copy, sections or keys…', all: 'Entire website', fields: 'editable fields',
    published: 'published', pending: 'drafts', publishAll: 'Publish all drafts', openPage: 'View page',
    french: 'French', english: 'English', current: 'Currently published version', saveDraft: 'Save draft',
    savePublish: 'Save and publish', history: 'History', noHistory: 'No previous publication.', rollback: 'Restore',
    saved: 'Draft saved.', live: 'Published live.', allLive: 'All changes are live.', upload: 'Upload image', uploading: 'Uploading…',
    reset: 'Restore built-in value', resetConfirm: 'Delete this CMS override and restore the website’s built-in value?', resetDone: 'The built-in value has been restored.',
    inherited: 'Built-in website value', overridden: 'Managed by CMS', empty: 'No field matches this search.',
    editorHint: 'Select an item from the list to edit it.', seo: 'SEO', media: 'Media', content: 'Content',
    status: 'Live publishing', error: 'Something went wrong.',
  },
};

export const CmsContentStudio = () => {
  const { i18n } = useTranslation();
  const c = i18n.language.startsWith('en') ? copy.en : copy.fr;
  const english = i18n.language.startsWith('en');
  const [items, setItems] = useState<CmsContentItemDto[]>([]);
  const [selectedKey, setSelectedKey] = useState(catalog[0]?.key || '');
  const [page, setPage] = useState('all');
  const [search, setSearch] = useState('');
  const [valueFr, setValueFr] = useState('');
  const [valueEn, setValueEn] = useState('');
  const [revisions, setRevisions] = useState<CmsContentRevisionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [notice, setNotice] = useState('');

  const load = async () => {
    setLoading(true);
    try {
      const response = await siteContentApi.getCmsItems();
      if (response.success && response.data) setItems(response.data);
    } finally { setLoading(false); }
  };

  useEffect(() => { void load(); }, []);

  const itemByKey = useMemo(() => Object.fromEntries(items.map((item) => [item.key, item])), [items]);
  const selected = catalog.find((entry) => entry.key === selectedKey) || catalog[0];
  const stored = selected ? itemByKey[selected.key] : undefined;

  useEffect(() => {
    if (!selected) return;
    setValueFr(stored?.draftValueFr ?? selected.fallbackFr);
    setValueEn(stored?.draftValueEn ?? selected.fallbackEn);
    setRevisions([]);
    if (stored?.id) {
      void siteContentApi.getCmsRevisions(stored.id).then((response) => {
        if (response.success && response.data) setRevisions(response.data);
      });
    }
  }, [selectedKey, stored?.id]);

  const pages = useMemo(() => Array.from(new Set(catalog.map((entry) => entry.page))), []);
  const visible = useMemo(() => {
    const query = search.trim().toLowerCase();
    return catalog.filter((entry) =>
      (page === 'all' || entry.page === page)
      && (!query || `${entry.key} ${entry.label} ${entry.fallbackFr} ${entry.fallbackEn}`.toLowerCase().includes(query)));
  }, [page, search]);
  const pendingCount = items.filter((item) => item.hasUnpublishedChanges).length;
  const publishedCount = items.filter((item) => item.isPublished).length;

  const save = async (publish: boolean) => {
    if (!selected) return;
    setBusy(true); setNotice('');
    const request: UpsertCmsContentRequest = {
      key: selected.key,
      page: selected.page,
      section: selected.section,
      contentType: selected.contentType,
      label: selected.label,
      valueFr,
      valueEn,
      publish,
    };
    try {
      const response = await siteContentApi.upsertCmsItem(request);
      if (response.success && response.data) {
        setItems((current) => [...current.filter((item) => item.key !== response.data!.key), response.data!]);
        setNotice(publish ? c.live : c.saved);
        if (publish) {
          const history = await siteContentApi.getCmsRevisions(response.data.id);
          if (history.success && history.data) setRevisions(history.data);
        }
      } else setNotice(c.error);
    } catch { setNotice(c.error); }
    finally { setBusy(false); }
  };

  const publishAll = async () => {
    setBusy(true); setNotice('');
    try {
      const response = await siteContentApi.publishAllCms();
      setNotice(response.success ? c.allLive : c.error);
      if (response.success) await load();
    } catch { setNotice(c.error); }
    finally { setBusy(false); }
  };

  const upload = async (file?: File) => {
    if (!file) return;
    setUploading(true); setNotice('');
    try {
      const response = await siteContentApi.uploadCmsMedia(file);
      if (response.success && response.data) {
        setValueFr(response.data.url);
        setValueEn(response.data.url);
      } else setNotice(c.error);
    } catch { setNotice(c.error); }
    finally { setUploading(false); }
  };

  const rollback = async (version: number) => {
    if (!stored) return;
    setBusy(true);
    try {
      const response = await siteContentApi.rollbackCmsItem(stored.id, version);
      if (response.success && response.data) {
        setItems((current) => [...current.filter((item) => item.key !== response.data!.key), response.data!]);
        setValueFr(response.data.draftValueFr || ''); setValueEn(response.data.draftValueEn || ''); setNotice(c.live);
        const history = await siteContentApi.getCmsRevisions(stored.id);
        if (history.success && history.data) setRevisions(history.data);
      }
    } finally { setBusy(false); }
  };

  const resetOverride = async () => {
    if (!stored || !window.confirm(c.resetConfirm)) return;
    setBusy(true); setNotice('');
    try {
      const response = await siteContentApi.deleteCmsItem(stored.id);
      if (response.success) {
        setItems((current) => current.filter((item) => item.id !== stored.id));
        setValueFr(selected.fallbackFr); setValueEn(selected.fallbackEn); setRevisions([]); setNotice(c.resetDone);
      } else setNotice(c.error);
    } catch { setNotice(c.error); }
    finally { setBusy(false); }
  };

  return (
    <section className="overflow-hidden rounded-[22px] border border-green/15 bg-surface shadow-[0_24px_70px_rgba(0,59,27,.10)]">
      <header className="public-grid-pattern relative overflow-hidden bg-green-deep px-5 py-6 text-white sm:px-7 lg:px-8">
        <div className="pointer-events-none absolute -right-16 -top-24 h-64 w-64 rounded-full border-[48px] border-white/[.035]" />
        <div className="relative flex flex-col gap-6 xl:flex-row xl:items-end xl:justify-between">
          <div className="max-w-3xl">
            <p className="text-[9px] font-bold uppercase tracking-[.2em] text-gold">{c.eyebrow}</p>
            <h2 className="mt-2 font-display text-3xl font-bold text-white sm:text-4xl">{c.title}</h2>
            <p className="mt-3 max-w-2xl text-sm leading-6 text-white/65">{c.description}</p>
          </div>
          <div className="flex flex-wrap items-center gap-3">
            <span className="inline-flex items-center gap-2 rounded-full border border-emerald-300/15 bg-emerald-300/10 px-3 py-2 text-[10px] font-bold uppercase tracking-[.12em] text-emerald-100">
              <span className="h-2 w-2 animate-pulse rounded-full bg-emerald-300" />{c.status}
            </span>
            <Button type="button" variant="primary" disabled={busy || pendingCount === 0} onClick={() => void publishAll()}>
              <i className="ri-broadcast-line" /> {c.publishAll}{pendingCount > 0 ? ` (${pendingCount})` : ''}
            </Button>
          </div>
        </div>
        <div className="relative mt-6 grid grid-cols-3 gap-px overflow-hidden rounded-xl border border-white/10 bg-white/10">
          {[[catalog.length, c.fields], [publishedCount, c.published], [pendingCount, c.pending]].map(([value, label]) => (
            <div key={String(label)} className="bg-green-deep/75 px-4 py-3"><strong className="font-display text-2xl text-white">{value}</strong><span className="ml-2 text-[10px] uppercase tracking-[.12em] text-white/50">{label}</span></div>
          ))}
        </div>
      </header>

      {notice && <div role="status" className="border-b border-gold/25 bg-gold/10 px-6 py-3 text-sm font-medium text-green-deep"><i className="ri-checkbox-circle-line mr-2 text-green" />{notice}</div>}

      <div className="grid min-h-[680px] xl:grid-cols-[230px_minmax(300px,.8fr)_minmax(440px,1.2fr)]">
        <aside className="border-b border-line bg-surface-container/45 p-4 xl:border-b-0 xl:border-r">
          <p className="mb-3 px-2 text-[9px] font-bold uppercase tracking-[.17em] text-ink-variant">Pages</p>
          <div className="space-y-1">
            <PageButton active={page === 'all'} label={c.all} count={catalog.length} onClick={() => setPage('all')} />
            {pages.map((pageName) => <PageButton key={pageName} active={page === pageName} label={english ? pageLabels[pageName]?.en || pageName : pageLabels[pageName]?.fr || pageName} count={catalog.filter((entry) => entry.page === pageName).length} onClick={() => setPage(pageName)} />)}
          </div>
          <div className="mt-6 rounded-xl border border-line bg-surface p-3 text-xs leading-5 text-ink-variant">
            <i className="ri-shield-check-line mr-2 text-green" />Les contenus intégrés restent disponibles comme valeurs de secours.
          </div>
        </aside>

        <div className="border-b border-line xl:border-b-0 xl:border-r">
          <div className="sticky top-0 z-10 border-b border-line bg-surface/95 p-4 backdrop-blur">
            <label className="relative block"><i className="ri-search-line absolute left-3 top-1/2 -translate-y-1/2 text-ink-variant" /><input className={`${inputClasses} pl-10`} value={search} onChange={(event) => setSearch(event.target.value)} placeholder={c.search} /></label>
          </div>
          <div className="max-h-[720px] overflow-y-auto">
            {loading ? <div className="p-10 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-2xl" /></div> : visible.length === 0 ? <p className="p-8 text-center text-sm text-ink-variant">{c.empty}</p> : visible.map((entry) => {
              const saved = itemByKey[entry.key];
              return <button key={entry.key} type="button" onClick={() => setSelectedKey(entry.key)} className={`group flex w-full items-start gap-3 border-b border-line/70 px-4 py-3 text-left transition-colors ${selectedKey === entry.key ? 'bg-green/8' : 'hover:bg-surface-container'}`}>
                <span className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${entry.contentType === 'image' ? 'bg-red-link/10 text-red-link' : entry.contentType === 'seo' ? 'bg-gold/15 text-gold-ink' : 'bg-green/10 text-green'}`}><i className={entry.contentType === 'image' ? 'ri-image-line' : entry.contentType === 'seo' ? 'ri-search-eye-line' : 'ri-text'} /></span>
                <span className="min-w-0 flex-1"><strong className="block truncate text-sm font-semibold text-green-deep">{entry.label || entry.key}</strong><small className="mt-1 block truncate text-[10px] text-ink-variant">{entry.section} · {saved ? c.overridden : c.inherited}</small></span>
                {saved?.hasUnpublishedChanges ? <span className="mt-2 h-2 w-2 rounded-full bg-gold" title={c.pending} /> : saved?.isPublished ? <i className="ri-checkbox-circle-fill mt-1 text-green" /> : null}
              </button>;
            })}
          </div>
        </div>

        <div className="bg-surface p-5 sm:p-6 lg:p-7">
          {!selected ? <p className="text-sm text-ink-variant">{c.editorHint}</p> : <>
            <div className="flex flex-wrap items-start justify-between gap-4 border-b border-line pb-5">
              <div><p className="text-[9px] font-bold uppercase tracking-[.17em] text-red-link">{selected.page} · {selected.section}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{selected.label || selected.key}</h3><code className="mt-2 block text-[10px] text-ink-variant">{selected.key}</code></div>
              <a href={pageLabels[selected.page]?.path || '/'} target="_blank" rel="noreferrer" className="inline-flex min-h-10 items-center gap-2 rounded-full border border-green/25 px-4 text-[10px] font-bold uppercase tracking-[.1em] text-green hover:bg-green hover:text-white">{c.openPage}<i className="ri-external-link-line" /></a>
            </div>

            {selected.contentType === 'image' && <div className="mt-5 overflow-hidden rounded-xl border border-line bg-canvas p-3">
              {valueFr ? <img src={resolveMediaUrl(valueFr)} alt="" className="h-40 w-full rounded-lg object-cover" /> : <div className="flex h-32 items-center justify-center text-sm text-ink-variant"><i className="ri-image-add-line mr-2 text-xl" />{c.upload}</div>}
              <label className="mt-3 inline-flex cursor-pointer items-center gap-2 rounded-full bg-green px-4 py-2 text-[10px] font-bold uppercase tracking-[.1em] text-white"><i className={uploading ? 'ri-loader-4-line animate-spin' : 'ri-upload-cloud-2-line'} />{uploading ? c.uploading : c.upload}<input type="file" accept="image/*" className="sr-only" disabled={uploading} onChange={(event) => void upload(event.target.files?.[0])} /></label>
            </div>}

            <div className="mt-6 grid gap-5">
              <EditorField label={c.french} value={valueFr} setValue={setValueFr} multiline={selected.contentType === 'richtext' || selected.key.endsWith('.description')} />
              <EditorField label={c.english} value={valueEn} setValue={setValueEn} multiline={selected.contentType === 'richtext' || selected.key.endsWith('.description')} />
            </div>

            {stored?.isPublished && <div className="mt-5 rounded-xl border border-green/10 bg-green/5 p-4"><p className="text-[9px] font-bold uppercase tracking-[.15em] text-green">{c.current} · v{stored.version}</p><div className="mt-2 grid gap-3 text-xs text-ink-variant sm:grid-cols-2"><p className="line-clamp-3">FR · {stored.publishedValueFr || '—'}</p><p className="line-clamp-3">EN · {stored.publishedValueEn || '—'}</p></div></div>}

            <div className="mt-6 flex flex-wrap justify-end gap-3 border-t border-line pt-5">
              {stored && <button type="button" disabled={busy} onClick={() => void resetOverride()} className="mr-auto inline-flex min-h-11 items-center gap-2 px-1 text-[10px] font-bold uppercase tracking-[.1em] text-error disabled:opacity-40"><i className="ri-reset-left-line" />{c.reset}</button>}
              <Button type="button" variant="secondary" disabled={busy} onClick={() => void save(false)}><i className="ri-draft-line" /> {c.saveDraft}</Button>
              <Button type="button" variant="primary" disabled={busy} onClick={() => void save(true)}><i className="ri-broadcast-line" /> {c.savePublish}</Button>
            </div>

            {stored && <details className="mt-6 border-t border-line pt-5"><summary className="cursor-pointer text-[10px] font-bold uppercase tracking-[.14em] text-green">{c.history} ({revisions.length})</summary><div className="mt-3 space-y-2">{revisions.length === 0 ? <p className="text-sm text-ink-variant">{c.noHistory}</p> : revisions.map((revision) => <div key={revision.id} className="flex items-center justify-between gap-3 rounded-lg bg-surface-container px-3 py-2 text-xs"><span>v{revision.version} · {new Date(revision.publishedAt).toLocaleString(english ? 'en-CA' : 'fr-CA')}</span><button type="button" disabled={busy || revision.version === stored.version} onClick={() => void rollback(revision.version)} className="font-bold text-green disabled:opacity-35">{c.rollback}</button></div>)}</div></details>}
          </>}
        </div>
      </div>
    </section>
  );
};

const PageButton = ({ active, label, count, onClick }: { active: boolean; label: string; count: number; onClick: () => void }) => <button type="button" onClick={onClick} className={`flex w-full items-center justify-between rounded-lg px-3 py-2.5 text-left text-sm transition-colors ${active ? 'bg-green text-white shadow-sm' : 'text-ink-variant hover:bg-surface hover:text-green'}`}><span>{label}</span><span className={`rounded-full px-2 py-0.5 text-[9px] ${active ? 'bg-white/15' : 'bg-surface'}`}>{count}</span></button>;

const EditorField = ({ label, value, setValue, multiline }: { label: string; value: string; setValue: (value: string) => void; multiline: boolean }) => <label className="block"><span className="mb-2 block text-[10px] font-bold uppercase tracking-[.13em] text-ink-variant">{label}</span>{multiline ? <textarea rows={6} className={`${inputClasses} min-h-32 resize-y leading-6`} value={value} onChange={(event) => setValue(event.target.value)} /> : <input className={inputClasses} value={value} onChange={(event) => setValue(event.target.value)} />}</label>;

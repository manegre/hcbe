import { useEffect, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, Field, inputClasses } from '../../../components/ui';
import { siteContentApi } from '../../../lib/api/site-content';
import type { FooterLinkDto, NavigationItemDto, PageSectionDto } from '../../../lib/api/types';

const statisticDefaults: Record<string, string> = { provinces: '11', zones: '2', associations: '15', membership: 'free' };
const blankNavigation = (): Omit<NavigationItemDto, 'id'> => ({ label: '', labelEn: '', url: '/', isActive: true, displayOrder: 0 });
const blankFooter = (): Omit<FooterLinkDto, 'id'> => ({ category: 'Navigation', categoryEn: 'Navigation', label: '', labelEn: '', url: '/', isActive: true, displayOrder: 0 });
const blankSection = (): Omit<PageSectionDto, 'id'> => ({ page: 'home', section: '', title: '', titleEn: '', content: '', contentEn: '', isActive: true, displayOrder: 0 });

const SiteContentPage = () => {
  const { t } = useTranslation();
  const [statistics, setStatistics] = useState(statisticDefaults);
  const [navigation, setNavigation] = useState<NavigationItemDto[]>([]);
  const [footer, setFooter] = useState<FooterLinkDto[]>([]);
  const [sections, setSections] = useState<PageSectionDto[]>([]);
  const [newNavigation, setNewNavigation] = useState(blankNavigation);
  const [newFooter, setNewFooter] = useState(blankFooter);
  const [newSection, setNewSection] = useState(blankSection);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');

  const load = async () => {
    setLoading(true);
    const [statsResponse, navigationResponse, footerResponse, sectionResponse] = await Promise.all([
      siteContentApi.getStatistics(), siteContentApi.getNavigation(true), siteContentApi.getFooter(true), siteContentApi.getPageSections(undefined, true),
    ]);
    if (statsResponse.success && statsResponse.data) {
      const byKey = Object.fromEntries(statsResponse.data.map((item) => [item.key, item.value]));
      setStatistics((current) => ({ ...current, provinces: byKey.provinces || byKey.provinces_covered || current.provinces, zones: byKey.zones || byKey.zones_covered || current.zones, associations: byKey.associations || current.associations, membership: byKey.membership || current.membership }));
    }
    if (navigationResponse.success && navigationResponse.data) setNavigation(navigationResponse.data);
    if (footerResponse.success && footerResponse.data) setFooter(footerResponse.data);
    if (sectionResponse.success && sectionResponse.data) setSections(sectionResponse.data);
    setLoading(false);
  };

  useEffect(() => { void load(); }, []);

  const run = async (action: () => Promise<{ success: boolean }>, successMessage: string) => {
    setBusy(true); setMessage('');
    try {
      const response = await action();
      setMessage(response.success ? successMessage : t('admin.siteContent.error'));
      if (response.success) await load();
    } finally { setBusy(false); }
  };

  const saveStatistics = (event: React.FormEvent) => {
    event.preventDefault();
    void run(async () => {
      const results = await Promise.all(Object.entries(statistics).map(([key, value]) => siteContentApi.updateStatistic(key, value)));
      return { success: results.every((result) => result.success) };
    }, t('admin.siteContent.saved'));
  };

  if (loading) return <div className="flex justify-center py-24"><div className="h-8 w-8 animate-spin border-2 border-line border-t-green" /></div>;

  return (
    <section className="space-y-5">
      <AdminPageHeader title={t('admin.siteContent.title')} subtitle={t('admin.siteContent.subtitle')} icon="ri-layout-4-line" />
      {message && <p role="status" className="admin-panel border-l-4 border-l-gold px-5 py-4 text-sm text-ink">{message}</p>}

      <CmsPanel eyebrow={t('admin.siteContent.statistics')} title={t('admin.siteContent.statisticsHint')} icon="ri-bar-chart-box-line">
        <form onSubmit={saveStatistics}>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {Object.keys(statisticDefaults).map((key) => <Field key={key} label={t(`admin.siteContent.${key}`)} htmlFor={`site-stat-${key}`}><input id={`site-stat-${key}`} className={inputClasses} disabled={busy} value={statistics[key] || ''} onChange={(event) => setStatistics((current) => ({ ...current, [key]: event.target.value }))} /></Field>)}
          </div>
          <div className="mt-5 flex justify-end"><Button type="submit" variant="primary" disabled={busy}>{t('admin.common.save')}</Button></div>
        </form>
      </CmsPanel>

      <CmsPanel eyebrow={t('admin.siteContent.navigation')} title={t('admin.siteContent.navigationHint')} icon="ri-menu-2-line">
        <p className="mb-3 text-xs text-ink-variant">FR · EN · URL · {t('admin.common.order')}</p>
        <div className="space-y-3">
          {navigation.map((item) => <NavigationRow key={item.id} item={item} busy={busy} setItems={setNavigation} save={() => void run(() => siteContentApi.updateNavigation(item.id, item), t('admin.siteContent.saved'))} remove={() => void run(() => siteContentApi.deleteNavigation(item.id), t('admin.siteContent.deleted'))} />)}
          <div className="grid gap-3 rounded-xl border border-dashed border-green/35 p-4 md:grid-cols-[1fr_1fr_1.2fr_90px_auto]">
            <input aria-label="Label FR" placeholder="Accueil" className={inputClasses} value={newNavigation.label} onChange={(event) => setNewNavigation((row) => ({ ...row, label: event.target.value }))} />
            <input aria-label="Label EN" placeholder="Home" className={inputClasses} value={newNavigation.labelEn || ''} onChange={(event) => setNewNavigation((row) => ({ ...row, labelEn: event.target.value }))} />
            <input aria-label="URL" placeholder="/" className={inputClasses} value={newNavigation.url} onChange={(event) => setNewNavigation((row) => ({ ...row, url: event.target.value }))} />
            <input aria-label={t('admin.common.order')} type="number" className={inputClasses} value={newNavigation.displayOrder} onChange={(event) => setNewNavigation((row) => ({ ...row, displayOrder: Number(event.target.value) }))} />
            <Button type="button" variant="secondary" disabled={busy || !newNavigation.label.trim()} onClick={() => void run(async () => { const response = await siteContentApi.createNavigation(newNavigation); if (response.success) setNewNavigation(blankNavigation()); return response; }, t('admin.siteContent.created'))}>{t('admin.common.add')}</Button>
          </div>
        </div>
      </CmsPanel>

      <CmsPanel eyebrow={t('admin.siteContent.footer')} title={t('admin.siteContent.footerHint')} icon="ri-layout-bottom-2-line">
        <p className="mb-3 text-xs text-ink-variant">Catégorie FR · Category EN · Libellé FR · Label EN · URL · {t('admin.common.order')}</p>
        <div className="space-y-3">
          {footer.map((item) => <FooterRow key={item.id} item={item} busy={busy} setItems={setFooter} save={() => void run(() => siteContentApi.updateFooter(item.id, item), t('admin.siteContent.saved'))} remove={() => void run(() => siteContentApi.deleteFooter(item.id), t('admin.siteContent.deleted'))} />)}
          <div className="grid gap-3 rounded-xl border border-dashed border-green/35 p-4 lg:grid-cols-[.8fr_.8fr_1fr_1fr_1.1fr_80px_auto]">
            {(['category', 'categoryEn', 'label', 'labelEn', 'url'] as const).map((field) => <input key={field} aria-label={field} placeholder={field} className={inputClasses} value={newFooter[field] || ''} onChange={(event) => setNewFooter((row) => ({ ...row, [field]: event.target.value }))} />)}
            <input aria-label={t('admin.common.order')} type="number" className={inputClasses} value={newFooter.displayOrder} onChange={(event) => setNewFooter((row) => ({ ...row, displayOrder: Number(event.target.value) }))} />
            <Button type="button" variant="secondary" disabled={busy || !newFooter.label.trim()} onClick={() => void run(async () => { const response = await siteContentApi.createFooter(newFooter); if (response.success) setNewFooter(blankFooter()); return response; }, t('admin.siteContent.created'))}>{t('admin.common.add')}</Button>
          </div>
        </div>
      </CmsPanel>

      <CmsPanel eyebrow={t('admin.siteContent.sections')} title={t('admin.siteContent.sectionsHint')} icon="ri-pages-line">
        <div className="grid gap-4 xl:grid-cols-2">
          {sections.map((item) => <SectionCard key={item.id} item={item} busy={busy} setItems={setSections} save={() => void run(() => siteContentApi.updatePageSection(item.id, item), t('admin.siteContent.saved'))} remove={() => void run(() => siteContentApi.deletePageSection(item.id), t('admin.siteContent.deleted'))} />)}
        </div>
        <div className="mt-4 grid gap-3 rounded-xl border border-dashed border-green/35 p-4 md:grid-cols-2 xl:grid-cols-4">
          <input aria-label="Page" placeholder="home" className={inputClasses} value={newSection.page} onChange={(event) => setNewSection((row) => ({ ...row, page: event.target.value }))} />
          <input aria-label="Section" placeholder="mission" className={inputClasses} value={newSection.section} onChange={(event) => setNewSection((row) => ({ ...row, section: event.target.value }))} />
          <input aria-label="Titre FR" placeholder="Titre FR" className={inputClasses} value={newSection.title || ''} onChange={(event) => setNewSection((row) => ({ ...row, title: event.target.value }))} />
          <input aria-label="Title EN" placeholder="Title EN" className={inputClasses} value={newSection.titleEn || ''} onChange={(event) => setNewSection((row) => ({ ...row, titleEn: event.target.value }))} />
          <textarea aria-label="Contenu FR" placeholder="Contenu FR" className={`${inputClasses} min-h-24 md:col-span-2`} value={newSection.content || ''} onChange={(event) => setNewSection((row) => ({ ...row, content: event.target.value }))} />
          <textarea aria-label="Content EN" placeholder="Content EN" className={`${inputClasses} min-h-24 md:col-span-2`} value={newSection.contentEn || ''} onChange={(event) => setNewSection((row) => ({ ...row, contentEn: event.target.value }))} />
          <div className="md:col-span-2 xl:col-span-4 flex justify-end"><Button type="button" variant="secondary" disabled={busy || !newSection.page.trim() || !newSection.section.trim()} onClick={() => void run(async () => { const response = await siteContentApi.createPageSection(newSection); if (response.success) setNewSection(blankSection()); return response; }, t('admin.siteContent.created'))}>{t('admin.common.add')}</Button></div>
        </div>
      </CmsPanel>
    </section>
  );
};

const CmsPanel = ({ eyebrow, title, icon, children }: { eyebrow: string; title: string; icon: string; children: ReactNode }) => <section className="admin-panel overflow-hidden"><header className="flex items-center gap-3 border-b border-line/60 bg-surface-container/55 px-5 py-4"><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-gold/15 text-gold-ink"><i className={icon} aria-hidden="true" /></span><div><p className="text-[10px] font-bold uppercase tracking-[.16em] text-green">{eyebrow}</p><h2 className="font-display text-lg text-green-deep">{title}</h2></div></header><div className="p-5">{children}</div></section>;

const RowActions = ({ busy, active, onToggle, onSave, onDelete }: { busy: boolean; active: boolean; onToggle: () => void; onSave: () => void; onDelete: () => void }) => <div className="flex items-center justify-end gap-1"><button type="button" disabled={busy} onClick={onToggle} className={active ? 'h-10 w-10 text-green' : 'h-10 w-10 text-ink-variant'} aria-label="Toggle"><i className={active ? 'ri-eye-line' : 'ri-eye-off-line'} /></button><button type="button" disabled={busy} onClick={onSave} className="h-10 w-10 text-green" aria-label="Save"><i className="ri-save-line" /></button><button type="button" disabled={busy} onClick={onDelete} className="h-10 w-10 text-error" aria-label="Delete"><i className="ri-delete-bin-line" /></button></div>;

const NavigationRow = ({ item, busy, setItems, save, remove }: { item: NavigationItemDto; busy: boolean; setItems: React.Dispatch<React.SetStateAction<NavigationItemDto[]>>; save: () => void; remove: () => void }) => <div className="grid gap-3 rounded-xl border border-line/70 bg-surface-container/40 p-4 md:grid-cols-[1fr_1fr_1.2fr_90px_auto]"><input aria-label="Label FR" className={inputClasses} value={item.label} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, label: event.target.value } : row))} /><input aria-label="Label EN" className={inputClasses} value={item.labelEn || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, labelEn: event.target.value } : row))} /><input aria-label="URL" className={inputClasses} value={item.url} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, url: event.target.value } : row))} /><input aria-label="Order" type="number" className={inputClasses} value={item.displayOrder} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, displayOrder: Number(event.target.value) } : row))} /><RowActions busy={busy} active={item.isActive} onToggle={() => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, isActive: !row.isActive } : row))} onSave={save} onDelete={remove} /></div>;

const FooterRow = ({ item, busy, setItems, save, remove }: { item: FooterLinkDto; busy: boolean; setItems: React.Dispatch<React.SetStateAction<FooterLinkDto[]>>; save: () => void; remove: () => void }) => <div className="grid gap-3 rounded-xl border border-line/70 bg-surface-container/40 p-4 lg:grid-cols-[.8fr_.8fr_1fr_1fr_1.1fr_80px_auto]">{(['category', 'categoryEn', 'label', 'labelEn', 'url'] as const).map((field) => <input key={field} aria-label={field} className={inputClasses} value={item[field] || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, [field]: event.target.value } : row))} />)}<input aria-label="Order" type="number" className={inputClasses} value={item.displayOrder} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, displayOrder: Number(event.target.value) } : row))} /><RowActions busy={busy} active={item.isActive} onToggle={() => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, isActive: !row.isActive } : row))} onSave={save} onDelete={remove} /></div>;

const SectionCard = ({ item, busy, setItems, save, remove }: { item: PageSectionDto; busy: boolean; setItems: React.Dispatch<React.SetStateAction<PageSectionDto[]>>; save: () => void; remove: () => void }) => <article className="rounded-xl border border-line/70 bg-surface-container/40 p-5"><div className="mb-4 flex items-center justify-between gap-3"><p className="font-display text-lg text-green-deep">{item.page} / {item.section}</p><button type="button" className="text-sm text-error" onClick={remove}>Delete</button></div><div className="grid gap-3 sm:grid-cols-2"><input aria-label="Titre FR" placeholder="Titre FR" className={inputClasses} value={item.title || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, title: event.target.value } : row))} /><input aria-label="Title EN" placeholder="Title EN" className={inputClasses} value={item.titleEn || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, titleEn: event.target.value } : row))} /><textarea aria-label="Contenu FR" className={`${inputClasses} min-h-28`} value={item.content || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, content: event.target.value } : row))} /><textarea aria-label="Content EN" className={`${inputClasses} min-h-28`} value={item.contentEn || ''} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, contentEn: event.target.value } : row))} /></div><div className="mt-4 flex items-center justify-between"><label className="flex items-center gap-2 text-sm text-ink"><input type="checkbox" checked={item.isActive} onChange={(event) => setItems((rows) => rows.map((row) => row.id === item.id ? { ...row, isActive: event.target.checked } : row))} />Active</label><Button type="button" variant="secondary" disabled={busy} onClick={save}>Save</Button></div></article>;

export default SiteContentPage;

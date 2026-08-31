import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, Field, StatusChip, inputClasses } from '../../../components/ui';
import { partnersApi } from '../../../lib/api/partners';
import { resolveMediaUrl } from '../../../lib/api/media-url';
import type { CreatePartnerRequest, PartnerDto } from '../../../lib/api/types';

const emptyForm = (displayOrder: number): CreatePartnerRequest => ({
  name: '',
  nameEn: '',
  description: '',
  descriptionEn: '',
  logoUrl: '',
  websiteUrl: '',
  altText: '',
  altTextEn: '',
  isFeatured: true,
  isActive: true,
  displayOrder,
});

const toForm = (partner: PartnerDto): CreatePartnerRequest => ({
  name: partner.name,
  nameEn: partner.nameEn ?? '',
  description: partner.description ?? '',
  descriptionEn: partner.descriptionEn ?? '',
  logoUrl: partner.logoUrl ?? '',
  websiteUrl: partner.websiteUrl ?? '',
  altText: partner.altText ?? '',
  altTextEn: partner.altTextEn ?? '',
  isFeatured: partner.isFeatured,
  isActive: partner.isActive,
  displayOrder: partner.displayOrder,
});

const PartnerMark = ({ partner, compact = false }: { partner: PartnerDto; compact?: boolean }) => (
  <span className={`flex items-center justify-center overflow-hidden ${compact ? 'h-11 w-28' : 'h-14 w-40'}`}>
    {partner.logoUrl ? (
      <img
        src={resolveMediaUrl(partner.logoUrl)}
        alt={partner.altText || partner.name}
        className="max-h-full max-w-full object-contain grayscale transition duration-300 hover:grayscale-0"
      />
    ) : (
      <span className="max-w-full truncate font-display text-base font-bold text-green-deep">{partner.name}</span>
    )}
  </span>
);

const AdminPartnersPage = () => {
  const { t } = useTranslation();
  const [partners, setPartners] = useState<PartnerDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [editing, setEditing] = useState<PartnerDto | 'new' | null>(null);
  const [form, setForm] = useState<CreatePartnerRequest>(emptyForm(0));
  const [logoFile, setLogoFile] = useState<File | null>(null);
  const [logoPreview, setLogoPreview] = useState('');

  const visiblePartners = useMemo(
    () => partners.filter((partner) => partner.isActive && partner.isFeatured),
    [partners],
  );

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await partnersApi.getAdmin();
      if (response.success && response.data) setPartners(response.data);
      else setError(response.message || t('admin.partners.errorLoad'));
    } catch {
      setError(t('admin.partners.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  useEffect(() => () => {
    if (logoPreview.startsWith('blob:')) URL.revokeObjectURL(logoPreview);
  }, [logoPreview]);

  const openCreate = () => {
    setEditing('new');
    setForm(emptyForm(partners.length));
    setLogoFile(null);
    setLogoPreview('');
    setError('');
  };

  const openEdit = (partner: PartnerDto) => {
    setEditing(partner);
    setForm(toForm(partner));
    setLogoFile(null);
    setLogoPreview(partner.logoUrl ? resolveMediaUrl(partner.logoUrl) : '');
    setError('');
  };

  const closeEditor = () => {
    if (busy) return;
    setEditing(null);
    setLogoFile(null);
    setLogoPreview('');
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!form.name.trim()) return;
    setBusy(true);
    setError('');
    setMessage('');
    try {
      let logoUrl = form.logoUrl;
      if (logoFile) {
        const upload = await partnersApi.uploadLogo(logoFile);
        if (!upload.success || !upload.data) throw new Error(upload.message || t('admin.partners.errorUpload'));
        logoUrl = upload.data.url;
      }

      const payload = { ...form, name: form.name.trim(), logoUrl };
      const response = editing === 'new'
        ? await partnersApi.create(payload)
        : await partnersApi.update(editing!.id, payload);
      if (!response.success) throw new Error(response.message || t('admin.partners.errorSave'));
      setMessage(editing === 'new' ? t('admin.partners.created') : t('admin.partners.updated'));
      setEditing(null);
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('admin.partners.errorSave'));
    } finally {
      setBusy(false);
    }
  };

  const toggle = async (partner: PartnerDto, field: 'isActive' | 'isFeatured') => {
    setBusy(true);
    setError('');
    try {
      const response = await partnersApi.update(partner.id, { [field]: !partner[field] });
      if (!response.success) throw new Error(response.message || t('admin.partners.errorSave'));
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('admin.partners.errorSave'));
    } finally {
      setBusy(false);
    }
  };

  const move = async (index: number, direction: -1 | 1) => {
    const target = index + direction;
    if (target < 0 || target >= partners.length || busy) return;
    const reordered = [...partners];
    [reordered[index], reordered[target]] = [reordered[target], reordered[index]];
    setPartners(reordered);
    setBusy(true);
    try {
      const response = await partnersApi.reorder(reordered.map((partner) => partner.id));
      if (response.success && response.data) setPartners(response.data);
      else throw new Error(response.message || t('admin.partners.errorOrder'));
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('admin.partners.errorOrder'));
      await load();
    } finally {
      setBusy(false);
    }
  };

  const remove = async (partner: PartnerDto) => {
    if (!window.confirm(t('admin.partners.confirmDelete', { name: partner.name }))) return;
    setBusy(true);
    try {
      const response = await partnersApi.delete(partner.id);
      if (!response.success) throw new Error(response.message || t('admin.partners.errorDelete'));
      setMessage(t('admin.partners.deleted'));
      await load();
    } catch (caught) {
      setError(caught instanceof Error ? caught.message : t('admin.partners.errorDelete'));
    } finally {
      setBusy(false);
    }
  };

  if (loading) return <div className="flex justify-center py-24"><div className="h-8 w-8 animate-spin border-2 border-line border-t-green" /></div>;

  return (
    <section className="space-y-5">
      <AdminPageHeader
        title={t('admin.partners.title')}
        subtitle={t('admin.partners.subtitle')}
        icon="ri-shake-hands-line"
        count={partners.length}
        actions={<Button onClick={openCreate}><i className="ri-add-line text-lg" />{t('admin.partners.add')}</Button>}
      />

      {(error || message) && (
        <div role="status" className={`admin-panel border-l-4 px-5 py-4 text-sm ${error ? 'border-l-error text-error' : 'border-l-green text-green'}`}>
          {error || message}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <Metric icon="ri-shake-hands-line" value={partners.length} label={t('admin.partners.metricTotal')} />
        <Metric icon="ri-eye-line" value={partners.filter((partner) => partner.isActive).length} label={t('admin.partners.metricPublished')} />
        <Metric icon="ri-layout-row-line" value={visiblePartners.length} label={t('admin.partners.metricMarquee')} />
      </div>

      <section className="admin-panel overflow-hidden">
        <header className="flex flex-wrap items-start justify-between gap-4 border-b border-line/60 bg-surface-container/55 px-5 py-4">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[.16em] text-green">{t('admin.partners.previewEyebrow')}</p>
            <h2 className="mt-1 font-display text-xl font-bold text-green-deep">{t('admin.partners.previewTitle')}</h2>
          </div>
          <span className="rounded-full border border-line bg-surface px-3 py-1.5 text-xs text-ink-variant">
            {t('admin.partners.previewCount', { count: visiblePartners.length })}
          </span>
        </header>
        <div className="relative overflow-hidden bg-white px-6 py-7 dark:bg-surface-container">
          {visiblePartners.length ? (
            <div className="flex items-center gap-12 overflow-x-auto pb-2">
              {visiblePartners.map((partner) => <PartnerMark key={partner.id} partner={partner} />)}
            </div>
          ) : (
            <p className="py-4 text-center text-sm text-ink-variant">{t('admin.partners.previewEmpty')}</p>
          )}
        </div>
      </section>

      <section className="admin-panel overflow-hidden">
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-line/60 px-5 py-4">
          <div>
            <h2 className="font-display text-xl font-bold text-green-deep">{t('admin.partners.libraryTitle')}</h2>
            <p className="mt-1 text-sm text-ink-variant">{t('admin.partners.libraryHint')}</p>
          </div>
          <div className="flex items-center gap-2 text-xs text-ink-variant"><i className="ri-drag-move-2-line" />{t('admin.partners.orderHint')}</div>
        </header>
        {partners.length === 0 ? (
          <div className="flex flex-col items-center px-6 py-16 text-center">
            <span className="flex h-14 w-14 items-center justify-center rounded-full bg-gold/15 text-gold-ink"><i className="ri-shake-hands-line text-2xl" /></span>
            <h3 className="mt-4 font-display text-xl text-green-deep">{t('admin.partners.emptyTitle')}</h3>
            <p className="mt-2 max-w-md text-sm text-ink-variant">{t('admin.partners.emptyDescription')}</p>
            <Button className="mt-5" onClick={openCreate}>{t('admin.partners.addFirst')}</Button>
          </div>
        ) : (
          <div className="divide-y divide-line/60">
            {partners.map((partner, index) => (
              <article key={partner.id} className={`grid items-center gap-4 px-5 py-4 transition-colors hover:bg-surface-container/45 lg:grid-cols-[64px_150px_1fr_auto_auto] ${partner.isActive ? '' : 'opacity-60'}`}>
                <div className="flex items-center gap-1">
                  <button type="button" disabled={busy || index === 0} onClick={() => void move(index, -1)} aria-label={t('admin.partners.moveUp')} className="flex h-8 w-7 items-center justify-center text-ink-variant hover:text-green disabled:opacity-25"><i className="ri-arrow-up-s-line text-lg" /></button>
                  <button type="button" disabled={busy || index === partners.length - 1} onClick={() => void move(index, 1)} aria-label={t('admin.partners.moveDown')} className="flex h-8 w-7 items-center justify-center text-ink-variant hover:text-green disabled:opacity-25"><i className="ri-arrow-down-s-line text-lg" /></button>
                </div>
                <div className="flex h-16 items-center justify-center rounded-xl border border-line/60 bg-white px-3 dark:bg-surface-container-high"><PartnerMark partner={partner} compact /></div>
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="font-display text-lg font-bold text-green-deep">{partner.name}</h3>
                    <StatusChip status={partner.isActive ? 'published' : 'draft'} label={partner.isActive ? t('admin.common.active') : t('admin.common.inactive')} />
                    {partner.isFeatured && <span className="rounded-full bg-gold/15 px-2.5 py-1 text-[10px] font-bold uppercase tracking-wider text-gold-ink">{t('admin.partners.featured')}</span>}
                  </div>
                  <p className="mt-1 truncate text-sm text-ink-variant">{partner.description || partner.websiteUrl || t('admin.partners.noDescription')}</p>
                </div>
                <div className="flex items-center gap-2 text-xs text-ink-variant">
                  <span className="rounded-lg border border-line px-2.5 py-1.5 tabular-nums">#{index + 1}</span>
                </div>
                <div className="flex items-center justify-end gap-1">
                  <button type="button" disabled={busy} onClick={() => void toggle(partner, 'isFeatured')} className={`flex h-10 w-10 items-center justify-center rounded-lg hover:bg-surface-container ${partner.isFeatured ? 'text-gold-ink' : 'text-ink-variant'}`} aria-label={t('admin.partners.toggleFeatured')}><i className={partner.isFeatured ? 'ri-star-fill' : 'ri-star-line'} /></button>
                  <button type="button" disabled={busy} onClick={() => void toggle(partner, 'isActive')} className="flex h-10 w-10 items-center justify-center rounded-lg text-green hover:bg-surface-container" aria-label={t('admin.partners.toggleVisibility')}><i className={partner.isActive ? 'ri-eye-line' : 'ri-eye-off-line'} /></button>
                  <button type="button" onClick={() => openEdit(partner)} className="flex h-10 w-10 items-center justify-center rounded-lg text-green hover:bg-surface-container" aria-label={t('admin.common.edit')}><i className="ri-edit-line" /></button>
                  <button type="button" disabled={busy} onClick={() => void remove(partner)} className="flex h-10 w-10 items-center justify-center rounded-lg text-error hover:bg-error/10" aria-label={t('admin.common.delete')}><i className="ri-delete-bin-line" /></button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>

      {editing && (
        <div className="fixed inset-0 z-[80] flex justify-end bg-ink/45 backdrop-blur-[2px]" role="dialog" aria-modal="true" aria-labelledby="partner-editor-title">
          <button type="button" className="absolute inset-0 cursor-default" onClick={closeEditor} aria-label={t('admin.common.close')} />
          <form onSubmit={submit} className="relative flex h-full w-full max-w-2xl flex-col overflow-hidden bg-background shadow-[-20px_0_70px_rgba(0,0,0,.18)]">
            <header className="flex items-center justify-between border-b border-line px-5 py-4 sm:px-7">
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[.18em] text-green">{t('admin.partners.editorEyebrow')}</p>
                <h2 id="partner-editor-title" className="mt-1 font-display text-2xl font-bold text-green-deep">{editing === 'new' ? t('admin.partners.createTitle') : t('admin.partners.editTitle')}</h2>
              </div>
              <button type="button" onClick={closeEditor} className="flex h-11 w-11 items-center justify-center rounded-full border border-line text-ink-variant hover:text-green" aria-label={t('admin.common.close')}><i className="ri-close-line text-xl" /></button>
            </header>

            <div className="flex-1 space-y-7 overflow-y-auto px-5 py-6 sm:px-7">
              <section>
                <p className="mb-4 text-[10px] font-bold uppercase tracking-[.16em] text-ink-variant">{t('admin.partners.identity')}</p>
                <div className="grid gap-4 sm:grid-cols-2">
                  <Field label={t('admin.partners.nameFr')} htmlFor="partner-name"><input id="partner-name" autoFocus required maxLength={160} className={inputClasses} value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} /></Field>
                  <Field label={t('admin.partners.nameEn')} htmlFor="partner-name-en"><input id="partner-name-en" maxLength={160} className={inputClasses} value={form.nameEn} onChange={(event) => setForm((current) => ({ ...current, nameEn: event.target.value }))} /></Field>
                  <Field label={t('admin.partners.descriptionFr')} htmlFor="partner-description"><textarea id="partner-description" maxLength={600} className={`${inputClasses} min-h-24 resize-y`} value={form.description} onChange={(event) => setForm((current) => ({ ...current, description: event.target.value }))} /></Field>
                  <Field label={t('admin.partners.descriptionEn')} htmlFor="partner-description-en"><textarea id="partner-description-en" maxLength={600} className={`${inputClasses} min-h-24 resize-y`} value={form.descriptionEn} onChange={(event) => setForm((current) => ({ ...current, descriptionEn: event.target.value }))} /></Field>
                </div>
              </section>

              <section className="border-t border-line pt-6">
                <p className="mb-4 text-[10px] font-bold uppercase tracking-[.16em] text-ink-variant">{t('admin.partners.brandAssets')}</p>
                <div className="grid gap-5 sm:grid-cols-[170px_1fr]">
                  <div className="flex h-32 items-center justify-center rounded-2xl border border-dashed border-green/30 bg-white p-5 dark:bg-surface-container">
                    {logoPreview ? <img src={logoPreview} alt="" className="max-h-full max-w-full object-contain" /> : <span className="text-center text-xs text-ink-variant"><i className="ri-image-add-line mb-2 block text-2xl text-green" />{t('admin.partners.logoPreview')}</span>}
                  </div>
                  <div className="space-y-4">
                    <Field label={t('admin.partners.logoFile')} htmlFor="partner-logo"><input id="partner-logo" type="file" accept="image/png,image/jpeg,image/webp,image/gif" className={`${inputClasses} file:mr-3 file:border-0 file:bg-transparent file:text-xs file:font-bold file:uppercase file:text-green`} onChange={(event) => { const file = event.target.files?.[0] ?? null; setLogoFile(file); if (file) setLogoPreview(URL.createObjectURL(file)); }} /></Field>
                    <Field label={t('admin.partners.logoUrl')} htmlFor="partner-logo-url"><input id="partner-logo-url" type="url" placeholder="https://…" className={inputClasses} value={form.logoUrl} onChange={(event) => { setForm((current) => ({ ...current, logoUrl: event.target.value })); if (!logoFile) setLogoPreview(event.target.value); }} /></Field>
                  </div>
                  <Field label={t('admin.partners.altFr')} htmlFor="partner-alt"><input id="partner-alt" maxLength={220} className={inputClasses} value={form.altText} onChange={(event) => setForm((current) => ({ ...current, altText: event.target.value }))} /></Field>
                  <Field label={t('admin.partners.altEn')} htmlFor="partner-alt-en"><input id="partner-alt-en" maxLength={220} className={inputClasses} value={form.altTextEn} onChange={(event) => setForm((current) => ({ ...current, altTextEn: event.target.value }))} /></Field>
                </div>
              </section>

              <section className="border-t border-line pt-6">
                <p className="mb-4 text-[10px] font-bold uppercase tracking-[.16em] text-ink-variant">{t('admin.partners.publishing')}</p>
                <div className="grid gap-4 sm:grid-cols-[1fr_120px]">
                  <Field label={t('admin.partners.website')} htmlFor="partner-website"><input id="partner-website" type="url" placeholder="https://…" className={inputClasses} value={form.websiteUrl} onChange={(event) => setForm((current) => ({ ...current, websiteUrl: event.target.value }))} /></Field>
                  <Field label={t('admin.common.order')} htmlFor="partner-order"><input id="partner-order" type="number" min={0} className={inputClasses} value={form.displayOrder} onChange={(event) => setForm((current) => ({ ...current, displayOrder: Number(event.target.value) }))} /></Field>
                </div>
                <div className="mt-5 grid gap-3 sm:grid-cols-2">
                  <Toggle checked={form.isActive} onChange={(checked) => setForm((current) => ({ ...current, isActive: checked }))} icon="ri-eye-line" title={t('admin.partners.publishTitle')} description={t('admin.partners.publishHint')} />
                  <Toggle checked={form.isFeatured} onChange={(checked) => setForm((current) => ({ ...current, isFeatured: checked }))} icon="ri-star-line" title={t('admin.partners.featureTitle')} description={t('admin.partners.featureHint')} />
                </div>
              </section>
            </div>

            <footer className="flex items-center justify-end gap-3 border-t border-line bg-surface-container/70 px-5 py-4 sm:px-7">
              <Button variant="tertiary" onClick={closeEditor}>{t('admin.common.cancel')}</Button>
              <Button type="submit" disabled={busy || !form.name.trim()}>{busy && <i className="ri-loader-4-line animate-spin" />}{t('admin.common.save')}</Button>
            </footer>
          </form>
        </div>
      )}
    </section>
  );
};

const Metric = ({ icon, value, label }: { icon: string; value: number; label: string }) => (
  <article className="admin-panel flex items-center gap-4 px-5 py-4">
    <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-green/10 text-green"><i className={icon} /></span>
    <div><p className="font-display text-2xl font-bold tabular-nums text-green-deep">{value}</p><p className="text-[10px] font-bold uppercase tracking-[.13em] text-ink-variant">{label}</p></div>
  </article>
);

const Toggle = ({ checked, onChange, icon, title, description }: { checked: boolean; onChange: (checked: boolean) => void; icon: string; title: string; description: string }) => (
  <label className={`flex cursor-pointer items-start gap-3 rounded-xl border p-4 transition-colors ${checked ? 'border-green/35 bg-green/5' : 'border-line bg-surface-container/35'}`}>
    <input type="checkbox" className="sr-only" checked={checked} onChange={(event) => onChange(event.target.checked)} />
    <span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-lg ${checked ? 'bg-green text-white' : 'bg-surface-container-high text-ink-variant'}`}><i className={icon} /></span>
    <span className="min-w-0"><span className="block text-sm font-semibold text-ink">{title}</span><span className="mt-1 block text-xs leading-5 text-ink-variant">{description}</span></span>
    <span className={`ml-auto mt-1 h-5 w-9 shrink-0 rounded-full p-0.5 transition-colors ${checked ? 'bg-green' : 'bg-line'}`}><span className={`block h-4 w-4 rounded-full bg-white transition-transform ${checked ? 'translate-x-4' : ''}`} /></span>
  </label>
);

export default AdminPartnersPage;

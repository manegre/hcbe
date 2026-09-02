import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../../components/admin/AdminPageHeader';
import { Button, EmptyState, Field, inputClasses } from '../../../../components/ui';
import { eventCategoriesApi } from '../../../../lib/api/event-categories';
import type { EventCategory } from '../../../../lib/api/types';

const emptyForm = { name: '', nameEn: '', slug: '', displayOrder: 0, isActive: true };

export const EventCategoriesPage = () => {
  const { t } = useTranslation();
  const [categories, setCategories] = useState<EventCategory[]>([]);
  const [editing, setEditing] = useState<EventCategory | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const load = async () => {
    try {
      setLoading(true);
      setError('');
      const response = await eventCategoriesApi.getCategoriesForAdmin();
      if (!response.success || !response.data) throw new Error(response.message);
      setCategories(response.data);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : t('admin.events.categories.loadError'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const beginCreate = () => {
    setEditing(null);
    setForm({ ...emptyForm, displayOrder: categories.length });
  };

  const beginEdit = (category: EventCategory) => {
    setEditing(category);
    setForm({
      name: category.name,
      nameEn: category.nameEn || '',
      slug: category.slug,
      displayOrder: category.displayOrder,
      isActive: category.isActive,
    });
  };

  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.name.trim()) return;
    try {
      setSaving(true);
      setError('');
      const response = editing
        ? await eventCategoriesApi.updateCategory(editing.id, {
            name: form.name.trim(),
            nameEn: form.nameEn.trim(),
            displayOrder: form.displayOrder,
            isActive: form.isActive,
          })
        : await eventCategoriesApi.createCategory({
            name: form.name.trim(),
            nameEn: form.nameEn.trim() || undefined,
            slug: form.slug.trim() || undefined,
            displayOrder: form.displayOrder,
            isActive: form.isActive,
          });
      if (!response.success) throw new Error(response.message);
      setEditing(null);
      setForm(emptyForm);
      await load();
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : t('admin.events.categories.saveError'));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (category: EventCategory) => {
    if (!window.confirm(t('admin.events.categories.confirmDelete', { name: category.name }))) return;
    const response = await eventCategoriesApi.deleteCategory(category.id);
    if (!response.success) {
      setError(response.message || t('admin.events.categories.deleteError'));
      return;
    }
    await load();
  };

  return (
    <section className="flex flex-col gap-5">
      <AdminPageHeader
        title={t('admin.events.categories.title')}
        subtitle={t('admin.events.categories.subtitle')}
        icon="ri-price-tag-3-line"
        count={categories.length}
        actions={
          <div className="flex flex-wrap gap-2">
            <Button to="/admin/events" variant="secondary">
              <i className="ri-arrow-left-line" aria-hidden="true" />
              {t('admin.events.backToList')}
            </Button>
            <Button type="button" onClick={beginCreate}>
              <i className="ri-add-line" aria-hidden="true" />
              {t('admin.events.categories.create')}
            </Button>
          </div>
        }
      />

      {error && (
        <div className="rounded-xl border border-error/30 bg-error/5 px-5 py-4 text-sm text-error" role="alert">
          {error}
        </div>
      )}

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_390px]">
        <div className="admin-panel overflow-hidden">
          <div className="border-b border-line bg-surface-container/60 px-5 py-4">
            <p className="text-[10px] font-bold uppercase tracking-[.14em] text-green">
              {t('admin.events.categories.registry')}
            </p>
            <p className="mt-1 text-sm text-ink-variant">{t('admin.events.categories.registryHint')}</p>
          </div>

          {loading ? (
            <div className="flex justify-center py-20"><span className="h-8 w-8 animate-spin rounded-full border-2 border-line border-t-green" /></div>
          ) : categories.length === 0 ? (
            <EmptyState title={t('admin.events.categories.empty')} />
          ) : (
            <ol className="divide-y divide-line/70">
              {categories.map((category, index) => (
                <li key={category.id} className="group grid grid-cols-[42px_minmax(0,1fr)_auto] items-center gap-4 px-5 py-4 transition-colors hover:bg-surface-container/50">
                  <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-green/8 font-display text-sm font-bold text-green">
                    {String(index + 1).padStart(2, '0')}
                  </span>
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <p className="font-semibold text-ink">{category.name}</p>
                      <span className={`rounded-full px-2 py-1 text-[9px] font-bold uppercase tracking-[.1em] ${category.isActive ? 'bg-green/10 text-green' : 'bg-surface-container-high text-ink-variant'}`}>
                        {category.isActive ? t('admin.events.categories.active') : t('admin.events.categories.inactive')}
                      </span>
                    </div>
                    <p className="mt-1 truncate text-xs text-ink-variant">
                      {category.nameEn || t('admin.events.categories.noEnglish')} · {category.slug}
                    </p>
                  </div>
                  <div className="flex items-center gap-1">
                    <button type="button" onClick={() => beginEdit(category)} className="flex h-10 w-10 items-center justify-center rounded-lg text-green transition-colors hover:bg-green/10" aria-label={t('admin.common.edit')}>
                      <i className="ri-edit-line" aria-hidden="true" />
                    </button>
                    <button type="button" onClick={() => void remove(category)} className="flex h-10 w-10 items-center justify-center rounded-lg text-ink-variant transition-colors hover:bg-error/10 hover:text-error" aria-label={t('admin.common.delete')}>
                      <i className="ri-delete-bin-line" aria-hidden="true" />
                    </button>
                  </div>
                </li>
              ))}
            </ol>
          )}
        </div>

        <aside className="admin-panel h-fit overflow-hidden xl:sticky xl:top-24">
          <div className="bg-green-deep px-6 py-5 text-white">
            <p className="text-[10px] font-bold uppercase tracking-[.15em] text-gold">
              {editing ? t('admin.events.categories.editEyebrow') : t('admin.events.categories.createEyebrow')}
            </p>
            <h2 className="mt-2 font-display text-2xl font-bold">
              {editing ? editing.name : t('admin.events.categories.newTitle')}
            </h2>
          </div>
          <form onSubmit={save} className="space-y-5 p-6">
            <Field label={t('admin.events.categories.nameFr')} htmlFor="category-name" required>
              <input id="category-name" className={inputClasses} maxLength={120} value={form.name} onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} />
            </Field>
            <Field label={t('admin.events.categories.nameEn')} htmlFor="category-name-en">
              <input id="category-name-en" className={inputClasses} maxLength={120} value={form.nameEn} onChange={(event) => setForm((current) => ({ ...current, nameEn: event.target.value }))} />
            </Field>
            <Field label={t('admin.events.categories.slug')} htmlFor="category-slug" hint={editing ? t('admin.events.categories.slugLocked') : t('admin.events.categories.slugHint')}>
              <input id="category-slug" className={inputClasses} maxLength={80} disabled={Boolean(editing)} value={form.slug} onChange={(event) => setForm((current) => ({ ...current, slug: event.target.value }))} placeholder="professional-development" />
            </Field>
            <Field label={t('admin.common.order')} htmlFor="category-order">
              <input id="category-order" type="number" min={0} className={inputClasses} value={form.displayOrder} onChange={(event) => setForm((current) => ({ ...current, displayOrder: Number(event.target.value) }))} />
            </Field>
            <label className="flex cursor-pointer items-center justify-between rounded-xl border border-line bg-surface-container/50 p-4">
              <span><span className="block text-sm font-semibold text-ink">{t('admin.events.categories.visible')}</span><span className="mt-1 block text-xs text-ink-variant">{t('admin.events.categories.visibleHint')}</span></span>
              <input type="checkbox" checked={form.isActive} onChange={(event) => setForm((current) => ({ ...current, isActive: event.target.checked }))} className="h-5 w-5 accent-green" />
            </label>
            <div className="flex justify-end gap-2 border-t border-line pt-5">
              {editing && <Button type="button" variant="tertiary" onClick={beginCreate}>{t('admin.common.cancel')}</Button>}
              <Button type="submit" disabled={saving || !form.name.trim()}>{saving && <i className="ri-loader-4-line animate-spin" />}{t('admin.common.save')}</Button>
            </div>
          </form>
        </aside>
      </div>
    </section>
  );
};

export default EventCategoriesPage;

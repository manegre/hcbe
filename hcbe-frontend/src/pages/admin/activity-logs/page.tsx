import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button, Field, inputClasses } from '../../../components/ui';
import { auditApi, type AuditLogQuery } from '../../../lib/api/audit';
import type { AuditLog, AuditLogPage } from '../../../lib/api/types';

const pageSize = 25;

const actionTone = (action: string) => {
  const value = action.toLowerCase();
  if (value.includes('delete') || value.includes('fail') || value.includes('revoke')) return 'border-error/20 bg-error/10 text-error';
  if (value.includes('add') || value.includes('create') || value.includes('enable') || value.includes('verify')) return 'border-green/20 bg-green/10 text-green';
  if (value.includes('modify') || value.includes('update') || value.includes('change')) return 'border-gold/35 bg-gold/12 text-gold-ink';
  return 'border-line bg-surface-container text-ink-variant';
};

const actionIcon = (action: string) => {
  const value = action.toLowerCase();
  if (value.includes('delete') || value.includes('revoke')) return 'ri-delete-bin-line';
  if (value.includes('add') || value.includes('create')) return 'ri-add-circle-line';
  if (value.includes('login') || value.includes('session')) return 'ri-login-circle-line';
  if (value.includes('mfa') || value.includes('security')) return 'ri-shield-keyhole-line';
  if (value.includes('modify') || value.includes('update') || value.includes('change')) return 'ri-edit-line';
  return 'ri-history-line';
};

const initials = (email?: string) => (email?.split('@')[0]?.slice(0, 2) || 'SY').toUpperCase();

const parseChanges = (value?: string): Array<[string, string]> => {
  if (!value) return [];
  try {
    const parsed = JSON.parse(value) as Record<string, unknown>;
    return Object.entries(parsed).map(([key, item]) => [key, item == null ? '—' : String(item)]);
  } catch {
    return [['Détails', value]];
  }
};

export default function AdminActivityLogsPage() {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const copy = fr ? {
    title: 'Journal des activités', subtitle: 'Une piste chronologique des actions administratives et des changements apportés aux données.',
    refresh: 'Actualiser', filters: 'Affiner le journal', search: 'Rechercher', searchHint: 'Acteur, action, identifiant ou trace…', actor: 'Acteur', actorHint: 'Courriel de l’administrateur', action: 'Action', module: 'Module', from: 'Du', to: 'Au', allActions: 'Toutes les actions', allModules: 'Tous les modules', apply: 'Appliquer', reset: 'Réinitialiser',
    today: 'Actions aujourd’hui', activeActors: 'Acteurs sur 30 jours', security: 'Événements de sécurité', retention: 'Conservation', days: 'jours',
    timeline: 'Chronologie opérationnelle', result: 'résultat', results: 'résultats', date: 'Date et heure', target: 'Élément concerné', details: 'Détails', view: 'Consulter', system: 'Système', emptyTitle: 'Aucune activité trouvée', emptyText: 'Modifiez les filtres ou la période pour afficher d’autres entrées.', error: 'Impossible de charger le journal des activités.', loading: 'Chargement du journal…',
    previous: 'Précédent', next: 'Suivant', page: 'Page', of: 'sur', close: 'Fermer', eventDetails: 'Détail de l’activité', identity: 'Identité et traçabilité', timestamp: 'Horodatage', ip: 'Adresse IP', trace: 'Identifiant de trace', entityId: 'Identifiant de l’élément', changes: 'Données enregistrées', noChanges: 'Aucun détail de champ n’a été enregistré pour cette action.', redacted: 'Les secrets et données d’authentification sont masqués automatiquement.',
    Added: 'Ajout', Modified: 'Modification', Deleted: 'Suppression',
  } : {
    title: 'Activity log', subtitle: 'A chronological audit trail of administrative actions and data changes.',
    refresh: 'Refresh', filters: 'Refine the log', search: 'Search', searchHint: 'Actor, action, identifier or trace…', actor: 'Actor', actorHint: 'Administrator email', action: 'Action', module: 'Module', from: 'From', to: 'To', allActions: 'All actions', allModules: 'All modules', apply: 'Apply', reset: 'Reset',
    today: 'Actions today', activeActors: 'Actors in 30 days', security: 'Security events', retention: 'Retention', days: 'days',
    timeline: 'Operational timeline', result: 'result', results: 'results', date: 'Date and time', target: 'Affected item', details: 'Details', view: 'View', system: 'System', emptyTitle: 'No activity found', emptyText: 'Adjust the filters or date range to display other entries.', error: 'Unable to load the activity log.', loading: 'Loading activity log…',
    previous: 'Previous', next: 'Next', page: 'Page', of: 'of', close: 'Close', eventDetails: 'Activity details', identity: 'Identity and traceability', timestamp: 'Timestamp', ip: 'IP address', trace: 'Trace identifier', entityId: 'Item identifier', changes: 'Recorded data', noChanges: 'No field details were recorded for this action.', redacted: 'Secrets and authentication data are automatically redacted.',
    Added: 'Added', Modified: 'Modified', Deleted: 'Deleted',
  };

  const emptyFilters = { search: '', userEmail: '', action: '', entityType: '', from: '', to: '' };
  const [filters, setFilters] = useState(emptyFilters);
  const [query, setQuery] = useState<AuditLogQuery>({ page: 1, pageSize });
  const [data, setData] = useState<AuditLogPage | null>(null);
  const [selected, setSelected] = useState<AuditLog | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = async (currentQuery = query) => {
    setLoading(true);
    setError('');
    try {
      const response = await auditApi.list(currentQuery);
      if (!response.success || !response.data) throw new Error(response.message);
      setData(response.data);
    } catch {
      setError(copy.error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(query); }, [query]);
  useEffect(() => {
    if (!selected) return;
    const close = (event: KeyboardEvent) => event.key === 'Escape' && setSelected(null);
    window.addEventListener('keydown', close);
    return () => window.removeEventListener('keydown', close);
  }, [selected]);

  const localizeAction = (value: string) => copy[value as keyof typeof copy] || value.replace(/([a-z])([A-Z])/g, '$1 $2');
  const humanizeEntity = (value: string) => value.replace(/([a-z])([A-Z])/g, '$1 $2');
  const formatDate = (value: string) => new Date(value).toLocaleString(locale, { dateStyle: 'medium', timeStyle: 'short' });
  const selectedChanges = useMemo(() => parseChanges(selected?.changesJson), [selected]);

  const applyFilters = (event: React.FormEvent) => {
    event.preventDefault();
    const fromUtc = filters.from ? new Date(`${filters.from}T00:00:00`).toISOString() : undefined;
    const toUtc = filters.to ? new Date(`${filters.to}T23:59:59.999`).toISOString() : undefined;
    setQuery({ page: 1, pageSize, search: filters.search.trim(), userEmail: filters.userEmail.trim(), action: filters.action, entityType: filters.entityType, fromUtc, toUtc });
  };

  const resetFilters = () => {
    setFilters(emptyFilters);
    setQuery({ page: 1, pageSize });
  };

  const stats = [
    [copy.today, data?.stats.eventsToday ?? 0, 'ri-flashlight-line', 'text-gold-ink bg-gold/15'],
    [copy.activeActors, data?.stats.activeActors ?? 0, 'ri-team-line', 'text-green bg-green/10'],
    [copy.security, data?.stats.securityEvents ?? 0, 'ri-shield-check-line', 'text-red-link bg-red-link/10'],
    [copy.retention, `${data?.stats.retentionDays ?? 730} ${copy.days}`, 'ri-archive-drawer-line', 'text-ink-variant bg-surface-container'],
  ];

  return (
    <div className="space-y-6 pb-12">
      <AdminPageHeader
        title={copy.title}
        subtitle={copy.subtitle}
        icon="ri-history-line"
        count={data?.total ?? 0}
        actions={<Button variant="secondary" onClick={() => void load()} disabled={loading}><i className="ri-refresh-line" aria-hidden="true" />{copy.refresh}</Button>}
      />

      {error && <div role="alert" className="rounded-2xl border border-error/25 bg-error/5 p-4 text-sm text-error">{error}</div>}

      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4" aria-label={fr ? 'Résumé du journal' : 'Log summary'}>
        {stats.map(([label, value, icon, tone]) => (
          <article key={String(label)} className="group relative overflow-hidden rounded-[20px] border border-line/70 bg-surface p-5 shadow-[0_14px_36px_rgba(0,59,27,.045)]">
            <div className="flex items-start justify-between gap-4">
              <div><strong className="block font-display text-[30px] leading-none text-green-deep tabular-nums">{value}</strong><span className="mt-2 block text-[10px] font-bold uppercase tracking-[.14em] text-ink-variant">{label}</span></div>
              <span className={`flex h-10 w-10 items-center justify-center rounded-xl ${tone}`}><i className={`${icon} text-lg`} aria-hidden="true" /></span>
            </div>
            <span className="absolute inset-x-0 bottom-0 h-0.5 origin-left scale-x-0 bg-gold transition-transform duration-300 group-hover:scale-x-100" aria-hidden="true" />
          </article>
        ))}
      </section>

      <form onSubmit={applyFilters} className="rounded-[24px] border border-line/70 bg-surface p-5 shadow-[0_16px_40px_rgba(0,59,27,.045)] sm:p-6">
        <div className="mb-5 flex items-center gap-3"><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green text-gold"><i className="ri-equalizer-2-line" aria-hidden="true" /></span><h2 className="font-display text-xl font-bold text-green-deep">{copy.filters}</h2></div>
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-12">
          <Field label={copy.search} htmlFor="audit-search" className="xl:col-span-3"><div className="relative"><i className="ri-search-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-ink-variant" aria-hidden="true" /><input id="audit-search" maxLength={120} className={`${inputClasses} pl-11`} placeholder={copy.searchHint} value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} /></div></Field>
          <Field label={copy.actor} htmlFor="audit-actor" className="xl:col-span-2"><input id="audit-actor" type="email" maxLength={254} className={inputClasses} placeholder={copy.actorHint} value={filters.userEmail} onChange={(event) => setFilters({ ...filters, userEmail: event.target.value })} /></Field>
          <Field label={copy.action} htmlFor="audit-action" className="xl:col-span-2"><select id="audit-action" className={inputClasses} value={filters.action} onChange={(event) => setFilters({ ...filters, action: event.target.value })}><option value="">{copy.allActions}</option>{data?.filters.actions.map((value) => <option value={value} key={value}>{localizeAction(value)}</option>)}</select></Field>
          <Field label={copy.module} htmlFor="audit-module" className="xl:col-span-2"><select id="audit-module" className={inputClasses} value={filters.entityType} onChange={(event) => setFilters({ ...filters, entityType: event.target.value })}><option value="">{copy.allModules}</option>{data?.filters.entityTypes.map((value) => <option value={value} key={value}>{humanizeEntity(value)}</option>)}</select></Field>
          <div className="grid grid-cols-2 gap-3 md:col-span-2 xl:col-span-3"><Field label={copy.from} htmlFor="audit-from"><input id="audit-from" type="date" className={inputClasses} value={filters.from} max={filters.to || undefined} onChange={(event) => setFilters({ ...filters, from: event.target.value })} /></Field><Field label={copy.to} htmlFor="audit-to"><input id="audit-to" type="date" className={inputClasses} value={filters.to} min={filters.from || undefined} onChange={(event) => setFilters({ ...filters, to: event.target.value })} /></Field></div>
        </div>
        <div className="mt-5 flex flex-wrap gap-3"><Button type="submit" disabled={loading}><i className="ri-filter-3-line" aria-hidden="true" />{copy.apply}</Button><Button type="button" variant="tertiary" onClick={resetFilters}>{copy.reset}</Button></div>
      </form>

      <section className="overflow-hidden rounded-[24px] border border-line/70 bg-surface shadow-[0_18px_45px_rgba(0,59,27,.05)]">
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-line/70 px-5 py-5 sm:px-7">
          <div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{fr ? 'Piste d’audit' : 'Audit trail'}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{copy.timeline}</h2></div>
          <p className="text-xs font-semibold text-ink-variant"><span className="text-green-deep tabular-nums">{data?.total ?? 0}</span> {(data?.total ?? 0) === 1 ? copy.result : copy.results}</p>
        </div>

        {loading ? <div className="space-y-3 p-6" role="status"><span className="sr-only">{copy.loading}</span>{Array.from({ length: 6 }).map((_, index) => <div key={index} className="h-[76px] animate-pulse rounded-2xl bg-surface-container" />)}</div> : !data?.items.length ? (
          <div className="px-6 py-16 text-center"><span className="mx-auto flex h-14 w-14 items-center justify-center rounded-full border border-dashed border-green/30 bg-green/5 text-2xl text-green"><i className="ri-search-eye-line" aria-hidden="true" /></span><h3 className="mt-4 font-display text-2xl font-bold text-green-deep">{copy.emptyTitle}</h3><p className="mx-auto mt-2 max-w-md text-sm leading-6 text-ink-variant">{copy.emptyText}</p></div>
        ) : <>
          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full border-collapse text-left">
              <thead className="bg-surface-container"><tr>{[copy.date, copy.actor, copy.action, copy.target, copy.details].map((label) => <th key={label} className="px-5 py-3 text-[9px] font-bold uppercase tracking-[.15em] text-ink-variant first:pl-7 last:pr-7">{label}</th>)}</tr></thead>
              <tbody className="divide-y divide-line/70">{data.items.map((item) => <tr key={item.id} className="group transition-colors hover:bg-green/[.025]">
                <td className="whitespace-nowrap py-4 pl-7 pr-5 text-xs text-ink-variant">{formatDate(item.createdAtUtc)}</td>
                <td className="px-5 py-4"><div className="flex min-w-[180px] items-center gap-3"><span className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-green text-[10px] font-bold text-white">{initials(item.userEmail)}</span><span className="max-w-[230px] truncate text-sm font-semibold text-ink">{item.userEmail || copy.system}</span></div></td>
                <td className="px-5 py-4"><span className={`inline-flex items-center gap-2 rounded-full border px-3 py-1.5 text-[10px] font-bold uppercase tracking-[.08em] ${actionTone(item.action)}`}><i className={actionIcon(item.action)} aria-hidden="true" />{localizeAction(item.action)}</span></td>
                <td className="px-5 py-4"><strong className="block text-sm text-ink">{humanizeEntity(item.entityType)}</strong>{item.entityId && <span className="mt-1 block max-w-[210px] truncate font-mono text-[10px] text-ink-variant">{item.entityId}</span>}</td>
                <td className="py-4 pl-5 pr-7 text-right"><button type="button" onClick={() => setSelected(item)} className="inline-flex min-h-10 items-center gap-2 rounded-xl px-3 text-[10px] font-bold uppercase tracking-[.1em] text-green transition-colors hover:bg-green/8 focus-visible:outline focus-visible:outline-2 focus-visible:outline-green">{copy.view}<i className="ri-arrow-right-line" aria-hidden="true" /></button></td>
              </tr>)}</tbody>
            </table>
          </div>

          <div className="divide-y divide-line/70 lg:hidden">{data.items.map((item) => <article key={item.id} className="p-5">
            <div className="flex items-start gap-3"><span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-green text-[10px] font-bold text-white">{initials(item.userEmail)}</span><div className="min-w-0 flex-1"><p className="truncate text-sm font-semibold text-ink">{item.userEmail || copy.system}</p><time className="mt-1 block text-[11px] text-ink-variant">{formatDate(item.createdAtUtc)}</time></div><span className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-xl border ${actionTone(item.action)}`}><i className={actionIcon(item.action)} aria-hidden="true" /></span></div>
            <div className="mt-4 flex items-end justify-between gap-3"><div><span className="text-xs font-bold text-green">{localizeAction(item.action)}</span><p className="mt-1 text-sm text-ink-variant">{humanizeEntity(item.entityType)}</p></div><button type="button" onClick={() => setSelected(item)} className="min-h-10 rounded-xl border border-green/30 px-3 text-[10px] font-bold uppercase tracking-[.1em] text-green">{copy.view}</button></div>
          </article>)}</div>

          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-line/70 bg-surface-container/50 px-5 py-4 sm:px-7">
            <p className="text-xs text-ink-variant">{copy.page} <strong className="text-green-deep">{data.page}</strong> {copy.of} <strong className="text-green-deep">{data.totalPages}</strong></p>
            <div className="flex gap-2"><Button variant="secondary" className="min-h-[40px] px-4 py-2" disabled={data.page <= 1 || loading} onClick={() => setQuery({ ...query, page: Math.max(1, (query.page ?? 1) - 1) })}><i className="ri-arrow-left-s-line" aria-hidden="true" />{copy.previous}</Button><Button variant="secondary" className="min-h-[40px] px-4 py-2" disabled={data.page >= data.totalPages || loading} onClick={() => setQuery({ ...query, page: (query.page ?? 1) + 1 })}>{copy.next}<i className="ri-arrow-right-s-line" aria-hidden="true" /></Button></div>
          </div>
        </>}
      </section>

      {selected && <div className="fixed inset-0 z-[80] flex justify-end" role="dialog" aria-modal="true" aria-labelledby="audit-detail-title"><button type="button" className="absolute inset-0 bg-ink/55 backdrop-blur-[2px]" aria-label={copy.close} onClick={() => setSelected(null)} /><aside className="relative h-full w-full max-w-xl overflow-y-auto border-l border-line bg-surface p-5 shadow-[-24px_0_70px_rgba(0,0,0,.18)] sm:p-8">
        <div className="flex items-start justify-between gap-4"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{localizeAction(selected.action)}</p><h2 id="audit-detail-title" className="mt-1 font-display text-3xl font-bold text-green-deep">{copy.eventDetails}</h2></div><button type="button" onClick={() => setSelected(null)} className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-line text-green transition hover:bg-green hover:text-white" aria-label={copy.close}><i className="ri-close-line text-xl" aria-hidden="true" /></button></div>
        <div className="mt-7 rounded-[22px] bg-green-deep p-6 text-white"><div className="flex items-center gap-3"><span className="flex h-11 w-11 items-center justify-center rounded-full bg-gold font-bold text-green-deep">{initials(selected.userEmail)}</span><div className="min-w-0"><p className="truncate font-semibold">{selected.userEmail || copy.system}</p><p className="mt-1 text-xs text-white/65">{formatDate(selected.createdAtUtc)}</p></div></div><div className="mt-6 border-t border-white/15 pt-5"><span className="text-[9px] font-bold uppercase tracking-[.16em] text-gold">{copy.target}</span><p className="mt-2 font-display text-2xl font-bold">{humanizeEntity(selected.entityType)}</p></div></div>
        <section className="mt-7"><h3 className="font-display text-xl font-bold text-green-deep">{copy.identity}</h3><dl className="mt-4 divide-y divide-line rounded-2xl border border-line">{[[copy.timestamp, formatDate(selected.createdAtUtc)], [copy.ip, selected.ipAddress || '—'], [copy.trace, selected.traceId || '—'], [copy.entityId, selected.entityId || '—']].map(([label, value]) => <div key={label} className="grid gap-1 px-4 py-3 sm:grid-cols-[150px_1fr]"><dt className="text-[10px] font-bold uppercase tracking-[.1em] text-ink-variant">{label}</dt><dd className="break-all font-mono text-xs text-ink">{value}</dd></div>)}</dl></section>
        <section className="mt-7"><div className="flex items-center justify-between gap-3"><h3 className="font-display text-xl font-bold text-green-deep">{copy.changes}</h3><i className="ri-lock-2-line text-green" aria-hidden="true" /></div>{selectedChanges.length ? <dl className="mt-4 space-y-2">{selectedChanges.map(([key, value]) => <div key={key} className="rounded-2xl border border-line bg-surface-container/55 p-4"><dt className="text-[10px] font-bold uppercase tracking-[.1em] text-green">{humanizeEntity(key)}</dt><dd className="mt-2 break-words text-sm leading-6 text-ink">{value}</dd></div>)}</dl> : <p className="mt-4 rounded-2xl border border-dashed border-line p-5 text-sm leading-6 text-ink-variant">{copy.noChanges}</p>}<p className="mt-4 flex items-start gap-2 text-xs leading-5 text-ink-variant"><i className="ri-shield-check-line mt-0.5 text-green" aria-hidden="true" />{copy.redacted}</p></section>
      </aside></div>}
    </div>
  );
}

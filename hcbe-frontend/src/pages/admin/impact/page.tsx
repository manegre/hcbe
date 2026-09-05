import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { impactApi } from '../../../lib/api/impact';
import type { ImpactDashboard, MemberDimension } from '../../../lib/api/types';

const colors = ['bg-green', 'bg-gold', 'bg-red-link', 'bg-green-muted'];

function DimensionList({ items, empty, translate }: { items: MemberDimension[]; empty: string; translate: (item: MemberDimension) => string }) {
  return <div className="space-y-4">{items.length === 0 ? <p className="text-sm text-ink-variant">{empty}</p> : items.map((item) => <div key={item.key}>
    <div className="mb-2 flex items-center justify-between gap-4 text-xs"><span className="font-semibold text-green-deep">{translate(item)}</span><span className="tabular-nums text-ink-variant">{item.count} · {item.percentage}%</span></div>
    <div className="h-2 overflow-hidden rounded-full bg-green/[.07]"><div className="h-full rounded-full bg-green transition-[width]" style={{ width: `${Math.min(100, item.percentage)}%` }} /></div>
  </div>)}</div>;
}

export default function ImpactPage() {
  const { t, i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [data, setData] = useState<ImpactDashboard | null>(null);
  const [error, setError] = useState('');
  const [exporting, setExporting] = useState(false);
  useEffect(() => { impactApi.get().then((response) => response.data ? setData(response.data) : setError(response.message ?? t('admin.impact.error'))).catch(() => setError(t('admin.impact.error'))); }, [t]);
  const max = useMemo(() => Math.max(1, ...(data?.periods.flatMap((item) => [item.newMembers, item.eventRegistrations, item.serviceRequests, item.opportunityApplications]) ?? [1])), [data]);
  const legend = ['newMembers', 'eventRegistrations', 'serviceRequests', 'opportunityApplications'].map((key) => t(`admin.impact.legend.${key}`));
  const exportCsv = async () => {
    setExporting(true); setError('');
    try {
      const result = await impactApi.exportCsv();
      const url = URL.createObjectURL(result.blob); const link = document.createElement('a');
      link.href = url; link.download = result.fileName || 'hcbe-activation.csv'; link.click(); URL.revokeObjectURL(url);
    } catch { setError(fr ? 'Impossible d’exporter le rapport.' : 'Unable to export the report.'); }
    finally { setExporting(false); }
  };

  return <div className="space-y-6">
    <AdminPageHeader title={t('admin.impact.title')} subtitle={t('admin.impact.subtitle')} icon="ri-line-chart-line" count={data?.metrics.length} actions={
      <button type="button" onClick={() => void exportCsv()} disabled={!data || exporting} className="inline-flex min-h-11 items-center gap-2 rounded-xl border border-green/20 bg-surface px-4 text-[10px] font-bold uppercase tracking-[.12em] text-green transition hover:border-green disabled:opacity-50"><i className={exporting ? 'ri-loader-4-line animate-spin' : 'ri-download-2-line'} />{fr ? 'Exporter les données' : 'Export data'}</button>
    } />
    {error && <p className="rounded-xl border border-error/20 bg-error/5 p-4 text-error">{error}</p>}
    {!data ? <div className="h-48 animate-pulse rounded-2xl bg-surface" /> : <>
      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{data.metrics.map((metric) => <article key={metric.key} className="rounded-2xl border border-line bg-surface p-5 shadow-[0_12px_32px_rgba(0,59,27,.045)]"><div className="flex items-start justify-between"><p className="max-w-[70%] text-[10px] font-bold uppercase leading-5 tracking-[.12em] text-ink-variant">{t(`admin.impact.metric.${metric.key}`, { defaultValue: metric.label })}</p><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green/8 text-green"><i className="ri-pulse-line" /></span></div><div className="mt-5 flex items-end gap-2"><strong className="font-display text-4xl text-green-deep">{metric.value.toLocaleString(locale)}</strong><span className="pb-1 text-xs text-ink-variant">{t(`admin.impact.unit.${metric.unit}`, { defaultValue: metric.unit })}</span></div>{metric.changePercent != null && <p className={`mt-2 text-xs font-bold ${metric.changePercent >= 0 ? 'text-green' : 'text-error'}`}>{metric.changePercent >= 0 ? '↑' : '↓'} {t('admin.impact.change', { count: Math.abs(metric.changePercent) })}</p>}</article>)}</section>

      <section className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.05)]">
        <header className="grid gap-4 border-b border-line bg-green/[.035] p-6 sm:grid-cols-[1fr_auto] sm:items-end sm:px-8"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{fr ? 'Activation membre' : 'Member activation'}</p><h2 className="mt-2 font-display text-2xl font-bold text-green-deep">{fr ? 'Du compte à la première participation' : 'From account to first participation'}</h2><p className="mt-2 max-w-2xl text-sm text-ink-variant">{fr ? 'Le tunnel utilise uniquement des jalons fonctionnels et ne collecte aucun contenu personnel supplémentaire.' : 'The funnel uses functional milestones only and collects no additional personal content.'}</p></div><span className="rounded-full border border-green/15 bg-surface px-3 py-2 text-[9px] font-bold uppercase tracking-[.12em] text-green"><i className="ri-shield-check-line mr-2" />{fr ? 'Agrégé · Loi 25' : 'Aggregated · Law 25'}</span></header>
        <div className="grid gap-3 p-5 sm:grid-cols-5 sm:p-7">{data.activationFunnel.map((stage, index) => <article key={stage.key} className="relative rounded-2xl border border-line bg-canvas/40 p-4"><span className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">0{index + 1}</span><strong className="mt-4 block font-display text-3xl text-green-deep">{stage.percentage}%</strong><p className="mt-2 text-xs font-semibold leading-5 text-green-deep">{stage.label}</p><p className="mt-1 text-[11px] text-ink-variant">{stage.count} {fr ? 'membres' : 'members'}</p></article>)}</div>
      </section>

      <div className="grid gap-5 lg:grid-cols-2">
        <section className="rounded-[24px] border border-line bg-surface p-6 sm:p-7"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Santé de la communauté' : 'Community health'}</p><h2 className="mb-6 mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Activité récente' : 'Recent activity'}</h2><DimensionList items={data.activitySegments} empty={fr ? 'Aucune activité disponible.' : 'No activity available.'} translate={(item) => fr ? item.label : ({ active: 'Active — 30 days', warm: 'Re-engage — 31 to 60 days', dormant: 'Dormant — over 60 days', never: 'Never signed in' }[item.key] || item.label)} /></section>
        <section className="rounded-[24px] border border-line bg-surface p-6 sm:p-7"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Présence nationale' : 'National presence'}</p><h2 className="mb-2 mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Répartition par province' : 'Breakdown by province'}</h2><p className="mb-6 text-xs leading-5 text-ink-variant">{fr ? 'Les groupes de moins de trois personnes sont regroupés afin de réduire le risque de réidentification.' : 'Groups with fewer than three people are combined to reduce re-identification risk.'}</p><DimensionList items={data.provinceBreakdown} empty={fr ? 'Aucune province renseignée.' : 'No province provided.'} translate={(item) => item.key === 'other' && !fr ? 'Other regions (grouped)' : item.label} /></section>
      </div>

      <section className="rounded-[24px] border border-line bg-surface p-5 sm:p-7"><div className="flex items-end justify-between"><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{t('admin.impact.sixMonths')}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{t('admin.impact.activity')}</h2></div><p className="hidden text-xs text-ink-variant sm:block">{t('admin.impact.activitySummary')}</p></div><div className="mt-8 grid grid-cols-6 gap-3">{data.periods.map((period) => <div key={period.period} className="flex min-w-0 flex-col items-center"><div className="flex h-56 w-full items-end justify-center gap-1 border-b border-line px-1">{[period.newMembers, period.eventRegistrations, period.serviceRequests, period.opportunityApplications].map((value, index) => <div key={index} title={`${value}`} className={`w-1/5 min-w-1 rounded-t ${colors[index]}`} style={{ height: `${Math.max(value ? 5 : 0, value / max * 100)}%` }} />)}</div><span className="mt-2 text-[9px] font-bold text-ink-variant">{period.period.slice(5)}</span></div>)}</div><div className="mt-5 flex flex-wrap gap-4 text-xs">{legend.map((label, index) => <span key={label} className="flex items-center gap-2"><span className={`h-2.5 w-2.5 rounded-full ${colors[index]}`} />{label}</span>)}</div></section>
    </>}
  </div>;
}

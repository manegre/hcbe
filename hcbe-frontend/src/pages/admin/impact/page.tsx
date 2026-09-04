import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { impactApi } from '../../../lib/api/impact';
import type { ImpactDashboard } from '../../../lib/api/types';

const colors = ['bg-green', 'bg-gold', 'bg-red-link', 'bg-green-muted'];
export default function ImpactPage() {
  const { t, i18n } = useTranslation();
  const locale = i18n.language.startsWith('en') ? 'en-CA' : 'fr-CA';
  const [data, setData] = useState<ImpactDashboard | null>(null); const [error, setError] = useState('');
  useEffect(() => { impactApi.get().then((response) => response.data ? setData(response.data) : setError(response.message ?? t('admin.impact.error'))).catch(() => setError(t('admin.impact.error'))); }, [t]);
  const max = useMemo(() => Math.max(1, ...(data?.periods.flatMap((item) => [item.newMembers, item.eventRegistrations, item.serviceRequests, item.opportunityApplications]) ?? [1])), [data]);
  const legend = ['newMembers', 'eventRegistrations', 'serviceRequests', 'opportunityApplications'].map((key) => t(`admin.impact.legend.${key}`));
  return <div className="space-y-6"><AdminPageHeader title={t('admin.impact.title')} subtitle={t('admin.impact.subtitle')} icon="ri-line-chart-line" count={data?.metrics.length} />
    {error && <p className="rounded-xl border border-error/20 bg-error/5 p-4 text-error">{error}</p>}
    {!data ? <div className="h-48 animate-pulse rounded-2xl bg-surface" /> : <><section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">{data.metrics.map((metric) => <article key={metric.key} className="rounded-2xl border border-line bg-surface p-5 shadow-[0_12px_32px_rgba(0,59,27,.045)]"><div className="flex items-start justify-between"><p className="max-w-[70%] text-[10px] font-bold uppercase leading-5 tracking-[.12em] text-ink-variant">{t(`admin.impact.metric.${metric.key}`, { defaultValue: metric.label })}</p><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green/8 text-green"><i className="ri-pulse-line" /></span></div><div className="mt-5 flex items-end gap-2"><strong className="font-display text-4xl text-green-deep">{metric.value.toLocaleString(locale)}</strong><span className="pb-1 text-xs text-ink-variant">{t(`admin.impact.unit.${metric.unit}`, { defaultValue: metric.unit })}</span></div>{metric.changePercent != null && <p className={`mt-2 text-xs font-bold ${metric.changePercent >= 0 ? 'text-green' : 'text-error'}`}>{metric.changePercent >= 0 ? '↑' : '↓'} {t('admin.impact.change', { count: Math.abs(metric.changePercent) })}</p>}</article>)}</section>
    <section className="rounded-[24px] border border-line bg-surface p-5 sm:p-7"><div className="flex items-end justify-between"><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{t('admin.impact.sixMonths')}</p><h2 className="mt-1 font-display text-2xl font-bold text-green-deep">{t('admin.impact.activity')}</h2></div><p className="text-xs text-ink-variant">{t('admin.impact.activitySummary')}</p></div><div className="mt-8 grid grid-cols-6 gap-3">{data.periods.map((period) => <div key={period.period} className="flex min-w-0 flex-col items-center"><div className="flex h-56 w-full items-end justify-center gap-1 border-b border-line px-1">{[period.newMembers, period.eventRegistrations, period.serviceRequests, period.opportunityApplications].map((value, index) => <div key={index} title={`${value}`} className={`w-1/5 min-w-1 rounded-t ${colors[index]}`} style={{ height: `${Math.max(value ? 5 : 0, value / max * 100)}%` }} />)}</div><span className="mt-2 text-[9px] font-bold text-ink-variant">{period.period.slice(5)}</span></div>)}</div><div className="mt-5 flex flex-wrap gap-4 text-xs">{legend.map((label, index) => <span key={label} className="flex items-center gap-2"><span className={`h-2.5 w-2.5 rounded-full ${colors[index]}`} />{label}</span>)}</div></section></>}
  </div>;
}

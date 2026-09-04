import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { engagementApi } from '../../../lib/api/engagement';
import type { MemberEngagementDashboard } from '../../../lib/api/types';

interface Props { onNavigate: (section: 'messages' | 'services' | 'notifications' | 'opportunities') => void; }

export default function MemberDashboardPanel({ onNavigate }: Props) {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [data, setData] = useState<MemberEngagementDashboard | null>(null);
  const [error, setError] = useState('');
  useEffect(() => { engagementApi.dashboard().then((result) => result.data ? setData(result.data) : setError(result.message || 'Error')).catch((reason) => setError(reason instanceof Error ? reason.message : 'Error')); }, []);
  if (error) return <div className="rounded-2xl border border-red-link/20 bg-red-link/5 p-5 text-sm text-red-link">{error}</div>;
  if (!data) return <div className="grid gap-4 md:grid-cols-3">{[1,2,3].map((item) => <div key={item} className="h-32 animate-pulse rounded-[24px] bg-green/[.06]" />)}</div>;

  const date = (value: string) => new Intl.DateTimeFormat(fr ? 'fr-CA' : 'en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  return <div className="space-y-7">
    <section className="relative overflow-hidden rounded-[28px] bg-green-deep px-6 py-8 text-white sm:px-9 sm:py-10">
      <div className="absolute inset-0 opacity-35 [background-image:linear-gradient(rgba(255,255,255,.04)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.04)_1px,transparent_1px)] [background-size:48px_48px]" />
      <div className="absolute -right-20 -top-24 h-64 w-64 rounded-full border-[42px] border-gold/[.1]" />
      <div className="relative grid gap-7 xl:grid-cols-[1fr_auto] xl:items-end"><div><p className="text-[9px] font-bold uppercase tracking-[.2em] text-gold">{fr ? 'Aujourd’hui dans votre communauté' : 'Today in your community'}</p><h2 className="mt-3 max-w-2xl font-display text-3xl font-bold leading-tight sm:text-4xl">{fr ? `Bonjour ${data.memberName.split(' ')[0]}, votre espace est prêt.` : `Hello ${data.memberName.split(' ')[0]}, your space is ready.`}</h2><p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{fr ? 'Vos prochains rendez-vous, vos échanges et les occasions qui comptent — réunis au même endroit.' : 'Your next events, conversations and meaningful opportunities — all in one place.'}</p></div><span className="w-fit rounded-full border border-white/15 bg-white/[.07] px-4 py-2 text-[10px] font-bold uppercase tracking-[.14em] text-gold"><i className="ri-vip-crown-2-line mr-2" />{data.membershipStatus}</span></div>
    </section>

    <section className="grid gap-3 sm:grid-cols-3">{[
      ['ri-notification-3-line', data.unreadNotifications, fr ? 'notifications non lues' : 'unread notifications', 'notifications' as const],
      ['ri-message-3-line', data.unreadMessages, fr ? 'messages à lire' : 'messages to read', 'messages' as const],
      ['ri-customer-service-2-line', data.openServiceCases, fr ? 'demandes en cours' : 'open requests', 'services' as const],
    ].map(([icon,value,label,target]) => <button key={target} onClick={() => onNavigate(target)} className="group flex items-center gap-4 rounded-2xl border border-line bg-surface p-5 text-left shadow-[0_10px_28px_rgba(0,59,27,.045)] transition hover:-translate-y-0.5 hover:border-green/25"><span className="flex h-11 w-11 items-center justify-center rounded-xl bg-green/[.08] text-xl text-green group-hover:bg-green group-hover:text-white"><i className={String(icon)} /></span><span><strong className="block font-display text-3xl text-green-deep">{value}</strong><small className="text-xs text-ink-variant">{label}</small></span></button>)}</section>

    <div className="grid gap-6 xl:grid-cols-[1.12fr_.88fr]">
      <section className="overflow-hidden rounded-[26px] border border-line bg-surface"><header className="flex items-end justify-between border-b border-line bg-green/[.035] px-6 py-5"><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Mon agenda' : 'My agenda'}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Prochains rendez-vous' : 'Upcoming events'}</h3></div><Link to="/actualites/evenements" className="text-[10px] font-bold uppercase tracking-[.12em] text-green">{fr ? 'Tout voir' : 'View all'} <i className="ri-arrow-right-line" /></Link></header><div className="divide-y divide-line">{data.upcomingEvents.length ? data.upcomingEvents.map((item) => <Link key={item.id} to={`/actualites/evenements/${item.id}`} className="group grid gap-3 p-5 transition hover:bg-green/[.025] sm:grid-cols-[120px_1fr_auto] sm:items-center"><span className="text-xs font-semibold text-red-link">{date(item.date)}</span><span><strong className="block font-display text-lg text-green-deep">{fr ? item.title : item.titleEn || item.title}</strong><small className="text-ink-variant">{item.location || (fr ? 'Lieu à confirmer' : 'Location TBC')}</small></span><span className="rounded-full bg-green/[.08] px-3 py-1 text-[9px] font-bold uppercase text-green">{item.registrationStatus}</span></Link>) : <p className="p-7 text-sm text-ink-variant">{fr ? 'Aucun événement inscrit pour le moment.' : 'No registered events yet.'}</p>}</div></section>

      <section className="overflow-hidden rounded-[26px] border border-line bg-surface"><header className="border-b border-line px-6 py-5"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'À découvrir' : 'Discover'}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Occasions recommandées' : 'Recommended opportunities'}</h3></header><div className="space-y-3 p-4">{data.opportunities.length ? data.opportunities.map((item) => <button key={item.id} onClick={() => onNavigate('opportunities')} className="flex w-full items-start gap-4 rounded-2xl border border-transparent bg-canvas/55 p-4 text-left transition hover:border-green/20"><span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gold/20 text-green"><i className="ri-briefcase-4-line" /></span><span><strong className="block text-sm text-green-deep">{fr ? item.title : item.titleEn || item.title}</strong><small className="mt-1 block text-ink-variant">{item.organization} · {item.isRemote ? (fr ? 'À distance' : 'Remote') : item.location}</small></span></button>) : <p className="p-3 text-sm text-ink-variant">{fr ? 'Aucune nouvelle occasion actuellement.' : 'No new opportunities right now.'}</p>}</div></section>
    </div>

    {data.savedItems.length > 0 && <section><div className="mb-4 flex items-center gap-3"><h3 className="font-display text-xl font-bold text-green-deep">{fr ? 'À retrouver plus tard' : 'Saved for later'}</h3><span className="h-px flex-1 bg-line" /></div><div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{data.savedItems.slice(0,6).map((item) => <article key={item.id} className="rounded-2xl border border-line bg-surface p-5"><span className="text-[9px] font-bold uppercase tracking-[.14em] text-red-link">{item.entityType}</span><h4 className="mt-2 font-display text-lg font-bold text-green-deep">{fr ? item.title : item.titleEn || item.title}</h4><p className="mt-2 text-xs text-ink-variant">{item.subtitle}</p></article>)}</div></section>}
  </div>;
}

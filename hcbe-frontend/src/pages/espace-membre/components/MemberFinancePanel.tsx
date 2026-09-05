import { useEffect, useState } from 'react';
import QRCode from 'qrcode';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { financeApi } from '../../../lib/api/finance';
import type { MemberDto, MemberFinanceSummary } from '../../../lib/api/types';

const money = (cents: number, currency: string, locale: string) => new Intl.NumberFormat(locale, { style: 'currency', currency: currency.toUpperCase() }).format(cents / 100);
const date = (value: string | undefined, locale: string) => value ? new Intl.DateTimeFormat(locale, { dateStyle: 'long' }).format(new Date(value)) : '—';

export default function MemberFinancePanel({ member }: { member: MemberDto }) {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [summary, setSummary] = useState<MemberFinanceSummary | null>(null);
  const [qr, setQr] = useState('');
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');

  const load = () => financeApi.getMemberSummary().then((response) => response.data && setSummary(response.data)).catch((reason) => setError(reason instanceof Error ? reason.message : 'Error'));
  useEffect(() => { void load(); }, []);
  useEffect(() => {
    if (!summary?.membership.verificationUrl) { setQr(''); return; }
    QRCode.toDataURL(summary.membership.verificationUrl, { width: 220, margin: 1, color: { dark: '#0b351d', light: '#ffffff' } }).then(setQr).catch(() => setQr(''));
  }, [summary?.membership.verificationUrl]);

  const renew = async () => {
    setBusy('renew'); setError('');
    try {
      const response = await financeApi.renewCommunityMembership();
      if (!response.data) throw new Error(response.message || (fr ? 'Renouvellement indisponible' : 'Renewal unavailable'));
      await load();
    } catch (reason) { setError(reason instanceof Error ? reason.message : (fr ? 'Renouvellement indisponible' : 'Renewal unavailable')); }
    finally { setBusy(''); }
  };
  const portal = async () => {
    setBusy('portal'); setError('');
    try {
      const response = await financeApi.createBillingPortal();
      if (!response.data?.url) throw new Error(response.message || 'Portal unavailable');
      window.location.assign(response.data.url);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Portal unavailable'); setBusy(''); }
  };

  if (!summary) return <div className="h-72 animate-pulse rounded-[28px] bg-surface" />;
  const standing = summary.membership;
  const valid = standing.status === 'Active' || standing.status === 'GracePeriod';
  const daysUntilExpiry = standing.currentPeriodEndUtc ? Math.ceil((new Date(standing.currentPeriodEndUtc).getTime() - Date.now()) / 86_400_000) : 0;
  const canRenew = standing.status !== 'Inactive' && daysUntilExpiry <= 30;
  const statusLabel = fr
    ? ({ Active: 'Membre en règle', GracePeriod: 'Période de grâce', Expired: 'À renouveler', Inactive: 'Adhésion inactive' } as Record<string, string>)[standing.status]
    : ({ Active: 'Member in good standing', GracePeriod: 'Grace period', Expired: 'Renewal required', Inactive: 'Inactive membership' } as Record<string, string>)[standing.status];

  return <div className="space-y-7" data-testid="member-finance-panel">
    <section className="relative overflow-hidden rounded-[30px] bg-green-deep text-white shadow-[0_24px_70px_rgba(0,45,22,.18)]">
      <div className="absolute inset-0 opacity-40 [background-image:linear-gradient(rgba(255,255,255,.04)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.04)_1px,transparent_1px)] [background-size:44px_44px]" />
      <div className="absolute -right-20 -top-24 h-72 w-72 rounded-full border-[50px] border-gold/10" />
      <div className="relative grid gap-8 p-6 sm:p-9 lg:grid-cols-[1fr_260px] lg:items-center">
        <div><p className="text-[9px] font-bold uppercase tracking-[.22em] text-gold">{fr ? 'Adhésion HCBE Canada' : 'HCBE Canada membership'}</p><h2 className="mt-3 max-w-2xl font-display text-4xl font-bold leading-[1.05] text-white sm:text-5xl">{statusLabel}</h2><p className="mt-4 max-w-xl text-sm leading-6 text-green-dim">{valid ? (fr ? 'Votre engagement soutient les services et les liens qui font vivre notre communauté.' : 'Your commitment supports the services and connections that sustain our community.') : (fr ? 'Choisissez une formule pour activer ou renouveler votre statut de membre.' : 'Choose a plan to activate or renew your membership status.')}</p>
          <div className="mt-7 flex flex-wrap gap-5 border-t border-white/10 pt-5"><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Formule' : 'Plan'}</span><strong className="mt-1 block text-sm">{fr ? standing.plan?.name || 'Membre communautaire — Gratuit' : standing.plan?.nameEn || 'Community member — Free'}</strong></div><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Valide jusqu’au' : 'Valid until'}</span><strong className="mt-1 block text-sm">{date(standing.currentPeriodEndUtc, locale)}</strong></div><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Cotisation' : 'Membership fee'}</span><strong className="mt-1 block text-sm">{fr ? 'Gratuite' : 'Free'}</strong></div></div>
          {standing.hasBillingAccount && <button onClick={portal} disabled={busy === 'portal'} className="mt-6 rounded-xl border border-white/20 px-4 py-3 text-[10px] font-bold uppercase tracking-[.12em] transition hover:border-gold hover:text-gold">{fr ? 'Gérer la facturation' : 'Manage billing'} ↗</button>}
        </div>
        <div className="rounded-[24px] border border-white/15 bg-white/[.07] p-4 backdrop-blur-sm">{valid && qr ? <><div className="mx-auto w-fit rounded-2xl bg-white p-3"><img src={qr} alt={fr ? 'Code QR de vérification' : 'Verification QR code'} className="h-40 w-40" /></div><p className="mt-3 text-center text-[9px] font-bold uppercase tracking-[.15em] text-green-dim">{fr ? 'Carte de membre vérifiable' : 'Verifiable member card'}</p></> : <div className="flex min-h-48 flex-col items-center justify-center text-center"><i className="ri-qr-code-line text-5xl text-gold" /><p className="mt-3 text-xs text-green-dim">{fr ? 'La carte numérique sera disponible après l’activation.' : 'Your digital card will appear after activation.'}</p></div>}</div>
      </div>
    </section>

    {error && <p className="rounded-2xl border border-red-link/20 bg-red-link/5 p-4 text-sm text-red-link">{error}</p>}

    <section className="grid gap-5 lg:grid-cols-[1.35fr_.65fr]">
      <article className="relative overflow-hidden rounded-[28px] border border-green/15 bg-surface p-6 shadow-[0_18px_55px_rgba(0,59,27,.07)] sm:p-8">
        <div className="absolute -right-12 -top-16 h-44 w-44 rounded-full border-[30px] border-gold/10" />
        <div className="relative"><div className="flex flex-wrap items-center gap-3"><span className="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-green text-xl text-gold"><i className="ri-community-line" /></span><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{fr ? 'Votre formule' : 'Your plan'}</p><h3 className="font-display text-2xl font-bold text-green-deep">{fr ? 'Membre communautaire — Gratuit' : 'Community member — Free'}</h3></div></div>
          <p className="mt-5 max-w-2xl text-sm leading-6 text-ink-variant">{fr ? 'Votre compte vous donne accès à la communauté, aux ressources, aux événements et aux services du HCBE Canada. Aucun paiement n’est requis.' : 'Your account gives you access to the HCBE Canada community, resources, events and services. No payment is required.'}</p>
          <ul className="mt-5 grid gap-3 text-sm text-ink sm:grid-cols-2">{(fr ? ['Carte de membre numérique', 'Services communautaires', 'Événements et occasions', 'Renouvellement annuel gratuit'] : ['Digital membership card', 'Community services', 'Events and opportunities', 'Free annual renewal']).map((benefit) => <li key={benefit} className="flex items-center gap-2"><i className="ri-check-line text-green" />{benefit}</li>)}</ul>
          <div className="mt-7 flex flex-wrap items-center gap-3 border-t border-line pt-5"><button type="button" onClick={() => void renew()} disabled={!canRenew || Boolean(busy)} className="inline-flex min-h-11 items-center gap-2 rounded-xl bg-green px-5 py-3 text-[10px] font-bold uppercase tracking-[.13em] text-white transition hover:bg-green-deep disabled:cursor-not-allowed disabled:opacity-50"><i className={busy === 'renew' ? 'ri-loader-4-line animate-spin' : 'ri-refresh-line'} />{busy === 'renew' ? (fr ? 'Renouvellement…' : 'Renewing…') : canRenew ? (fr ? 'Renouveler gratuitement' : 'Renew for free') : (fr ? 'Déjà renouvelée' : 'Already renewed')}</button>{!canRenew && <span className="text-xs text-ink-variant">{fr ? 'Le renouvellement ouvre 30 jours avant l’échéance.' : 'Renewal opens 30 days before expiry.'}</span>}</div>
        </div>
      </article>
      <article className="flex flex-col justify-between rounded-[28px] bg-gold p-6 text-green-deep sm:p-8"><div><span className="inline-flex h-11 w-11 items-center justify-center rounded-full bg-green-deep text-xl text-gold"><i className="ri-heart-3-line" /></span><p className="mt-6 text-[9px] font-bold uppercase tracking-[.18em]">{fr ? 'Soutenir la mission' : 'Support the mission'}</p><h3 className="mt-2 font-display text-3xl font-bold leading-tight">{fr ? 'Votre contribution reste facultative.' : 'Your contribution remains optional.'}</h3><p className="mt-3 text-sm leading-6 opacity-75">{fr ? 'Les dons financent les initiatives communautaires sans conditionner votre statut de membre.' : 'Donations fund community initiatives without affecting your membership status.'}</p></div><Link to="/contribuer" className="mt-7 inline-flex min-h-11 items-center justify-between rounded-xl bg-green-deep px-5 py-3 text-[10px] font-bold uppercase tracking-[.13em] text-white">{fr ? 'Faire une contribution' : 'Make a contribution'}<i className="ri-arrow-right-up-line text-lg" /></Link></article>
    </section>

    <section className="overflow-hidden rounded-[26px] border border-line bg-surface"><header className="flex items-center justify-between border-b border-line bg-green/[.035] p-6"><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Historique' : 'History'}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Paiements et reçus' : 'Payments and receipts'}</h3></div><span className="rounded-full bg-green/10 px-3 py-1 text-xs font-bold text-green">{summary.transactions.length}</span></header>
      {summary.transactions.length === 0 ? <p className="p-7 text-sm text-ink-variant">{fr ? 'Aucune transaction pour le moment.' : 'No transactions yet.'}</p> : <div className="divide-y divide-line">{summary.transactions.map((item) => <div key={item.id} className="grid gap-3 p-5 sm:grid-cols-[1fr_auto_auto] sm:items-center"><div><strong className="text-sm text-green-deep">{item.kind === 'Membership' ? (fr ? 'Adhésion' : 'Membership') : (item.campaignTitle || (fr ? 'Contribution' : 'Contribution'))}</strong><p className="mt-1 text-xs text-ink-variant">{new Date(item.createdAtUtc).toLocaleDateString(locale)} · {item.receiptNumber}</p></div><span className="text-sm font-bold text-green-deep">{money(item.amountCents - item.refundedAmountCents, item.currency, locale)}</span>{item.receiptUrl ? <a href={item.receiptUrl} download className="inline-flex items-center gap-2 text-[10px] font-bold uppercase tracking-[.12em] text-red-link hover:text-green"><i className="ri-file-pdf-2-line" />{fr ? 'Reçu PDF' : 'PDF receipt'}</a> : <span className="text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">{item.status}</span>}</div>)}</div>}
    </section>
  </div>;
}

import { useEffect, useState } from 'react';
import QRCode from 'qrcode';
import { useTranslation } from 'react-i18next';
import { financeApi } from '../../../lib/api/finance';
import type { MemberDto, MemberFinanceSummary, MembershipPlan } from '../../../lib/api/types';

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

  const checkout = async (plan: MembershipPlan) => {
    setBusy(plan.id); setError('');
    try {
      const response = await financeApi.createMembershipCheckout(plan.id);
      if (!response.data?.checkoutUrl) throw new Error(response.message || 'Checkout unavailable');
      window.location.assign(response.data.checkoutUrl);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Checkout unavailable'); setBusy(''); }
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
  const statusLabel = fr
    ? ({ Active: 'Membre en règle', GracePeriod: 'Période de grâce', Expired: 'À renouveler', Inactive: 'Adhésion inactive' } as Record<string, string>)[standing.status]
    : ({ Active: 'Member in good standing', GracePeriod: 'Grace period', Expired: 'Renewal required', Inactive: 'Inactive membership' } as Record<string, string>)[standing.status];

  return <div className="space-y-7" data-testid="member-finance-panel">
    <section className="relative overflow-hidden rounded-[30px] bg-green-deep text-white shadow-[0_24px_70px_rgba(0,45,22,.18)]">
      <div className="absolute inset-0 opacity-40 [background-image:linear-gradient(rgba(255,255,255,.04)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.04)_1px,transparent_1px)] [background-size:44px_44px]" />
      <div className="absolute -right-20 -top-24 h-72 w-72 rounded-full border-[50px] border-gold/10" />
      <div className="relative grid gap-8 p-6 sm:p-9 lg:grid-cols-[1fr_260px] lg:items-center">
        <div><p className="text-[9px] font-bold uppercase tracking-[.22em] text-gold">{fr ? 'Adhésion HCBE Canada' : 'HCBE Canada membership'}</p><h2 className="mt-3 max-w-2xl font-display text-4xl font-bold leading-[1.05] text-white sm:text-5xl">{statusLabel}</h2><p className="mt-4 max-w-xl text-sm leading-6 text-green-dim">{valid ? (fr ? 'Votre engagement soutient les services et les liens qui font vivre notre communauté.' : 'Your commitment supports the services and connections that sustain our community.') : (fr ? 'Choisissez une formule pour activer ou renouveler votre statut de membre.' : 'Choose a plan to activate or renew your membership status.')}</p>
          <div className="mt-7 flex flex-wrap gap-5 border-t border-white/10 pt-5"><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Formule' : 'Plan'}</span><strong className="mt-1 block text-sm">{standing.plan?.name || '—'}</strong></div><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Valide jusqu’au' : 'Valid until'}</span><strong className="mt-1 block text-sm">{date(standing.currentPeriodEndUtc, locale)}</strong></div><div><span className="block text-[9px] uppercase tracking-[.14em] text-green-dim">{fr ? 'Renouvellement' : 'Renewal'}</span><strong className="mt-1 block text-sm">{standing.autoRenew ? (fr ? 'Automatique' : 'Automatic') : (fr ? 'Manuel' : 'Manual')}</strong></div></div>
          {standing.hasBillingAccount && <button onClick={portal} disabled={busy === 'portal'} className="mt-6 rounded-xl border border-white/20 px-4 py-3 text-[10px] font-bold uppercase tracking-[.12em] transition hover:border-gold hover:text-gold">{fr ? 'Gérer la facturation' : 'Manage billing'} ↗</button>}
        </div>
        <div className="rounded-[24px] border border-white/15 bg-white/[.07] p-4 backdrop-blur-sm">{valid && qr ? <><div className="mx-auto w-fit rounded-2xl bg-white p-3"><img src={qr} alt={fr ? 'Code QR de vérification' : 'Verification QR code'} className="h-40 w-40" /></div><p className="mt-3 text-center text-[9px] font-bold uppercase tracking-[.15em] text-green-dim">{fr ? 'Carte de membre vérifiable' : 'Verifiable member card'}</p></> : <div className="flex min-h-48 flex-col items-center justify-center text-center"><i className="ri-qr-code-line text-5xl text-gold" /><p className="mt-3 text-xs text-green-dim">{fr ? 'La carte numérique sera disponible après l’activation.' : 'Your digital card will appear after activation.'}</p></div>}</div>
      </div>
    </section>

    {error && <p className="rounded-2xl border border-red-link/20 bg-red-link/5 p-4 text-sm text-red-link">{error}</p>}

    <section><div className="flex items-end justify-between gap-4"><div><p className="text-[9px] font-bold uppercase tracking-[.18em] text-red-link">{fr ? 'Formules' : 'Plans'}</p><h3 className="mt-1 font-display text-3xl font-bold text-green-deep">{fr ? 'Renouveler mon engagement' : 'Renew my commitment'}</h3></div></div>
      {summary.plans.length === 0 ? <div className="mt-5 rounded-2xl border border-dashed border-line bg-surface p-8 text-sm text-ink-variant">{fr ? 'Les formules d’adhésion seront publiées prochainement.' : 'Membership plans will be published soon.'}</div> : <div className="mt-5 grid gap-4 lg:grid-cols-2 xl:grid-cols-3">{summary.plans.map((plan, index) => <article key={plan.id} className={`relative overflow-hidden rounded-[24px] border p-6 ${index === 0 ? 'border-green bg-green text-white' : 'border-line bg-surface text-ink'}`}><span className={`text-[9px] font-bold uppercase tracking-[.16em] ${index === 0 ? 'text-gold' : 'text-red-link'}`}>{plan.billingMode === 'Recurring' ? (fr ? 'Renouvellement automatique' : 'Automatic renewal') : (fr ? 'Paiement annuel' : 'Annual payment')}</span><h4 className={`mt-3 font-display text-2xl font-bold ${index === 0 ? 'text-white' : 'text-green-deep'}`}>{fr ? plan.name : plan.nameEn || plan.name}</h4><p className={`mt-2 text-sm leading-6 ${index === 0 ? 'text-white/70' : 'text-ink-variant'}`}>{fr ? plan.description : plan.descriptionEn || plan.description}</p><div className="mt-6 font-display text-4xl font-bold">{money(plan.amountCents, plan.currency, locale)}<span className="ml-1 text-xs font-normal opacity-65">/{fr ? 'an' : 'year'}</span></div>{plan.benefits.length > 0 && <ul className="mt-5 space-y-2">{plan.benefits.map((benefit) => <li key={benefit} className="flex gap-2 text-xs"><i className={`ri-check-line ${index === 0 ? 'text-gold' : 'text-green'}`} />{benefit}</li>)}</ul>}<button disabled={Boolean(busy)} onClick={() => void (standing.hasActiveSubscription ? portal() : checkout(plan))} className={`mt-6 w-full rounded-xl px-4 py-3 text-[10px] font-bold uppercase tracking-[.14em] transition disabled:cursor-not-allowed disabled:opacity-55 ${index === 0 ? 'bg-gold text-green-deep hover:bg-white' : 'bg-green text-white hover:bg-green-deep'}`}>{standing.hasActiveSubscription ? (fr ? 'Gérer dans la facturation' : 'Manage in billing') : busy === plan.id ? (fr ? 'Redirection…' : 'Redirecting…') : (fr ? 'Choisir cette formule' : 'Choose this plan')}</button></article>)}</div>}
    </section>

    <section className="overflow-hidden rounded-[26px] border border-line bg-surface"><header className="flex items-center justify-between border-b border-line bg-green/[.035] p-6"><div><p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Historique' : 'History'}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{fr ? 'Paiements et reçus' : 'Payments and receipts'}</h3></div><span className="rounded-full bg-green/10 px-3 py-1 text-xs font-bold text-green">{summary.transactions.length}</span></header>
      {summary.transactions.length === 0 ? <p className="p-7 text-sm text-ink-variant">{fr ? 'Aucune transaction pour le moment.' : 'No transactions yet.'}</p> : <div className="divide-y divide-line">{summary.transactions.map((item) => <div key={item.id} className="grid gap-3 p-5 sm:grid-cols-[1fr_auto_auto] sm:items-center"><div><strong className="text-sm text-green-deep">{item.kind === 'Membership' ? (fr ? 'Adhésion' : 'Membership') : (item.campaignTitle || (fr ? 'Contribution' : 'Contribution'))}</strong><p className="mt-1 text-xs text-ink-variant">{new Date(item.createdAtUtc).toLocaleDateString(locale)} · {item.receiptNumber}</p></div><span className="text-sm font-bold text-green-deep">{money(item.amountCents - item.refundedAmountCents, item.currency, locale)}</span>{item.receiptUrl ? <a href={item.receiptUrl} download className="inline-flex items-center gap-2 text-[10px] font-bold uppercase tracking-[.12em] text-red-link hover:text-green"><i className="ri-file-pdf-2-line" />{fr ? 'Reçu PDF' : 'PDF receipt'}</a> : <span className="text-[10px] font-bold uppercase tracking-[.12em] text-ink-variant">{item.status}</span>}</div>)}</div>}
    </section>
  </div>;
}

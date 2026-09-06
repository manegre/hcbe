import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { eventCommerceApi } from '../../lib/api/event-commerce';
import type { Event, EventTicketTier } from '../../lib/api/types';
import { localized, localizedOptional } from '../../lib/i18n/localized';

const money = (cents: number, currency: string, locale: string) =>
  new Intl.NumberFormat(locale, { style: 'currency', currency: currency.toUpperCase() }).format(cents / 100);

export function EventTicketPurchasePanel({ event }: { event: Event }) {
  const { i18n } = useTranslation();
  const { user } = useAuth();
  const fr = i18n.language.startsWith('fr');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [tiers, setTiers] = useState<EventTicketTier[]>([]);
  const [quantities, setQuantities] = useState<Record<string, number>>({});
  const [buyerName, setBuyerName] = useState([user?.firstName, user?.lastName].filter(Boolean).join(' '));
  const [buyerEmail, setBuyerEmail] = useState(user?.email ?? '');
  const [promoCode, setPromoCode] = useState('');
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    eventCommerceApi.getTiers(event.id).then((response) => {
      if (response.success && response.data) setTiers(response.data);
      else setError(response.message || (fr ? 'Billetterie indisponible.' : 'Ticketing unavailable.'));
    }).catch((reason) => setError(reason instanceof Error ? reason.message : String(reason))).finally(() => setLoading(false));
  }, [event.id, fr]);

  const total = useMemo(() => tiers.reduce((sum, tier) => sum + tier.priceCents * (quantities[tier.id] ?? 0), 0), [tiers, quantities]);
  const quantity = Object.values(quantities).reduce((sum, value) => sum + value, 0);

  const checkout = async () => {
    if (!buyerName.trim() || !buyerEmail.trim() || quantity < 1) {
      setError(fr ? 'Choisissez des billets et indiquez vos coordonnées.' : 'Choose tickets and enter your contact details.');
      return;
    }
    setSubmitting(true); setError('');
    try {
      const response = await eventCommerceApi.checkout(event.id, {
        buyerName, buyerEmail, promoCode: promoCode || undefined,
        items: Object.entries(quantities).filter(([, value]) => value > 0).map(([tierId, value]) => ({ tierId, quantity: value })),
      });
      if (!response.success || !response.data) throw new Error(response.message || 'Checkout failed');
      if (response.data.checkoutUrl) window.location.assign(response.data.checkoutUrl);
      else window.location.assign(`/billets/commande/${response.data.accessToken}`);
    } catch (reason) { setError(reason instanceof Error ? reason.message : String(reason)); }
    finally { setSubmitting(false); }
  };

  if (loading) return <div className="animate-pulse border border-white/15 bg-white/[.06] p-6 text-sm text-white/70">{fr ? 'Chargement de la billetterie…' : 'Loading tickets…'}</div>;

  return (
    <section className="overflow-hidden border border-white/15 bg-white/[.075] text-white shadow-[0_24px_55px_rgba(0,20,9,.18)]" aria-labelledby="ticketing-title">
      <header className="border-b border-white/15 px-6 py-6 sm:px-8">
        <p className="text-[10px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Billetterie sécurisée' : 'Secure ticketing'}</p>
        <h2 id="ticketing-title" className="mt-2 font-display text-[28px] font-bold">{fr ? 'Choisissez vos billets' : 'Choose your tickets'}</h2>
        <p className="mt-2 text-sm text-white/60">{fr ? 'Paiement traité de façon sécurisée par Stripe.' : 'Secure payment processing by Stripe.'}</p>
      </header>
      <div className="divide-y divide-white/10">
        {tiers.length ? tiers.map((tier) => {
          const value = quantities[tier.id] ?? 0;
          return <div key={tier.id} className="grid grid-cols-[1fr_auto] gap-4 px-6 py-5 sm:px-8">
            <div>
              <div className="flex flex-wrap items-center gap-2"><h3 className="font-semibold">{localized(tier.name, tier.nameEn, i18n.language)}</h3><span className="rounded-full bg-gold px-2 py-0.5 text-[9px] font-bold uppercase text-green-deep">{tier.priceCents ? money(tier.priceCents, tier.currency, locale) : (fr ? 'Gratuit' : 'Free')}</span></div>
              {localizedOptional(tier.description, tier.descriptionEn, i18n.language) && <p className="mt-1 text-xs leading-5 text-white/60">{localizedOptional(tier.description, tier.descriptionEn, i18n.language)}</p>}
              <p className="mt-2 text-[10px] font-bold uppercase tracking-[.12em] text-white/45">{tier.availableQuantity} {fr ? 'disponibles' : 'available'}</p>
            </div>
            <div className="flex items-center gap-2" aria-label={fr ? `Quantité ${tier.name}` : `${tier.name} quantity`}>
              <button type="button" className="h-10 w-10 border border-white/20 text-xl hover:bg-white/10 disabled:opacity-30" disabled={!value} onClick={() => setQuantities((current) => ({ ...current, [tier.id]: Math.max(0, value - 1) }))}>−</button>
              <output className="w-7 text-center font-bold">{value}</output>
              <button type="button" className="h-10 w-10 border border-white/20 text-xl hover:bg-white/10 disabled:opacity-30" disabled={value >= Math.min(tier.maxPerOrder, tier.availableQuantity)} onClick={() => setQuantities((current) => ({ ...current, [tier.id]: value + 1 }))}>+</button>
            </div>
          </div>;
        }) : <p className="px-6 py-6 text-sm text-white/65">{fr ? 'Aucun billet en vente pour le moment.' : 'No tickets are currently on sale.'}</p>}
      </div>
      {tiers.length > 0 && <div className="space-y-4 border-t border-white/15 bg-black/10 px-6 py-6 sm:px-8">
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="text-[10px] font-bold uppercase tracking-[.13em] text-white/65">{fr ? 'Nom complet' : 'Full name'}<input value={buyerName} onChange={(e) => setBuyerName(e.target.value)} className="mt-2 h-12 w-full border border-white/20 bg-white px-3 text-sm text-green-deep outline-none focus:border-gold" /></label>
          <label className="text-[10px] font-bold uppercase tracking-[.13em] text-white/65">{fr ? 'Courriel' : 'Email'}<input type="email" value={buyerEmail} onChange={(e) => setBuyerEmail(e.target.value)} className="mt-2 h-12 w-full border border-white/20 bg-white px-3 text-sm text-green-deep outline-none focus:border-gold" /></label>
        </div>
        <label className="block text-[10px] font-bold uppercase tracking-[.13em] text-white/65">{fr ? 'Code promotionnel (facultatif)' : 'Promo code (optional)'}<input value={promoCode} onChange={(e) => setPromoCode(e.target.value.toUpperCase())} className="mt-2 h-12 w-full border border-white/20 bg-white px-3 text-sm font-semibold uppercase tracking-wider text-green-deep outline-none focus:border-gold" /></label>
        {error && <p role="alert" className="border-l-2 border-red bg-red/10 px-3 py-2 text-sm text-white">{error}</p>}
        <div className="flex items-center justify-between gap-4 border-t border-white/15 pt-5"><span><small className="block text-[9px] font-bold uppercase tracking-[.14em] text-white/45">{fr ? 'Total estimé' : 'Estimated total'}</small><strong className="mt-1 block font-display text-2xl">{money(total, tiers[0]?.currency ?? 'cad', locale)}</strong></span><button type="button" disabled={submitting || !quantity} onClick={checkout} className="min-h-12 bg-gold px-6 text-[10px] font-bold uppercase tracking-[.13em] text-green-deep transition hover:bg-white disabled:cursor-not-allowed disabled:opacity-45">{submitting ? (fr ? 'Redirection…' : 'Redirecting…') : (fr ? 'Continuer au paiement' : 'Continue to payment')} <i className="ri-arrow-right-line ml-2" /></button></div>
      </div>}
    </section>
  );
}

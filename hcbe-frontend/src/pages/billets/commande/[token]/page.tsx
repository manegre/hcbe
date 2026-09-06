import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import QRCode from 'qrcode';
import Navbar from '../../../../components/feature/Navbar';
import Footer from '../../../../components/feature/Footer';
import { eventCommerceApi } from '../../../../lib/api/event-commerce';
import type { EventTicketOrder } from '../../../../lib/api/types';
import { localized } from '../../../../lib/i18n/localized';

const money = (cents: number, currency: string, locale: string) => new Intl.NumberFormat(locale, { style: 'currency', currency: currency.toUpperCase() }).format(cents / 100);

export default function TicketOrderPage() {
  const { token = '' } = useParams();
  const { i18n } = useTranslation();
  const fr = i18n.language.startsWith('fr');
  const locale = fr ? 'fr-CA' : 'en-CA';
  const [order, setOrder] = useState<EventTicketOrder | null>(null);
  const [qrs, setQrs] = useState<Record<string, string>>({});
  const [error, setError] = useState('');
  const [transferId, setTransferId] = useState<string>();
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');

  const load = () => eventCommerceApi.getOrder(token).then((response) => {
    if (response.success && response.data) setOrder(response.data);
    else setError(response.message || (fr ? 'Commande introuvable.' : 'Order not found.'));
  }).catch((reason) => setError(reason instanceof Error ? reason.message : String(reason)));

  useEffect(() => { void load(); }, [token]);
  useEffect(() => {
    if (!order?.tickets.length) return;
    Promise.all(order.tickets.map(async (ticket) => [ticket.id, await QRCode.toDataURL(ticket.ticketCode, { width: 220, margin: 1, color: { dark: '#063b1d', light: '#ffffff' } })] as const)).then((items) => setQrs(Object.fromEntries(items))).catch(() => undefined);
  }, [order?.tickets]);

  useEffect(() => {
    if (!order || order.status !== 'Pending') return;
    const timer = window.setInterval(load, 3000);
    return () => window.clearInterval(timer);
  }, [order?.status, token]);

  const transfer = async (ticketId: string) => {
    try {
      const response = await eventCommerceApi.transfer(token, ticketId, name, email);
      if (!response.success) throw new Error(response.message);
      setTransferId(undefined); setName(''); setEmail(''); await load();
    } catch (reason) { setError(reason instanceof Error ? reason.message : String(reason)); }
  };

  return <div className="min-h-screen bg-surface"><Navbar />
    <header className="public-grid-pattern bg-green-deep py-12 text-white"><div className="container-page"><p className="text-[10px] font-bold uppercase tracking-[.2em] text-gold">HCBE Canada · {fr ? 'Billetterie' : 'Ticketing'}</p><h1 className="mt-3 max-w-4xl font-display text-4xl font-bold sm:text-5xl">{order ? localized(order.eventTitle, order.eventTitleEn, i18n.language) : (fr ? 'Votre commande' : 'Your order')}</h1>{order && <p className="mt-4 text-sm text-white/65">{fr ? 'Commande' : 'Order'} {order.orderNumber}</p>}</div></header>
    <main className="container-page py-10 sm:py-14">
      {error && <div role="alert" className="border-l-4 border-red bg-background p-5 text-red-link">{error}</div>}
      {!order && !error && <div className="h-56 animate-pulse bg-background" />}
      {order && <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <section>
          {order.status === 'Pending' && <div className="border border-gold/40 bg-gold/10 p-6"><h2 className="font-display text-2xl font-bold text-green">{fr ? 'Confirmation du paiement…' : 'Confirming payment…'}</h2><p className="mt-2 text-sm text-ink-variant">{fr ? 'Cette page se mettra à jour automatiquement.' : 'This page will update automatically.'}</p></div>}
          {order.status === 'Paid' && <div className="border-l-4 border-green bg-background p-6"><p className="text-[10px] font-bold uppercase tracking-[.16em] text-green">{fr ? 'Paiement confirmé' : 'Payment confirmed'}</p><h2 className="mt-2 font-display text-3xl font-bold text-green-deep">{fr ? 'Vos billets sont prêts.' : 'Your tickets are ready.'}</h2></div>}
          <div className="mt-6 grid gap-5 sm:grid-cols-2">
            {order.tickets.map((ticket) => <article key={ticket.id} className="relative overflow-hidden border border-line bg-background shadow-[0_14px_34px_rgba(0,59,27,.08)]">
              <div className="bg-green-deep px-5 py-4 text-white"><p className="text-[9px] font-bold uppercase tracking-[.16em] text-gold">{fr ? 'Billet officiel' : 'Official ticket'}</p><h3 className="mt-1 font-display text-xl font-bold">{localized(ticket.tierName, ticket.tierNameEn, i18n.language)}</h3></div>
              <div className="p-5">{qrs[ticket.id] && <img src={qrs[ticket.id]} alt={fr ? 'Code QR du billet' : 'Ticket QR code'} className="mx-auto h-44 w-44" />}<p className="mt-3 text-center font-mono text-xs font-bold tracking-wider text-green">{ticket.ticketCode}</p><dl className="mt-5 space-y-3 border-t border-line pt-4 text-sm"><div><dt className="text-[9px] font-bold uppercase tracking-wider text-ink-variant">{fr ? 'Participant' : 'Attendee'}</dt><dd className="mt-1 font-semibold text-ink">{ticket.attendeeName}</dd></div><div><dt className="text-[9px] font-bold uppercase tracking-wider text-ink-variant">{fr ? 'Statut' : 'Status'}</dt><dd className="mt-1 font-semibold text-green">{ticket.status}</dd></div></dl>
                {ticket.status === 'Valid' && <button type="button" onClick={() => { setTransferId(ticket.id); setName(ticket.attendeeName); setEmail(ticket.attendeeEmail); }} className="mt-5 min-h-11 w-full border border-green text-[10px] font-bold uppercase tracking-wider text-green hover:bg-green hover:text-white">{fr ? 'Transférer ce billet' : 'Transfer this ticket'}</button>}
                {transferId === ticket.id && <div className="mt-4 space-y-3 border-t border-line pt-4"><input aria-label={fr ? 'Nom du nouveau participant' : 'New attendee name'} value={name} onChange={(e) => setName(e.target.value)} placeholder={fr ? 'Nom complet' : 'Full name'} className="h-11 w-full border border-line px-3 text-sm" /><input aria-label={fr ? 'Courriel du nouveau participant' : 'New attendee email'} type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder={fr ? 'Courriel' : 'Email'} className="h-11 w-full border border-line px-3 text-sm" /><button type="button" onClick={() => transfer(ticket.id)} className="min-h-11 w-full bg-gold text-[10px] font-bold uppercase tracking-wider text-green-deep">{fr ? 'Confirmer le transfert' : 'Confirm transfer'}</button></div>}
              </div>
            </article>)}
          </div>
        </section>
        <aside className="h-fit border border-line bg-background p-6 lg:sticky lg:top-24"><h2 className="font-display text-2xl font-bold text-green">{fr ? 'Résumé' : 'Summary'}</h2><dl className="mt-5 divide-y divide-line text-sm">{order.items.map((item) => <div key={item.id} className="flex justify-between gap-3 py-3"><dt>{item.quantity} × {localized(item.tierName, item.tierNameEn, i18n.language)}</dt><dd className="font-semibold">{money(item.lineTotalCents, order.currency, locale)}</dd></div>)}<div className="flex justify-between gap-3 py-4 font-bold"><dt>Total</dt><dd>{money(order.totalCents, order.currency, locale)}</dd></div></dl>{order.ticketPdfUrl && <a href={order.ticketPdfUrl} className="mt-5 flex min-h-12 items-center justify-center gap-2 bg-green px-4 text-[10px] font-bold uppercase tracking-wider text-white"><i className="ri-file-pdf-2-line text-lg" />{fr ? 'Télécharger les billets PDF' : 'Download PDF tickets'}</a>}<Link to={`/actualites/evenements/${order.eventId}`} className="mt-3 flex min-h-11 items-center justify-center text-[10px] font-bold uppercase tracking-wider text-green">{fr ? "Voir l'événement" : 'View event'}</Link></aside>
      </div>}
    </main><Footer /></div>;
}

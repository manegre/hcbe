import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../lib/api/events';
import { getApiBaseUrl } from '../../lib/api/base-url';
import type { EventRegistration, EventRegistrationStatus } from '../../lib/api/types';
import { Button, EmptyState, inputClasses } from '../ui';

const statuses: EventRegistrationStatus[] = ['Confirmed', 'Waitlisted', 'Attended', 'NoShow', 'Cancelled'];

export const EventRegistrationsManager = ({ eventId }: { eventId: string }) => {
  const { i18n } = useTranslation();
  const fr = i18n.language.startsWith('fr');
  const [items, setItems] = useState<EventRegistration[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [message, setMessage] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await eventsApi.getRegistrationsForAdmin(eventId, status || undefined, search || undefined);
      if (response.success && response.data) setItems(response.data);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : (fr ? 'Chargement impossible.' : 'Unable to load registrations.'));
    } finally {
      setLoading(false);
    }
  }, [eventId, fr, search, status]);

  useEffect(() => {
    const timeout = window.setTimeout(() => { void load(); }, 250);
    return () => window.clearTimeout(timeout);
  }, [load]);

  const counts = useMemo(() => ({
    active: items.filter((item) => item.status === 'Confirmed' || item.status === 'Attended').length,
    waitlisted: items.filter((item) => item.status === 'Waitlisted').length,
    attended: items.filter((item) => item.status === 'Attended').length,
  }), [items]);

  const updateStatus = async (item: EventRegistration, nextStatus: EventRegistrationStatus) => {
    setMessage('');
    try {
      const response = await eventsApi.updateRegistrationForAdmin(eventId, item.id, nextStatus, item.adminNotes);
      if (response.success && response.data) {
        setItems((current) => current.map((candidate) => candidate.id === item.id ? response.data! : candidate));
        setMessage(fr ? 'Participation mise à jour.' : 'Attendance updated.');
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : (fr ? 'Mise à jour impossible.' : 'Unable to update.'));
    }
  };

  const exportCsv = async () => {
    setMessage('');
    try {
      const token = localStorage.getItem('hcbe_token');
      const response = await fetch(`${getApiBaseUrl()}/api/events/admin/${eventId}/registrations/export`, {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (!response.ok) throw new Error(fr ? "L’export a échoué." : 'Export failed.');
      const blob = await response.blob();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `event-${eventId}-registrations.csv`;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : (fr ? "L’export a échoué." : 'Export failed.'));
    }
  };

  return (
    <section className="overflow-hidden rounded-[22px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.06)]">
      <div className="border-b border-line bg-green-deep px-5 py-6 text-white sm:px-7">
        <div className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
          <div>
            <p className="text-[9px] font-bold uppercase tracking-[.18em] text-gold">{fr ? 'Participation' : 'Attendance'}</p>
            <h2 className="mt-2 font-display text-2xl font-bold">{fr ? 'Registre des inscriptions' : 'Registration register'}</h2>
          </div>
          <div className="grid grid-cols-3 divide-x divide-white/15 border border-white/15 bg-white/[.06]">
            {[
              [counts.active, fr ? 'Inscrits' : 'Registered'],
              [counts.waitlisted, fr ? 'Attente' : 'Waiting'],
              [counts.attended, fr ? 'Présents' : 'Attended'],
            ].map(([value, label]) => <div key={label} className="min-w-20 px-3 py-2 text-center"><strong className="block font-display text-xl text-gold">{value}</strong><span className="text-[8px] font-bold uppercase tracking-[.1em] text-white/55">{label}</span></div>)}
          </div>
        </div>
      </div>

      <div className="flex flex-col gap-3 border-b border-line p-4 sm:flex-row sm:p-5">
        <div className="relative flex-1"><i className="ri-search-line absolute left-3 top-1/2 -translate-y-1/2 text-ink-variant" /><input value={search} onChange={(event) => setSearch(event.target.value)} className={`${inputClasses} pl-10`} placeholder={fr ? 'Nom, courriel ou confirmation…' : 'Name, email or confirmation…'} /></div>
        <select value={status} onChange={(event) => setStatus(event.target.value)} className={`${inputClasses} cursor-pointer sm:w-48`}><option value="">{fr ? 'Tous les statuts' : 'All statuses'}</option>{statuses.map((item) => <option key={item} value={item}>{item}</option>)}</select>
        <Button type="button" variant="secondary" onClick={exportCsv}><i className="ri-download-line" />CSV</Button>
      </div>

      {message && <p className="border-b border-line bg-gold/[.08] px-5 py-3 text-sm text-green">{message}</p>}
      {loading ? (
        <div className="flex items-center justify-center gap-3 py-12 text-sm text-ink-variant"><i className="ri-loader-4-line animate-spin text-lg text-green" />{fr ? 'Chargement…' : 'Loading…'}</div>
      ) : items.length === 0 ? (
        <div className="p-5"><EmptyState icon="ri-calendar-check-line" title={fr ? 'Aucune inscription' : 'No registrations'} description={fr ? 'Les participants apparaîtront ici dès leur inscription.' : 'Participants will appear here after registering.'} /></div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[760px] text-left">
            <thead className="bg-canvas/70 text-[9px] font-bold uppercase tracking-[.13em] text-ink-variant"><tr><th className="px-5 py-3">{fr ? 'Participant' : 'Participant'}</th><th className="px-5 py-3">{fr ? 'Confirmation' : 'Confirmation'}</th><th className="px-5 py-3">{fr ? 'Inscription' : 'Registered'}</th><th className="px-5 py-3">{fr ? 'Statut' : 'Status'}</th></tr></thead>
            <tbody className="divide-y divide-line">
              {items.map((item) => (
                <tr key={item.id} className="hover:bg-green/[.025]">
                  <td className="px-5 py-4"><strong className="block text-sm text-ink">{item.memberName}</strong><span className="mt-1 block text-xs text-ink-variant">{item.memberEmail}</span></td>
                  <td className="px-5 py-4 font-mono text-xs font-bold text-green">{item.confirmationCode}</td>
                  <td className="px-5 py-4 text-xs text-ink-variant">{new Intl.DateTimeFormat(fr ? 'fr-CA' : 'en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.registeredAt))}</td>
                  <td className="px-5 py-4"><select value={item.status} onChange={(event) => void updateStatus(item, event.target.value as EventRegistrationStatus)} className="min-h-10 rounded-control border border-outline bg-surface px-3 text-xs font-bold text-green outline-none focus:border-green">{statuses.map((candidate) => <option key={candidate} value={candidate}>{candidate}</option>)}</select></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
};

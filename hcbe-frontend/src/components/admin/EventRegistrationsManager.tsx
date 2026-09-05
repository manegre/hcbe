import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { eventsApi } from '../../lib/api/events';
import { getApiBaseUrl } from '../../lib/api/base-url';
import type { EventAttendanceStats, EventCommunication, EventRegistration, EventRegistrationStatus } from '../../lib/api/types';
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
  const [checkInCode, setCheckInCode] = useState('');
  const [stats, setStats] = useState<EventAttendanceStats | null>(null);
  const [communications, setCommunications] = useState<EventCommunication[]>([]);
  const [audience, setAudience] = useState('Active');
  const [subject, setSubject] = useState('');
  const [body, setBody] = useState('');
  const [sending, setSending] = useState(false);
  const [scanning, setScanning] = useState(false);
  const videoRef = useRef<HTMLVideoElement>(null);

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

  const loadInsights = useCallback(async () => {
    const [statsResponse, communicationsResponse] = await Promise.all([
      eventsApi.getAttendanceStats(eventId), eventsApi.getCommunications(eventId),
    ]);
    if (statsResponse.success && statsResponse.data) setStats(statsResponse.data);
    if (communicationsResponse.success && communicationsResponse.data) setCommunications(communicationsResponse.data);
  }, [eventId]);

  useEffect(() => { void loadInsights(); }, [loadInsights]);

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

  const performCheckIn = async (code: string) => {
    if (!code.trim()) return;
    setMessage('');
    try {
      const response = await eventsApi.checkInByCode(eventId, code.trim());
      if (!response.success || !response.data) throw new Error(response.message || (fr ? 'Code invalide.' : 'Invalid code.'));
      setCheckInCode('');
      setScanning(false);
      setMessage(fr ? `${response.data.memberName} est maintenant présent(e).` : `${response.data.memberName} is now checked in.`);
      await Promise.all([load(), loadInsights()]);
    } catch (error) { setMessage(error instanceof Error ? error.message : (fr ? 'Pointage impossible.' : 'Unable to check in.')); }
  };

  const checkIn = async (event: React.FormEvent) => { event.preventDefault(); await performCheckIn(checkInCode); };

  useEffect(() => {
    if (!scanning) return;
    let stream: MediaStream | null = null;
    let timer = 0;
    let cancelled = false;
    const start = async () => {
      try {
        const Detector = (window as unknown as { BarcodeDetector?: new (options: { formats: string[] }) => { detect: (source: CanvasImageSource) => Promise<Array<{ rawValue: string }>> } }).BarcodeDetector;
        if (!Detector) throw new Error(fr ? 'Le scanner caméra n’est pas pris en charge par ce navigateur.' : 'Camera scanning is not supported by this browser.');
        stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: 'environment' } }, audio: false });
        if (!videoRef.current || cancelled) return;
        videoRef.current.srcObject = stream; await videoRef.current.play();
        const detector = new Detector({ formats: ['qr_code'] });
        timer = window.setInterval(async () => {
          if (!videoRef.current || videoRef.current.readyState < 2) return;
          const result = await detector.detect(videoRef.current);
          if (result[0]?.rawValue) void performCheckIn(result[0].rawValue);
        }, 650);
      } catch (error) { setMessage(error instanceof Error ? error.message : (fr ? 'Caméra inaccessible.' : 'Camera unavailable.')); setScanning(false); }
    };
    void start();
    return () => { cancelled = true; window.clearInterval(timer); stream?.getTracks().forEach((track) => track.stop()); };
  }, [fr, scanning]);

  const sendCommunication = async (event: React.FormEvent) => {
    event.preventDefault(); if (!subject.trim() || !body.trim()) return;
    setSending(true); setMessage('');
    try {
      const response = await eventsApi.sendCommunication(eventId, audience, subject, body);
      if (response.success && response.data) {
        setCommunications((current) => [response.data!, ...current]); setSubject(''); setBody('');
        setMessage(fr ? `Message placé en file pour ${response.data.recipientCount} destinataire(s).` : `Message queued for ${response.data.recipientCount} recipient(s).`);
      }
    } catch (error) { setMessage(error instanceof Error ? error.message : (fr ? 'Envoi impossible.' : 'Unable to send.')); }
    finally { setSending(false); }
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

      {stats && (
        <div className="grid grid-cols-2 border-b border-line bg-canvas/40 sm:grid-cols-4">
          {[
            [stats.total, fr ? 'Inscriptions' : 'Registrations'],
            [`${stats.attendanceRate}%`, fr ? 'Taux de présence' : 'Attendance rate'],
            [stats.averageRating ? `${stats.averageRating}/5` : '—', fr ? 'Satisfaction' : 'Satisfaction'],
            [stats.surveyResponses, fr ? 'Avis reçus' : 'Survey responses'],
          ].map(([value, label]) => <div key={label} className="border-b border-r border-line p-4 last:border-r-0 sm:border-b-0"><strong className="block font-display text-2xl text-green-deep">{value}</strong><span className="mt-1 block text-[9px] font-bold uppercase tracking-[.1em] text-ink-variant">{label}</span></div>)}
        </div>
      )}

      <div className="flex flex-col gap-3 border-b border-line p-4 sm:flex-row sm:p-5">
        <div className="relative flex-1"><i className="ri-search-line absolute left-3 top-1/2 -translate-y-1/2 text-ink-variant" /><input value={search} onChange={(event) => setSearch(event.target.value)} className={`${inputClasses} pl-10`} placeholder={fr ? 'Nom, courriel ou confirmation…' : 'Name, email or confirmation…'} /></div>
        <select value={status} onChange={(event) => setStatus(event.target.value)} className={`${inputClasses} cursor-pointer sm:w-48`}><option value="">{fr ? 'Tous les statuts' : 'All statuses'}</option>{statuses.map((item) => <option key={item} value={item}>{item}</option>)}</select>
        <Button type="button" variant="secondary" onClick={exportCsv}><i className="ri-download-line" />CSV</Button>
      </div>
      <form onSubmit={checkIn} className="flex flex-col gap-3 border-b border-line bg-gold/[.07] p-4 sm:flex-row sm:items-center sm:px-5">
        <div className="flex items-center gap-3 sm:min-w-56"><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green text-lg text-white"><i className="ri-qr-scan-2-line" /></span><div><strong className="block text-sm text-green-deep">{fr ? 'Pointage rapide' : 'Quick check-in'}</strong><span className="text-xs text-ink-variant">{fr ? 'Scannez ou saisissez le code.' : 'Scan or enter the code.'}</span></div></div>
        <input value={checkInCode} onChange={(event) => setCheckInCode(event.target.value.toUpperCase())} className={`${inputClasses} font-mono uppercase sm:flex-1`} placeholder={fr ? 'Code de confirmation' : 'Confirmation code'} />
        <Button type="button" variant="secondary" onClick={() => setScanning(true)}><i className="ri-camera-line" />{fr ? 'Scanner' : 'Scan'}</Button>
        <Button type="submit" variant="primary" disabled={!checkInCode.trim()}>{fr ? 'Marquer présent' : 'Check in'}</Button>
      </form>

      {scanning && (
        <div className="fixed inset-0 z-[100] flex items-end bg-black/75 p-3 sm:items-center sm:justify-center" role="dialog" aria-modal="true" aria-label={fr ? 'Scanner un billet' : 'Scan a ticket'}>
          <div className="w-full max-w-lg overflow-hidden rounded-[24px] bg-green-deep p-4 text-white shadow-2xl">
            <div className="mb-4 flex items-center justify-between"><div><strong className="font-display text-xl">{fr ? 'Scanner le billet' : 'Scan ticket'}</strong><p className="mt-1 text-xs text-white/60">{fr ? 'Cadrez le code QR du participant.' : 'Place the participant QR code in frame.'}</p></div><button type="button" onClick={() => setScanning(false)} className="flex h-10 w-10 items-center justify-center rounded-full border border-white/20" aria-label={fr ? 'Fermer' : 'Close'}><i className="ri-close-line text-xl" /></button></div>
            <div className="relative aspect-square overflow-hidden rounded-2xl bg-black"><video ref={videoRef} muted playsInline className="h-full w-full object-cover" /><div className="pointer-events-none absolute inset-[14%] rounded-2xl border-2 border-gold shadow-[0_0_0_999px_rgba(0,0,0,.32)]" /></div>
          </div>
        </div>
      )}

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

      <div className="border-t border-line p-5 sm:p-7">
        <div className="grid gap-6 lg:grid-cols-[1.1fr_.9fr]">
          <form onSubmit={sendCommunication} className="rounded-2xl border border-line bg-canvas/35 p-4 sm:p-5">
            <p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">{fr ? 'Communication ciblée' : 'Targeted communication'}</p>
            <h3 className="mt-2 font-display text-xl text-green-deep">{fr ? 'Écrire aux participants' : 'Message participants'}</h3>
            <div className="mt-4 grid gap-3 sm:grid-cols-[180px_1fr]">
              <select value={audience} onChange={(e) => setAudience(e.target.value)} className={inputClasses} aria-label={fr ? 'Audience' : 'Audience'}>
                <option value="Active">{fr ? 'Participants actifs' : 'Active participants'}</option><option value="Confirmed">{fr ? 'Confirmés' : 'Confirmed'}</option><option value="Waitlisted">{fr ? 'Liste d’attente' : 'Waitlisted'}</option><option value="Attended">{fr ? 'Présents' : 'Attended'}</option><option value="NoShow">{fr ? 'Absents' : 'No-show'}</option><option value="Cancelled">{fr ? 'Annulés' : 'Cancelled'}</option>
              </select>
              <input value={subject} onChange={(e) => setSubject(e.target.value)} maxLength={180} required className={inputClasses} placeholder={fr ? 'Objet du message' : 'Message subject'} />
            </div>
            <textarea value={body} onChange={(e) => setBody(e.target.value)} maxLength={5000} required rows={5} className={`${inputClasses} mt-3 resize-y py-3`} placeholder={fr ? 'Informations pratiques, rappel ou suivi…' : 'Practical details, reminder or follow-up…'} />
            <Button type="submit" variant="primary" disabled={sending || !subject.trim() || !body.trim()} className="mt-3">{sending ? (fr ? 'Envoi…' : 'Sending…') : (fr ? 'Envoyer à cette audience' : 'Send to this audience')}</Button>
          </form>
          <div>
            <p className="text-[9px] font-bold uppercase tracking-[.16em] text-ink-variant">{fr ? 'Historique récent' : 'Recent history'}</p>
            <div className="mt-3 space-y-2">{communications.length === 0 ? <p className="rounded-xl border border-dashed border-line p-5 text-sm text-ink-variant">{fr ? 'Aucun message envoyé.' : 'No messages sent.'}</p> : communications.slice(0, 5).map((item) => <div key={item.id} className="rounded-xl border border-line p-3"><div className="flex items-start justify-between gap-3"><strong className="text-sm text-green-deep">{item.subject}</strong><span className="whitespace-nowrap text-[9px] font-bold uppercase text-ink-variant">{item.recipientCount} {fr ? 'dest.' : 'recip.'}</span></div><p className="mt-1 text-xs text-ink-variant">{item.audience} · {new Intl.DateTimeFormat(fr ? 'fr-CA' : 'en-CA', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(item.sentAtUtc))}</p></div>)}</div>
          </div>
        </div>
      </div>
    </section>
  );
};

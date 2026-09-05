import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { eventsApi } from '../../lib/api/events';
import { buildApiUrl } from '../../lib/api/base-url';
import type { Event, EventRegistration, EventSurveyResponse } from '../../lib/api/types';
import { eventCalendarLinks } from '../../lib/events/calendar-links';
import QRCode from 'qrcode';

interface EventRegistrationPanelProps {
  event: Event;
  isPast: boolean;
  externalLabel: string;
}

const statusStyles: Record<EventRegistration['status'], string> = {
  Confirmed: 'border-gold/35 bg-gold/10 text-gold',
  Waitlisted: 'border-white/20 bg-white/10 text-white',
  Cancelled: 'border-white/15 bg-white/5 text-white/60',
  Attended: 'border-green-dim/30 bg-green-dim/10 text-green-dim',
  NoShow: 'border-white/15 bg-white/5 text-white/60',
};

const CalendarButtons = ({ links, french }: { links: ReturnType<typeof eventCalendarLinks>; french: boolean }) => (
  <div className="mt-4 grid grid-cols-3 gap-2" aria-label={french ? 'Ajouter au calendrier' : 'Add to calendar'}>
    <a href={links.google} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-10 items-center justify-center gap-1 border border-white/20 px-2 text-[9px] font-bold uppercase tracking-[.06em] text-white hover:border-gold hover:text-gold"><i className="ri-google-fill" />Google</a>
    <a href={links.outlook} target="_blank" rel="noopener noreferrer" className="inline-flex min-h-10 items-center justify-center gap-1 border border-white/20 px-2 text-[9px] font-bold uppercase tracking-[.06em] text-white hover:border-gold hover:text-gold"><i className="ri-microsoft-fill" />Outlook</a>
    <a href={links.apple} className="inline-flex min-h-10 items-center justify-center gap-1 border border-white/20 px-2 text-[9px] font-bold uppercase tracking-[.06em] text-white hover:border-gold hover:text-gold"><i className="ri-apple-fill" />Apple</a>
  </div>
);

export const EventRegistrationPanel = ({ event, isPast, externalLabel }: EventRegistrationPanelProps) => {
  const { i18n } = useTranslation();
  const { isAuthenticated, user } = useAuth();
  const isFrench = i18n.language.startsWith('fr');
  const [registration, setRegistration] = useState<EventRegistration | null>(null);
  const [loading, setLoading] = useState(event.registrationMode === 'Native' && isAuthenticated);
  const [submitting, setSubmitting] = useState(false);
  const [message, setMessage] = useState('');
  const [accessibilityNeeds, setAccessibilityNeeds] = useState('');
  const [checkInQr, setCheckInQr] = useState('');
  const [survey, setSurvey] = useState<EventSurveyResponse | null>(null);
  const [rating, setRating] = useState(5);
  const [feedback, setFeedback] = useState('');
  const [consentToQuote, setConsentToQuote] = useState(false);

  useEffect(() => {
    if (event.registrationMode !== 'Native' || !isAuthenticated || !user?.memberId) {
      setLoading(false);
      return;
    }
    let active = true;
    eventsApi.getMyRegistration(event.id)
      .then((response) => {
        if (active && response.success && response.data) setRegistration(response.data);
      })
      .catch(() => undefined)
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [event.id, event.registrationMode, isAuthenticated, user?.memberId]);

  useEffect(() => {
    if (!registration || registration.status === 'Cancelled') { setCheckInQr(''); return; }
    QRCode.toDataURL(registration.confirmationCode, { width: 180, margin: 1, color: { dark: '#0b351d', light: '#ffffff' } }).then(setCheckInQr).catch(() => setCheckInQr(''));
  }, [registration]);

  useEffect(() => {
    if (registration?.status !== 'Attended') return;
    eventsApi.getMySurvey(event.id).then((response) => {
      if (!response.success || !response.data) return;
      setSurvey(response.data); setRating(response.data.rating); setFeedback(response.data.feedback ?? ''); setConsentToQuote(response.data.consentToQuote);
    }).catch(() => undefined);
  }, [event.id, registration?.status]);

  const calendarLinks = eventCalendarLinks(event, !isFrench);

  const downloadCertificate = async () => {
    setMessage('');
    const token = localStorage.getItem('hcbe_token');
    const response = await fetch(buildApiUrl(`/api/events/${event.id}/certificate.pdf`), { headers: token ? { Authorization: `Bearer ${token}` } : {} });
    if (!response.ok) { setMessage(isFrench ? "L’attestation n’est pas encore disponible." : 'The certificate is not available yet.'); return; }
    const url = URL.createObjectURL(await response.blob());
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = `HCBE-attestation-${registration?.confirmationCode}.pdf`; anchor.click(); URL.revokeObjectURL(url);
  };

  const submitSurvey = async () => {
    setSubmitting(true); setMessage('');
    try {
      const response = await eventsApi.submitSurvey(event.id, rating, feedback || undefined, consentToQuote);
      if (response.success && response.data) { setSurvey(response.data); setMessage(isFrench ? 'Merci, votre avis a été enregistré.' : 'Thank you, your feedback was saved.'); }
    } catch (error) { setMessage(error instanceof Error ? error.message : (isFrench ? 'Envoi impossible.' : 'Unable to submit.')); }
    finally { setSubmitting(false); }
  };

  const register = async () => {
    setSubmitting(true);
    setMessage('');
    try {
      const response = await eventsApi.register(event.id, accessibilityNeeds || undefined);
      if (response.success && response.data) {
        setRegistration(response.data);
        setMessage(response.data.status === 'Waitlisted'
          ? (isFrench ? "Vous êtes maintenant sur la liste d’attente." : 'You are now on the waiting list.')
          : (isFrench ? 'Votre place est confirmée.' : 'Your place is confirmed.'));
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : (isFrench ? "L’inscription a échoué." : 'Registration failed.'));
    } finally {
      setSubmitting(false);
    }
  };

  const cancel = async () => {
    if (!window.confirm(isFrench ? 'Annuler votre inscription à cet événement ?' : 'Cancel your registration for this event?')) return;
    setSubmitting(true);
    try {
      const response = await eventsApi.cancelRegistration(event.id);
      if (response.success && response.data) {
        setRegistration(response.data);
        setMessage(isFrench ? 'Votre inscription a été annulée.' : 'Your registration was cancelled.');
      }
    } catch (error) {
      setMessage(error instanceof Error ? error.message : (isFrench ? "L’annulation a échoué." : 'Cancellation failed.'));
    } finally {
      setSubmitting(false);
    }
  };

  if (isPast && registration?.status !== 'Attended') {
    return <div className="mt-6 border-t border-white/15 pt-5"><p className="text-sm text-white/65">{isFrench ? 'Cet événement est terminé.' : 'This event has ended.'}</p><CalendarButtons links={calendarLinks} french={isFrench} /></div>;
  }

  if (event.registrationMode === 'External') {
    const href = event.registrationUrl || event.meetingLink;
    return <div className="mt-6">{href && <a href={href} target="_blank" rel="noopener noreferrer" className="group inline-flex min-h-12 w-full items-center justify-between rounded-control bg-gold px-5 py-3 text-[12px] font-bold uppercase tracking-[0.1em] text-green-deep transition-colors hover:bg-gold-dim">{externalLabel}<i className="ri-arrow-right-up-line text-lg" aria-hidden="true" /></a>}<CalendarButtons links={calendarLinks} french={isFrench} /></div>;
  }

  if (event.registrationMode === 'Disabled') {
    return <div className="mt-6 border-t border-white/15 pt-5"><p className="text-sm text-white/65">{isFrench ? "Aucune inscription n’est requise." : 'No registration is required.'}</p><CalendarButtons links={calendarLinks} french={isFrench} /></div>;
  }

  if (!isAuthenticated || !user?.memberId) {
    const returnTo = `/actualites/evenements/${event.id}`;
    return (
      <div className="mt-6">
        <Link to={`/espace-membre?returnTo=${encodeURIComponent(returnTo)}`} className="group inline-flex min-h-12 w-full items-center justify-between rounded-control bg-gold px-5 py-3 text-[12px] font-bold uppercase tracking-[0.1em] text-green-deep transition-colors hover:bg-gold-dim">
          {isFrench ? "Se connecter pour s’inscrire" : 'Sign in to register'}
          <i className="ri-login-circle-line text-lg" aria-hidden="true" />
        </Link>
        <p className="mt-3 text-xs leading-5 text-white/55">{isFrench ? "L’inscription est réservée aux membres. La création d’un compte est gratuite." : 'Registration is available to members. Creating an account is free.'}</p>
        <CalendarButtons links={calendarLinks} french={isFrench} />
      </div>
    );
  }

  if (loading) {
    return <div className="mt-6 flex items-center gap-3 text-sm text-white/65"><i className="ri-loader-4-line animate-spin" />{isFrench ? 'Vérification…' : 'Checking…'}</div>;
  }

  if (registration && registration.status !== 'Cancelled') {
    const labels: Record<EventRegistration['status'], string> = isFrench
      ? { Confirmed: 'Place confirmée', Waitlisted: "Liste d’attente", Cancelled: 'Annulée', Attended: 'Présence confirmée', NoShow: 'Absent' }
      : { Confirmed: 'Confirmed', Waitlisted: 'Waiting list', Cancelled: 'Cancelled', Attended: 'Attended', NoShow: 'No-show' };
    return (
      <div className="mt-6 border-t border-white/15 pt-5">
        <span className={`inline-flex rounded-full border px-3 py-1.5 text-[10px] font-bold uppercase tracking-[0.13em] ${statusStyles[registration.status]}`}>{labels[registration.status]}</span>
        <p className="mt-4 text-sm leading-6 text-white/75">
          {registration.status === 'Waitlisted' && registration.waitlistPosition
            ? (isFrench ? `Position ${registration.waitlistPosition} sur la liste d’attente.` : `Position ${registration.waitlistPosition} on the waiting list.`)
            : (isFrench ? `Confirmation ${registration.confirmationCode}` : `Confirmation ${registration.confirmationCode}`)}
        </p>
        {checkInQr && registration.status !== 'Waitlisted' && <div className="mt-4 flex items-center gap-4 rounded-xl border border-white/15 bg-white/[.07] p-3"><img src={checkInQr} alt={isFrench ? 'Code QR de présence' : 'Attendance QR code'} className="h-20 w-20 rounded bg-white p-1" /><p className="text-xs leading-5 text-white/65">{isFrench ? 'Présentez ce code QR à l’accueil pour confirmer votre présence.' : 'Show this QR code at check-in to confirm your attendance.'}</p></div>}
        {registration.meetingLink && (
          <a href={registration.meetingLink} target="_blank" rel="noopener noreferrer" className="mt-4 inline-flex min-h-11 w-full items-center justify-between rounded-control bg-gold px-4 text-[11px] font-bold uppercase tracking-[.1em] text-green-deep">
            {isFrench ? 'Rejoindre la rencontre' : 'Join the meeting'}<i className="ri-video-chat-line text-lg" />
          </a>
        )}
        <CalendarButtons links={calendarLinks} french={isFrench} />
        {!isPast && <div className="mt-2">
          <button type="button" onClick={cancel} disabled={submitting} className="min-h-10 border border-white/20 px-3 text-[10px] font-bold uppercase tracking-[.08em] text-white/70 hover:border-red-300 hover:text-white disabled:opacity-50">
            {isFrench ? 'Annuler' : 'Cancel'}
          </button>
        </div>}
        {registration.status === 'Attended' && (
          <div className="mt-5 border-t border-white/15 pt-5">
            <button type="button" onClick={downloadCertificate} className="inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-control border border-gold/50 px-4 text-[10px] font-bold uppercase tracking-[.09em] text-gold hover:bg-gold hover:text-green-deep"><i className="ri-award-line" />{isFrench ? 'Télécharger mon attestation PDF' : 'Download my PDF certificate'}</button>
            <div className="mt-5 rounded-xl bg-white/[.07] p-4">
              <strong className="text-sm text-white">{isFrench ? 'Votre expérience' : 'Your experience'}</strong>
              <div className="mt-3 flex gap-1" role="radiogroup" aria-label={isFrench ? 'Note' : 'Rating'}>{[1,2,3,4,5].map((value) => <button key={value} type="button" onClick={() => setRating(value)} aria-pressed={rating === value} className={`text-xl ${value <= rating ? 'text-gold' : 'text-white/25'}`}><i className="ri-star-fill" /></button>)}</div>
              <textarea value={feedback} onChange={(e) => setFeedback(e.target.value)} maxLength={2000} rows={3} placeholder={isFrench ? 'Votre commentaire (facultatif)' : 'Your feedback (optional)'} className="mt-3 w-full resize-none rounded-control border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none placeholder:text-white/40 focus:border-gold" />
              <label className="mt-3 flex gap-2 text-xs leading-5 text-white/65"><input type="checkbox" checked={consentToQuote} onChange={(e) => setConsentToQuote(e.target.checked)} className="mt-1" />{isFrench ? 'J’autorise la publication anonyme de mon commentaire.' : 'I allow my feedback to be quoted anonymously.'}</label>
              <button type="button" onClick={submitSurvey} disabled={submitting} className="mt-3 min-h-10 w-full rounded-control bg-white px-3 text-[10px] font-bold uppercase tracking-[.08em] text-green-deep disabled:opacity-50">{survey ? (isFrench ? 'Mettre à jour mon avis' : 'Update feedback') : (isFrench ? 'Envoyer mon avis' : 'Send feedback')}</button>
            </div>
          </div>
        )}
        {message && <p className="mt-3 text-xs leading-5 text-white/65">{message}</p>}
      </div>
    );
  }

  return (
    <div className="mt-6">
      {event.remainingCapacity !== undefined && (
        <p className="mb-4 text-sm text-white/70">
          <strong className="text-white">{event.remainingCapacity}</strong> {isFrench ? 'place(s) disponible(s)' : 'spot(s) remaining'}
          {event.remainingCapacity === 0 && event.allowWaitlist ? (isFrench ? " — liste d’attente ouverte" : ' — waiting list open') : ''}
        </p>
      )}
      <label className="block text-[10px] font-bold uppercase tracking-[.12em] text-white/55" htmlFor="event-accessibility">{isFrench ? "Besoins d’accessibilité (facultatif)" : 'Accessibility needs (optional)'}</label>
      <textarea id="event-accessibility" value={accessibilityNeeds} onChange={(event) => setAccessibilityNeeds(event.target.value)} maxLength={500} rows={2} className="mt-2 w-full resize-none rounded-control border border-white/20 bg-white/10 px-3 py-2 text-sm text-white outline-none placeholder:text-white/35 focus:border-gold" />
      <button type="button" onClick={register} disabled={submitting || (event.remainingCapacity === 0 && !event.allowWaitlist)} className="group mt-4 inline-flex min-h-12 w-full items-center justify-between rounded-control bg-gold px-5 py-3 text-[12px] font-bold uppercase tracking-[0.1em] text-green-deep transition-colors hover:bg-gold-dim disabled:cursor-not-allowed disabled:opacity-50">
        {submitting ? (isFrench ? 'Inscription…' : 'Registering…') : event.remainingCapacity === 0 ? (isFrench ? "Rejoindre la liste d’attente" : 'Join waiting list') : (isFrench ? "S’inscrire" : 'Register')}
        <i className={submitting ? 'ri-loader-4-line animate-spin' : 'ri-arrow-right-line'} aria-hidden="true" />
      </button>
      {message && <p className="mt-3 text-xs leading-5 text-gold">{message}</p>}
    </div>
  );
};

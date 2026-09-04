import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { eventsApi } from '../../lib/api/events';
import { buildApiUrl } from '../../lib/api/base-url';
import type { Event, EventRegistration } from '../../lib/api/types';
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

  if (isPast) {
    return <p className="mt-6 border-t border-white/15 pt-5 text-sm text-white/65">{isFrench ? 'Cet événement est terminé.' : 'This event has ended.'}</p>;
  }

  if (event.registrationMode === 'External') {
    const href = event.registrationUrl || event.meetingLink;
    return href ? (
      <a href={href} target="_blank" rel="noopener noreferrer" className="group mt-6 inline-flex min-h-12 w-full items-center justify-between rounded-control bg-gold px-5 py-3 text-[12px] font-bold uppercase tracking-[0.1em] text-green-deep transition-colors hover:bg-gold-dim">
        {externalLabel}<i className="ri-arrow-right-up-line text-lg" aria-hidden="true" />
      </a>
    ) : null;
  }

  if (event.registrationMode === 'Disabled') {
    return <p className="mt-6 border-t border-white/15 pt-5 text-sm text-white/65">{isFrench ? "Aucune inscription n’est requise." : 'No registration is required.'}</p>;
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
        <div className="mt-3 grid grid-cols-2 gap-2">
          <a href={buildApiUrl(`/api/events/${event.id}/calendar.ics`)} className="inline-flex min-h-10 items-center justify-center gap-2 border border-white/20 px-3 text-[10px] font-bold uppercase tracking-[.08em] text-white hover:border-gold hover:text-gold">
            <i className="ri-calendar-event-line" />{isFrench ? 'Calendrier' : 'Calendar'}
          </a>
          <button type="button" onClick={cancel} disabled={submitting} className="min-h-10 border border-white/20 px-3 text-[10px] font-bold uppercase tracking-[.08em] text-white/70 hover:border-red-300 hover:text-white disabled:opacity-50">
            {isFrench ? 'Annuler' : 'Cancel'}
          </button>
        </div>
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

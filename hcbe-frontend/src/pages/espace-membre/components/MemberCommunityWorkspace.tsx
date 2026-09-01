import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { communityApi } from '../../../lib/api/community';
import { messagingApi } from '../../../lib/api/messaging';
import MemberMessagingPanel from './MemberMessagingPanel';
import type {
  ConnectionRequestDto,
  CreateMentorshipApplicationRequest,
  MentorshipApplicationDto,
  MentorshipMatchDto,
  NetworkingProfileDto,
  UpsertNetworkingProfileRequest,
} from '../../../lib/api/types';

type Tab = 'mentorship' | 'network' | 'requests' | 'messages';

const statusTone: Record<string, string> = {
  Pending: 'border-gold/40 bg-gold/10 text-green-deep',
  Approved: 'border-green/25 bg-green/10 text-green',
  Active: 'border-green/25 bg-green/10 text-green',
  Accepted: 'border-green/25 bg-green/10 text-green',
  Proposed: 'border-gold/40 bg-gold/10 text-green-deep',
  Rejected: 'border-red-link/25 bg-red-link/5 text-red-link',
  Declined: 'border-red-link/25 bg-red-link/5 text-red-link',
  Withdrawn: 'border-line bg-canvas text-ink-variant',
  Completed: 'border-line bg-canvas text-ink-variant',
};

const emptyApplication: CreateMentorshipApplicationRequest = {
  role: 'Mentor',
  professionalSummary: '',
  expertise: '',
  objectives: '',
  availability: '',
  preferredLanguage: 'fr',
  consentToShare: false,
};

const emptyProfile: UpsertNetworkingProfileRequest = {
  headline: '', bio: '', expertise: '', sectors: '', city: '', province: '',
  isVisible: false, allowContactRequests: false,
};

const Badge = ({ value }: { value: string }) => (
  <span className={`inline-flex rounded-full border px-2.5 py-1 text-[10px] font-bold uppercase tracking-[0.12em] ${statusTone[value] || statusTone.Completed}`}>
    {value}
  </span>
);

const MemberCommunityWorkspace = () => {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [tab, setTab] = useState<Tab>('mentorship');
  const [applications, setApplications] = useState<MentorshipApplicationDto[]>([]);
  const [matches, setMatches] = useState<MentorshipMatchDto[]>([]);
  const [profile, setProfile] = useState<UpsertNetworkingProfileRequest>(emptyProfile);
  const [directory, setDirectory] = useState<NetworkingProfileDto[]>([]);
  const [requests, setRequests] = useState<ConnectionRequestDto[]>([]);
  const [application, setApplication] = useState(emptyApplication);
  const [search, setSearch] = useState('');
  const [requestTarget, setRequestTarget] = useState<NetworkingProfileDto | null>(null);
  const [requestMessage, setRequestMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [unreadMessages, setUnreadMessages] = useState(0);

  const copy = fr ? {
    eyebrow: 'Espace privé', title: 'Communauté des membres',
    intro: 'Développez des relations utiles dans un cadre confidentiel, volontaire et modéré par le HCBE Canada.',
    mentorship: 'Mentorat', network: 'Annuaire privé', requests: 'Mises en relation', messages: 'Messages',
    apply: 'Proposer mon profil', myApplications: 'Mes candidatures', myMatches: 'Mes jumelages',
    noApplications: 'Aucune candidature de mentorat pour le moment.', noMatches: 'Aucun jumelage proposé.',
    summary: 'Parcours professionnel', expertise: 'Expertise et domaines', objectives: 'Objectifs du mentorat',
    availability: 'Disponibilités', language: 'Langue préférée', consent: 'J’accepte que mon profil soit partagé avec le comité de mentorat et un jumelage potentiel.',
    submit: 'Soumettre au comité', withdraw: 'Retirer', mentor: 'Mentor', mentee: 'Mentoré(e)',
    accept: 'Accepter', decline: 'Refuser', contact: 'Coordonnées partagées',
    profileTitle: 'Mon profil de réseautage', profileHint: 'Vous gardez le contrôle : votre profil reste invisible tant que vous ne l’activez pas.',
    headline: 'Titre professionnel', bio: 'Présentation', sectors: 'Secteurs d’activité', city: 'Ville', province: 'Province / territoire',
    visible: 'Afficher mon profil dans l’annuaire privé', allow: 'Autoriser les demandes de mise en relation', save: 'Enregistrer mon profil',
    directory: 'Membres disponibles', search: 'Rechercher par nom, expertise ou secteur…', noMembers: 'Aucun profil ne correspond à votre recherche.',
    connect: 'Demander une mise en relation', message: 'Présentez brièvement la raison de votre demande', send: 'Envoyer la demande', cancel: 'Annuler',
    received: 'Reçues', sent: 'Envoyées', noRequests: 'Aucune demande de mise en relation.',
    pendingReview: 'Votre candidature sera examinée par un comité avant tout jumelage.', saved: 'Modifications enregistrées.', sentNotice: 'Demande envoyée.', error: 'Une erreur est survenue. Veuillez réessayer.',
  } : {
    eyebrow: 'Private workspace', title: 'Member community',
    intro: 'Build useful relationships in a confidential, voluntary environment moderated by HCBE Canada.',
    mentorship: 'Mentorship', network: 'Private directory', requests: 'Connections', messages: 'Messages',
    apply: 'Submit my profile', myApplications: 'My applications', myMatches: 'My matches',
    noApplications: 'No mentorship applications yet.', noMatches: 'No match has been proposed.',
    summary: 'Professional background', expertise: 'Expertise and fields', objectives: 'Mentorship goals',
    availability: 'Availability', language: 'Preferred language', consent: 'I agree that my profile may be shared with the mentorship committee and a potential match.',
    submit: 'Submit to committee', withdraw: 'Withdraw', mentor: 'Mentor', mentee: 'Mentee',
    accept: 'Accept', decline: 'Decline', contact: 'Shared contact details',
    profileTitle: 'My networking profile', profileHint: 'You stay in control: your profile remains hidden until you enable it.',
    headline: 'Professional headline', bio: 'About me', sectors: 'Business sectors', city: 'City', province: 'Province / territory',
    visible: 'Show my profile in the private directory', allow: 'Allow connection requests', save: 'Save my profile',
    directory: 'Available members', search: 'Search by name, expertise or sector…', noMembers: 'No profiles match your search.',
    connect: 'Request a connection', message: 'Briefly explain why you would like to connect', send: 'Send request', cancel: 'Cancel',
    received: 'Received', sent: 'Sent', noRequests: 'No connection requests.',
    pendingReview: 'A committee will review your application before any match.', saved: 'Changes saved.', sentNotice: 'Request sent.', error: 'Something went wrong. Please try again.',
  };

  const load = async () => {
    setLoading(true);
    setNotice(null);

    try {
      const [apps, matchesResult, profileResult, requestsResult, directoryResult, conversationsResult] = await Promise.allSettled([
        communityApi.getMyApplications(), communityApi.getMyMatches(), communityApi.getMyProfile(),
        communityApi.getMyRequests(), communityApi.searchDirectory(), messagingApi.getConversations(),
      ]);

      if (apps.status === 'fulfilled' && apps.value.success && apps.value.data) setApplications(apps.value.data);
      if (matchesResult.status === 'fulfilled' && matchesResult.value.success && matchesResult.value.data) setMatches(matchesResult.value.data);
      if (profileResult.status === 'fulfilled' && profileResult.value.success && profileResult.value.data) {
        const { headline, bio, expertise, sectors, city, province, isVisible, allowContactRequests } = profileResult.value.data;
        setProfile({ headline, bio, expertise, sectors, city, province, isVisible, allowContactRequests });
      } else {
        // A missing profile is the expected first-login state. Keep the empty form ready
        // instead of blocking the entire member workspace.
        setProfile(emptyProfile);
      }
      if (requestsResult.status === 'fulfilled' && requestsResult.value.success && requestsResult.value.data) setRequests(requestsResult.value.data);
      if (directoryResult.status === 'fulfilled' && directoryResult.value.success && directoryResult.value.data) setDirectory(directoryResult.value.data);
      if (conversationsResult.status === 'fulfilled' && conversationsResult.value.success && conversationsResult.value.data) {
        setUnreadMessages(conversationsResult.value.data.reduce((sum, item) => sum + item.unreadCount, 0));
      }

      const requiredRequestFailed = [apps, matchesResult, requestsResult, directoryResult, conversationsResult]
        .some((result) => result.status === 'rejected');
      if (requiredRequestFailed) setNotice(copy.error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void load(); }, []);

  const run = async (action: () => Promise<{ success: boolean; message?: string }>, successMessage?: string) => {
    setBusy(true); setNotice(null);
    try {
      const result = await action();
      setNotice(result.success ? (successMessage || copy.saved) : (result.message || copy.error));
      if (result.success) await load();
      return result.success;
    } catch {
      setNotice(copy.error);
      return false;
    } finally {
      setBusy(false);
    }
  };

  const filteredDirectory = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return directory;
    return directory.filter((item) => [item.memberName, item.headline, item.expertise, item.sectors, item.city, item.province].some((value) => value?.toLowerCase().includes(term)));
  }, [directory, search]);

  const submitApplication = async (event: React.FormEvent) => {
    event.preventDefault();
    if (await run(() => communityApi.apply(application), copy.pendingReview)) setApplication(emptyApplication);
  };

  const saveProfile = async (event: React.FormEvent) => {
    event.preventDefault();
    await run(() => communityApi.saveProfile(profile));
  };

  const sendRequest = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!requestTarget) return;
    if (await run(() => communityApi.requestConnection(requestTarget.memberId, requestMessage), copy.sentNotice)) {
      setRequestTarget(null); setRequestMessage('');
    }
  };

  return (
    <section className="overflow-hidden rounded-[22px] border border-line bg-surface shadow-[0_20px_60px_rgba(0,59,27,.08)]">
      <div className="relative overflow-hidden bg-green-deep px-6 py-7 text-white sm:px-8">
        <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[38px] border-gold/[0.08]" aria-hidden="true" />
        <p className="text-[10px] font-bold uppercase tracking-[0.22em] text-gold">{copy.eyebrow}</p>
        <h2 className="mt-2 font-display text-3xl font-bold">{copy.title}</h2>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-green-dim">{copy.intro}</p>
      </div>

      <div className="border-b border-line px-4 sm:px-8">
        <div className="flex gap-1 overflow-x-auto" role="tablist">
          {([['mentorship', 'ri-user-heart-line', copy.mentorship], ['network', 'ri-group-line', copy.network], ['requests', 'ri-mail-open-line', copy.requests], ['messages', 'ri-chat-3-line', copy.messages]] as const).map(([value, icon, label]) => (
            <button key={value} type="button" onClick={() => setTab(value)} className={`flex shrink-0 items-center gap-2 border-b-2 px-4 py-4 text-xs font-bold uppercase tracking-[0.12em] ${tab === value ? 'border-gold text-green' : 'border-transparent text-ink-variant hover:text-green'}`}>
              <i className={icon} aria-hidden="true" />{label}{value === 'requests' && requests.filter((item) => item.direction === 'Received' && item.status === 'Pending').length > 0 && <span className="rounded-full bg-gold px-2 py-0.5 text-green-deep">{requests.filter((item) => item.direction === 'Received' && item.status === 'Pending').length}</span>}{value === 'messages' && unreadMessages > 0 && <span className="rounded-full bg-gold px-2 py-0.5 text-green-deep">{unreadMessages}</span>}
            </button>
          ))}
        </div>
      </div>

      <div className="p-5 sm:p-8">
        {notice && <p className="mb-5 border-l-2 border-gold bg-canvas px-4 py-3 text-sm text-ink-variant">{notice}</p>}
        {loading ? <div className="py-12 text-center text-ink-variant"><i className="ri-loader-4-line animate-spin text-2xl" /></div> : null}

        {!loading && tab === 'mentorship' && (
          <div className="grid gap-8 xl:grid-cols-[minmax(0,.88fr)_minmax(0,1.12fr)]">
            <form onSubmit={submitApplication} className="rounded-2xl border border-line bg-canvas/55 p-5">
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-red-link">{copy.apply}</p>
              <div className="mt-5 grid grid-cols-2 gap-2">
                {(['Mentor', 'Mentee'] as const).map((role) => <button key={role} type="button" onClick={() => setApplication({ ...application, role })} className={`rounded-xl border px-4 py-3 text-sm font-semibold ${application.role === role ? 'border-green bg-green text-white' : 'border-line bg-surface text-ink-variant'}`}>{role === 'Mentor' ? copy.mentor : copy.mentee}</button>)}
              </div>
              <div className="mt-5 space-y-4">
                <Field label={copy.summary} htmlFor="mentor-summary"><textarea id="mentor-summary" required minLength={20} rows={3} className={inputClasses} value={application.professionalSummary} onChange={(e) => setApplication({ ...application, professionalSummary: e.target.value })} /></Field>
                <Field label={copy.expertise} htmlFor="mentor-expertise"><textarea id="mentor-expertise" required minLength={10} rows={2} className={inputClasses} value={application.expertise} onChange={(e) => setApplication({ ...application, expertise: e.target.value })} /></Field>
                <Field label={copy.objectives} htmlFor="mentor-objectives"><textarea id="mentor-objectives" required minLength={20} rows={3} className={inputClasses} value={application.objectives} onChange={(e) => setApplication({ ...application, objectives: e.target.value })} /></Field>
                <Field label={copy.availability} htmlFor="mentor-availability"><input id="mentor-availability" required className={inputClasses} value={application.availability} onChange={(e) => setApplication({ ...application, availability: e.target.value })} /></Field>
                <Field label={copy.language} htmlFor="mentor-language"><select id="mentor-language" className={inputClasses} value={application.preferredLanguage} onChange={(e) => setApplication({ ...application, preferredLanguage: e.target.value as 'fr' | 'en' })}><option value="fr">Français</option><option value="en">English</option></select></Field>
                <label className="flex cursor-pointer gap-3 text-sm leading-5 text-ink-variant"><input type="checkbox" required checked={application.consentToShare} onChange={(e) => setApplication({ ...application, consentToShare: e.target.checked })} className="mt-0.5 h-4 w-4 accent-green" /><span>{copy.consent}</span></label>
                <Button type="submit" variant="secondary" className="w-full" disabled={busy}>{copy.submit}</Button>
              </div>
            </form>

            <div className="space-y-7">
              <div><h3 className="font-display text-xl font-bold text-green-deep">{copy.myApplications}</h3><div className="mt-3 space-y-3">{applications.length === 0 ? <p className="rounded-xl border border-dashed border-line p-5 text-sm text-ink-variant">{copy.noApplications}</p> : applications.map((item) => <article key={item.id} className="rounded-xl border border-line p-4"><div className="flex flex-wrap items-center justify-between gap-2"><h4 className="font-semibold text-green-deep">{item.role === 'Mentor' ? copy.mentor : copy.mentee}</h4><Badge value={item.status} /></div><p className="mt-2 line-clamp-2 text-sm leading-6 text-ink-variant">{item.objectives}</p>{item.committeeNotes && <p className="mt-2 border-l-2 border-gold pl-3 text-xs text-ink-variant">{item.committeeNotes}</p>}{['Pending', 'Approved'].includes(item.status) && <button type="button" disabled={busy} onClick={() => void run(() => communityApi.withdraw(item.id))} className="mt-3 text-[10px] font-bold uppercase tracking-[0.12em] text-red-link">{copy.withdraw}</button>}</article>)}</div></div>
              <div><h3 className="font-display text-xl font-bold text-green-deep">{copy.myMatches}</h3><div className="mt-3 space-y-3">{matches.length === 0 ? <p className="rounded-xl border border-dashed border-line p-5 text-sm text-ink-variant">{copy.noMatches}</p> : matches.map((match) => <article key={match.id} className="rounded-xl border border-line bg-canvas/45 p-4"><div className="flex flex-wrap items-center justify-between gap-2"><h4 className="font-display text-lg font-bold text-green-deep">{match.counterpartName || `${match.mentorName} / ${match.menteeName}`}</h4><Badge value={match.status} /></div>{match.committeeNotes && <p className="mt-2 text-sm leading-6 text-ink-variant">{match.committeeNotes}</p>}{match.status === 'Proposed' && <div className="mt-4 flex gap-2"><Button type="button" variant="primary" disabled={busy} onClick={() => void run(() => communityApi.respondToMatch(match.id, 'Accept'))}>{copy.accept}</Button><Button type="button" variant="tertiary" disabled={busy} onClick={() => void run(() => communityApi.respondToMatch(match.id, 'Decline'))}>{copy.decline}</Button></div>}{match.counterpartEmail && <p className="mt-4 rounded-lg bg-green/5 px-3 py-2 text-sm text-green"><span className="font-semibold">{copy.contact} :</span> {match.counterpartEmail}</p>}</article>)}</div></div>
            </div>
          </div>
        )}

        {!loading && tab === 'network' && (
          <div className="grid gap-8 xl:grid-cols-[minmax(300px,.72fr)_minmax(0,1.28fr)]">
            <form onSubmit={saveProfile} className="rounded-2xl border border-line bg-canvas/55 p-5">
              <h3 className="font-display text-xl font-bold text-green-deep">{copy.profileTitle}</h3><p className="mt-1 text-sm leading-5 text-ink-variant">{copy.profileHint}</p>
              <div className="mt-5 space-y-4">
                <Field label={copy.headline} htmlFor="network-headline"><input id="network-headline" required className={inputClasses} value={profile.headline} onChange={(e) => setProfile({ ...profile, headline: e.target.value })} /></Field>
                <Field label={copy.bio} htmlFor="network-bio"><textarea id="network-bio" required minLength={20} rows={3} className={inputClasses} value={profile.bio} onChange={(e) => setProfile({ ...profile, bio: e.target.value })} /></Field>
                <Field label={copy.expertise} htmlFor="network-expertise"><input id="network-expertise" required className={inputClasses} value={profile.expertise} onChange={(e) => setProfile({ ...profile, expertise: e.target.value })} /></Field>
                <Field label={copy.sectors} htmlFor="network-sectors"><input id="network-sectors" required className={inputClasses} value={profile.sectors} onChange={(e) => setProfile({ ...profile, sectors: e.target.value })} /></Field>
                <div className="grid gap-3 sm:grid-cols-2"><Field label={copy.city} htmlFor="network-city"><input id="network-city" className={inputClasses} value={profile.city || ''} onChange={(e) => setProfile({ ...profile, city: e.target.value })} /></Field><Field label={copy.province} htmlFor="network-province"><input id="network-province" className={inputClasses} value={profile.province || ''} onChange={(e) => setProfile({ ...profile, province: e.target.value })} /></Field></div>
                <label className="flex gap-3 text-sm text-ink-variant"><input type="checkbox" className="mt-0.5 h-4 w-4 accent-green" checked={profile.isVisible} onChange={(e) => setProfile({ ...profile, isVisible: e.target.checked })} />{copy.visible}</label>
                <label className="flex gap-3 text-sm text-ink-variant"><input type="checkbox" className="mt-0.5 h-4 w-4 accent-green" checked={profile.allowContactRequests} onChange={(e) => setProfile({ ...profile, allowContactRequests: e.target.checked })} />{copy.allow}</label>
                <Button type="submit" variant="secondary" className="w-full" disabled={busy}>{copy.save}</Button>
              </div>
            </form>

            <div><div className="flex flex-wrap items-end justify-between gap-3"><div><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.network}</p><h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{copy.directory}</h3></div><input className={`${inputClasses} max-w-sm`} value={search} onChange={(e) => setSearch(e.target.value)} placeholder={copy.search} /></div><div className="mt-5 grid gap-4 sm:grid-cols-2">{filteredDirectory.length === 0 ? <p className="col-span-full rounded-xl border border-dashed border-line p-6 text-sm text-ink-variant">{copy.noMembers}</p> : filteredDirectory.map((item) => <article key={item.id} className="flex flex-col rounded-2xl border border-line p-5 transition-transform hover:-translate-y-0.5"><div className="flex items-center gap-3"><span className="flex h-11 w-11 items-center justify-center rounded-full bg-green text-sm font-bold text-white">{item.memberName.split(' ').map((part) => part[0]).slice(0, 2).join('')}</span><div><h4 className="font-semibold text-green-deep">{item.memberName}</h4><p className="text-xs text-ink-variant">{item.headline}</p></div></div><p className="mt-4 line-clamp-3 text-sm leading-6 text-ink-variant">{item.bio}</p><div className="mt-3 flex flex-wrap gap-2"><span className="rounded-full bg-green/5 px-2.5 py-1 text-xs text-green">{item.expertise}</span>{item.province && <span className="rounded-full bg-canvas px-2.5 py-1 text-xs text-ink-variant">{item.city ? `${item.city}, ` : ''}{item.province}</span>}</div><button type="button" onClick={() => setRequestTarget(item)} className="mt-5 border-t border-line pt-4 text-left text-[10px] font-bold uppercase tracking-[0.13em] text-red-link">{copy.connect} <i className="ri-arrow-right-line" /></button></article>)}</div></div>
          </div>
        )}

        {!loading && tab === 'requests' && (
          <div className="grid gap-7 lg:grid-cols-2">{(['Received', 'Sent'] as const).map((direction) => <div key={direction}><h3 className="font-display text-xl font-bold text-green-deep">{direction === 'Received' ? copy.received : copy.sent}</h3><div className="mt-3 space-y-3">{requests.filter((item) => item.direction === direction).length === 0 ? <p className="rounded-xl border border-dashed border-line p-5 text-sm text-ink-variant">{copy.noRequests}</p> : requests.filter((item) => item.direction === direction).map((item) => <article key={item.id} className="rounded-xl border border-line p-4"><div className="flex items-center justify-between gap-3"><h4 className="font-semibold text-green-deep">{direction === 'Received' ? item.requesterName : item.recipientName}</h4><Badge value={item.status} /></div><p className="mt-3 text-sm leading-6 text-ink-variant">{item.message}</p>{item.sharedEmail && <p className="mt-3 rounded-lg bg-green/5 px-3 py-2 text-sm text-green">{item.sharedEmail}</p>}{direction === 'Received' && item.status === 'Pending' && <div className="mt-4 flex gap-2"><Button type="button" variant="primary" disabled={busy} onClick={() => void run(() => communityApi.respondToRequest(item.id, 'Accepted'))}>{copy.accept}</Button><Button type="button" variant="tertiary" disabled={busy} onClick={() => void run(() => communityApi.respondToRequest(item.id, 'Declined'))}>{copy.decline}</Button></div>}</article>)}</div></div>)}</div>
        )}
        {!loading && tab === 'messages' && <MemberMessagingPanel onUnreadChange={setUnreadMessages} />}
      </div>

      {requestTarget && <div className="fixed inset-0 z-[80] flex items-center justify-center bg-green-deep/70 p-4" role="dialog" aria-modal="true"><form onSubmit={sendRequest} className="w-full max-w-lg rounded-2xl bg-surface p-6 shadow-2xl"><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.connect}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{requestTarget.memberName}</h3><div className="mt-5"><Field label={copy.message} htmlFor="connection-message"><textarea id="connection-message" autoFocus required minLength={10} rows={5} className={inputClasses} value={requestMessage} onChange={(e) => setRequestMessage(e.target.value)} /></Field></div><div className="mt-5 flex gap-3"><Button type="submit" variant="secondary" disabled={busy}>{copy.send}</Button><Button type="button" variant="tertiary" onClick={() => setRequestTarget(null)}>{copy.cancel}</Button></div></form></div>}
    </section>
  );
};

export default MemberCommunityWorkspace;

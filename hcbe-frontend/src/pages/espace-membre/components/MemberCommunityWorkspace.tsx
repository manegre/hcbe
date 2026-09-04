import { useEffect, useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Button, Field, inputClasses } from '../../../components/ui';
import { communityApi } from '../../../lib/api/community';
import { messagingApi } from '../../../lib/api/messaging';
import { notificationsApi } from '../../../lib/api/notifications';
import MemberMessagingPanel from './MemberMessagingPanel';
import MemberServiceCasesPanel from './MemberServiceCasesPanel';
import MemberPreferencesPanel from './MemberPreferencesPanel';
import MemberAssociationsPanel from './MemberAssociationsPanel';
import MemberOpportunitiesPanel from './MemberOpportunitiesPanel';
import MentorshipJourneyPanel from './MentorshipJourneyPanel';
import MemberFinancePanel from './MemberFinancePanel';
import MemberDashboardPanel from './MemberDashboardPanel';
import MemberNotificationsPanel from './MemberNotificationsPanel';
import type {
  ConnectionRequestDto,
  CreateMentorshipApplicationRequest,
  MemberDto,
  MentorshipApplicationDto,
  MentorshipMatchDto,
  NetworkingProfileDto,
  UpsertNetworkingProfileRequest,
} from '../../../lib/api/types';

type Tab = 'overview' | 'membership' | 'services' | 'opportunities' | 'associations' | 'network' | 'mentorship' | 'requests' | 'messages' | 'notifications' | 'profile' | 'preferences';

interface MemberCommunityWorkspaceProps {
  member: MemberDto;
  accountPanel: ReactNode;
  onLogout: () => void;
}

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

const MemberCommunityWorkspace = ({ member, accountPanel, onLogout }: MemberCommunityWorkspaceProps) => {
  const { i18n } = useTranslation();
  const fr = !i18n.language.startsWith('en');
  const [tab, setTab] = useState<Tab>(() => {
    const requested = new URLSearchParams(window.location.search).get('section');
    return ['overview', 'membership', 'services', 'opportunities', 'associations', 'network', 'mentorship', 'requests', 'messages', 'notifications', 'profile', 'preferences'].includes(requested ?? '') ? requested as Tab : 'overview';
  });
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
  const [unreadNotifications, setUnreadNotifications] = useState(0);

  const copy = fr ? {
    eyebrow: 'Espace privé', title: 'Communauté des membres',
    intro: 'Développez des relations utiles dans un cadre confidentiel, volontaire et modéré par le HCBE Canada.',
    mentorship: 'Mentorat', network: 'Annuaire privé', requests: 'Mises en relation', messages: 'Messages', notifications: 'Notifications', services: 'Mes demandes', preferences: 'Mes préférences', associations: 'Associations', opportunities: 'Occasions', membership: 'Mon adhésion',
    apply: 'Proposer mon profil', myApplications: 'Mes candidatures', myMatches: 'Mes jumelages',
    noApplications: 'Aucune candidature de mentorat pour le moment.', noMatches: 'Aucun jumelage proposé.',
    summary: 'Parcours professionnel', expertise: 'Expertise et domaines', objectives: 'Objectifs du mentorat',
    availability: 'Disponibilités', language: 'Langue préférée', consent: 'J’accepte que mon profil soit partagé avec le comité de mentorat et un jumelage potentiel.',
    submit: 'Soumettre au comité', withdraw: 'Retirer', mentor: 'Mentor', mentee: 'Mentoré(e)',
    accept: 'Accepter', decline: 'Refuser', contact: 'Coordonnées partagées',
    profileTitle: 'Mon profil dans la communauté', profileHint: 'Créez votre carte de visite privée à votre rythme. Rien n’est publié sans votre accord.',
    headline: 'Titre professionnel', bio: 'Présentation', sectors: 'Secteurs d’activité', city: 'Ville', province: 'Province / territoire',
    visible: 'Afficher mon profil dans l’annuaire privé', allow: 'Autoriser les demandes de mise en relation', save: 'Enregistrer mon profil',
    directory: 'Membres disponibles', search: 'Rechercher par nom, expertise ou secteur…', noMembers: 'Aucun profil ne correspond à votre recherche.',
    connect: 'Demander une mise en relation', message: 'Présentez brièvement la raison de votre demande', send: 'Envoyer la demande', cancel: 'Annuler',
    received: 'Reçues', sent: 'Envoyées', pending: 'En attente', noRequests: 'Aucune demande de mise en relation.',
    pendingReview: 'Votre candidature sera examinée par un comité avant tout jumelage.', saved: 'Modifications enregistrées.', sentNotice: 'Demande envoyée.', error: 'Une erreur est survenue. Veuillez réessayer.',
    privateLabel: 'Confidentiel et réservé aux membres', profileDraft: 'Profil privé', profileLive: 'Visible dans l’annuaire',
    profileProgress: 'Profil complété', profileIntro: 'Présentez-vous aux membres lorsque vous vous sentez prêt. Votre participation au mentorat reste entièrement facultative.',
    directoryIntro: 'Découvrez les membres qui ont choisi d’être visibles et créez des liens en toute confiance.',
    emptyDirectoryTitle: 'L’annuaire prend vie avec ses membres', emptyDirectoryText: 'Aucun profil visible pour le moment. Complétez le vôtre et choisissez de le publier pour ouvrir la voie.',
    completeProfile: 'Compléter mon profil', communityStat: 'profils visibles', connectionStat: 'demandes reçues', messageStat: 'messages non lus',
    mentorshipIntro: 'Le mentorat est une démarche volontaire. Vous pouvez rejoindre la communauté sans déposer de candidature et revenir ici lorsque le moment sera opportun.',
    optional: 'Facultatif', connectionsIntro: 'Suivez vos demandes et décidez avec qui vous souhaitez entrer en contact.',
    overview: 'Aperçu', profile: 'Mon profil', greeting: 'Bonjour', workspace: 'Mon espace membre',
    welcome: 'Retrouvez l’essentiel de votre communauté en un coup d’œil.',
    overviewTitle: 'Votre communauté, à votre rythme.', overviewIntro: 'Complétez votre présence, découvrez les membres et participez lorsque vous êtes disponible.',
    nextStep: 'Prochaine étape recommandée', finishProfile: 'Compléter mon profil communautaire',
    publishProfile: 'Choisir ma visibilité', browseDirectory: 'Découvrir les membres',
    quickAccess: 'Accès rapides', viewConnections: 'Voir mes demandes', startMentorship: 'Explorer le mentorat',
    activity: 'Votre activité', accountTitle: 'Informations personnelles',
    accountIntro: 'Gardez vos coordonnées à jour. Elles ne sont jamais publiées dans l’annuaire privé.',
    communityProfileTitle: 'Présence dans la communauté', logout: 'Se déconnecter', privacyNote: 'Vos choix de visibilité restent sous votre contrôle.',
    memberLabel: 'Membre HCBE', manageProfile: 'Gérer mon profil', noUnread: 'Aucun message non lu', unreadLabel: 'message(s) à lire',
    noPending: 'Aucune demande en attente', pendingLabel: 'demande(s) en attente',
    mentorshipJourney: 'Parcours de mentorat', mentorshipTitle: 'Un accompagnement humain, au bon moment.',
    mentorshipDescription: 'Choisissez le rôle qui vous correspond, présentez vos attentes, puis laissez le comité construire un jumelage pertinent.',
    chooseRole: 'Quel rôle souhaitez-vous explorer ?', mentorDescription: 'Partager mon expérience et accompagner un membre.',
    menteeDescription: 'Être accompagné(e) dans mes objectifs et mon évolution.', applicationDetails: 'Parlez-nous de votre parcours',
    applicationHint: 'Ces informations aident le comité à comprendre votre profil et à proposer une relation utile.',
    journeyStatus: 'Suivi de mon parcours', journeyStatusHint: 'Retrouvez ici vos candidatures et les jumelages proposés par le comité.',
    applicationEmptyTitle: 'Votre parcours commence ici', applicationEmptyText: 'Après l’envoi, votre candidature apparaîtra ici avec son statut de traitement.',
    matchEmptyTitle: 'Aucun jumelage pour le moment', matchEmptyText: 'Une proposition apparaîtra ici lorsque le comité aura identifié un profil complémentaire.',
    stepProfile: 'Votre profil', stepReview: 'Revue du comité', stepMatch: 'Jumelage humain', voluntary: 'Toujours volontaire',
    profilePresenceEyebrow: 'Carte de membre privée', profilePresenceIntro: 'Présentez votre parcours avec les mots qui vous ressemblent. Vous décidez ensuite si cette carte apparaît dans l’annuaire.',
    identitySection: 'Votre identité professionnelle', locationSection: 'Votre ancrage au Canada', visibilitySection: 'Confidentialité et visibilité',
    visibilityHint: 'Aucune information n’est rendue visible sans une action explicite de votre part.',
    visibleHint: 'Votre carte devient visible uniquement aux membres connectés.', allowHint: 'Les membres pourront vous envoyer une demande avant tout échange.',
    directoryEyebrow: 'Réseau confidentiel', directoryPrivacy: 'Seuls les membres connectés peuvent consulter ces profils et demander une mise en relation.',
    connectionCenter: 'Centre de mises en relation', connectionsTitle: 'Des échanges choisis, jamais imposés.',
    connectionsDescription: 'Consultez les demandes reçues, suivez celles que vous avez envoyées et gardez le contrôle sur chaque nouveau contact.',
    receivedDescription: 'Décidez avec qui vous souhaitez entrer en contact.', sentDescription: 'Suivez la réponse des membres que vous avez contactés.',
  } : {
    eyebrow: 'Private workspace', title: 'Member community',
    intro: 'Build useful relationships in a confidential, voluntary environment moderated by HCBE Canada.',
    mentorship: 'Mentorship', network: 'Private directory', requests: 'Connections', messages: 'Messages', notifications: 'Notifications', services: 'My requests', preferences: 'My preferences', associations: 'Associations', opportunities: 'Opportunities', membership: 'My membership',
    apply: 'Submit my profile', myApplications: 'My applications', myMatches: 'My matches',
    noApplications: 'No mentorship applications yet.', noMatches: 'No match has been proposed.',
    summary: 'Professional background', expertise: 'Expertise and fields', objectives: 'Mentorship goals',
    availability: 'Availability', language: 'Preferred language', consent: 'I agree that my profile may be shared with the mentorship committee and a potential match.',
    submit: 'Submit to committee', withdraw: 'Withdraw', mentor: 'Mentor', mentee: 'Mentee',
    accept: 'Accept', decline: 'Decline', contact: 'Shared contact details',
    profileTitle: 'My community profile', profileHint: 'Build your private member card at your own pace. Nothing is published without your consent.',
    headline: 'Professional headline', bio: 'About me', sectors: 'Business sectors', city: 'City', province: 'Province / territory',
    visible: 'Show my profile in the private directory', allow: 'Allow connection requests', save: 'Save my profile',
    directory: 'Available members', search: 'Search by name, expertise or sector…', noMembers: 'No profiles match your search.',
    connect: 'Request a connection', message: 'Briefly explain why you would like to connect', send: 'Send request', cancel: 'Cancel',
    received: 'Received', sent: 'Sent', pending: 'Pending', noRequests: 'No connection requests.',
    pendingReview: 'A committee will review your application before any match.', saved: 'Changes saved.', sentNotice: 'Request sent.', error: 'Something went wrong. Please try again.',
    privateLabel: 'Confidential and members only', profileDraft: 'Private profile', profileLive: 'Visible in directory',
    profileProgress: 'Profile completed', profileIntro: 'Introduce yourself to members when you feel ready. Taking part in mentorship remains entirely optional.',
    directoryIntro: 'Meet members who have chosen to be visible and build trusted connections.',
    emptyDirectoryTitle: 'The directory grows with its members', emptyDirectoryText: 'No visible profiles yet. Complete yours and choose to publish it to help get the community started.',
    completeProfile: 'Complete my profile', communityStat: 'visible profiles', connectionStat: 'received requests', messageStat: 'unread messages',
    mentorshipIntro: 'Mentorship is entirely voluntary. You can join the community without applying and return here whenever the time feels right.',
    optional: 'Optional', connectionsIntro: 'Review your requests and decide who you would like to connect with.',
    overview: 'Overview', profile: 'My profile', greeting: 'Hello', workspace: 'My member space',
    welcome: 'See the essentials of your community at a glance.',
    overviewTitle: 'Your community, at your pace.', overviewIntro: 'Build your presence, meet members and participate whenever you are available.',
    nextStep: 'Recommended next step', finishProfile: 'Complete my community profile',
    publishProfile: 'Choose my visibility', browseDirectory: 'Discover members',
    quickAccess: 'Quick access', viewConnections: 'View my requests', startMentorship: 'Explore mentorship',
    activity: 'Your activity', accountTitle: 'Personal information',
    accountIntro: 'Keep your contact details current. They are never published in the private directory.',
    communityProfileTitle: 'Community presence', logout: 'Sign out', privacyNote: 'Your visibility choices always remain under your control.',
    memberLabel: 'HCBE member', manageProfile: 'Manage my profile', noUnread: 'No unread messages', unreadLabel: 'message(s) to read',
    noPending: 'No pending requests', pendingLabel: 'pending request(s)',
    mentorshipJourney: 'Mentorship journey', mentorshipTitle: 'Human support, at the right time.',
    mentorshipDescription: 'Choose the role that fits you, share your expectations, and let the committee build a meaningful match.',
    chooseRole: 'Which role would you like to explore?', mentorDescription: 'Share my experience and support another member.',
    menteeDescription: 'Receive support with my goals and professional growth.', applicationDetails: 'Tell us about your journey',
    applicationHint: 'This information helps the committee understand your profile and propose a useful relationship.',
    journeyStatus: 'My journey status', journeyStatusHint: 'Find your applications and committee-proposed matches here.',
    applicationEmptyTitle: 'Your journey starts here', applicationEmptyText: 'Once submitted, your application and review status will appear here.',
    matchEmptyTitle: 'No match yet', matchEmptyText: 'A proposal will appear here when the committee identifies a complementary profile.',
    stepProfile: 'Your profile', stepReview: 'Committee review', stepMatch: 'Human match', voluntary: 'Always voluntary',
    profilePresenceEyebrow: 'Private member card', profilePresenceIntro: 'Present your journey in your own words, then decide whether this card appears in the directory.',
    identitySection: 'Your professional identity', locationSection: 'Your Canadian base', visibilitySection: 'Privacy and visibility',
    visibilityHint: 'No information becomes visible without an explicit action from you.',
    visibleHint: 'Your card becomes visible only to signed-in members.', allowHint: 'Members can send a request before any conversation starts.',
    directoryEyebrow: 'Confidential network', directoryPrivacy: 'Only signed-in members can browse these profiles and request a connection.',
    connectionCenter: 'Connection centre', connectionsTitle: 'Intentional connections, never imposed.',
    connectionsDescription: 'Review incoming requests, follow the ones you sent, and stay in control of every new contact.',
    receivedDescription: 'Choose who you would like to connect with.', sentDescription: 'Follow responses from members you contacted.',
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
  useEffect(() => { notificationsApi.unreadCount().then((result) => setUnreadNotifications(result.data ?? 0)).catch(() => undefined); }, [tab]);

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

  const pendingRequestCount = requests.filter((item) => item.direction === 'Received' && item.status === 'Pending').length;
  const profileCompletion = useMemo(() => {
    const fields = [profile.headline, profile.bio, profile.expertise, profile.sectors, profile.city, profile.province];
    return Math.round((fields.filter((value) => Boolean(value?.trim())).length / fields.length) * 100);
  }, [profile]);

  const focusProfile = () => {
    setTab('profile');
    window.setTimeout(() => {
      document.getElementById('network-headline')?.focus();
      document.getElementById('member-network-profile')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }, 50);
  };

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

  const tabs = [
    ['overview', 'ri-layout-grid-line', copy.overview, 0],
    ['membership', 'ri-bank-card-line', copy.membership, 0],
    ['services', 'ri-customer-service-2-line', copy.services, 0],
    ['opportunities', 'ri-briefcase-4-line', copy.opportunities, 0],
    ['associations', 'ri-building-2-line', copy.associations, 0],
    ['network', 'ri-team-line', copy.network, directory.length],
    ['mentorship', 'ri-user-heart-line', copy.mentorship, applications.length],
    ['requests', 'ri-links-line', copy.requests, pendingRequestCount],
    ['messages', 'ri-chat-smile-2-line', copy.messages, unreadMessages],
    ['notifications', 'ri-notification-3-line', copy.notifications, unreadNotifications],
    ['profile', 'ri-user-settings-line', copy.profile, 0],
    ['preferences', 'ri-notification-3-line', copy.preferences, 0],
  ] as const;

  const memberInitials = `${member.firstName?.[0] || ''}${member.lastName?.[0] || ''}`.toUpperCase();
  const recommendedAction = profileCompletion < 100
    ? copy.finishProfile
    : !profile.isVisible
      ? copy.publishProfile
      : copy.browseDirectory;

  return (
    <section className="min-h-[calc(100vh-76px)] overflow-hidden border-y border-line bg-surface">
      <header className="relative isolate overflow-hidden bg-green-deep px-5 py-6 text-white sm:px-8 lg:px-10 xl:px-12 lg:py-7">
        <div className="absolute inset-0 -z-10 opacity-40 [background-image:linear-gradient(rgba(255,255,255,.035)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.035)_1px,transparent_1px)] [background-size:48px_48px]" aria-hidden="true" />
        <div className="absolute -right-20 -top-28 -z-10 h-72 w-72 rounded-full border-[44px] border-gold/[0.10]" aria-hidden="true" />
        <div className="flex flex-col gap-5 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex min-w-0 items-center gap-4">
            <span className="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl border border-white/15 bg-white/[0.09] font-display text-xl font-bold text-gold shadow-[0_12px_30px_rgba(0,0,0,.16)]">
              {memberInitials}
            </span>
            <div className="min-w-0">
              <p className="text-[9px] font-bold uppercase tracking-[0.22em] text-gold">{copy.workspace}</p>
              <h1 className="mt-1 truncate font-display text-2xl font-bold text-white sm:text-3xl">{copy.greeting}, {member.firstName}</h1>
              <p className="mt-1 truncate text-xs text-green-dim">{copy.welcome}</p>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <div className="hidden text-right md:block">
              <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-green-dim">{copy.memberLabel}</p>
              <p className="mt-1 max-w-56 truncate text-xs text-white/80">{member.email}</p>
            </div>
            <button type="button" onClick={onLogout} className="inline-flex h-11 items-center gap-2 rounded-xl border border-white/15 bg-white/[0.06] px-4 text-[10px] font-bold uppercase tracking-[0.12em] text-white transition-colors hover:border-gold/50 hover:bg-white/[0.10]">
              <i className="ri-logout-box-r-line text-base" aria-hidden="true" />
              <span className="hidden sm:inline">{copy.logout}</span>
            </button>
          </div>
        </div>
      </header>

      <div className="lg:grid lg:min-h-[calc(100vh-188px)] lg:grid-cols-[248px_minmax(0,1fr)] xl:grid-cols-[268px_minmax(0,1fr)]">
      <nav className="border-b border-line bg-canvas/70 p-3 lg:border-b-0 lg:border-r lg:p-4 xl:p-5" aria-label={copy.title}>
        <div className="flex gap-2 overflow-x-auto pb-1 lg:block lg:space-y-1 lg:overflow-visible" role="tablist">
          {tabs.map(([value, icon, label, count]) => (
            <button
              key={value}
              type="button"
              role="tab"
              aria-selected={tab === value}
              onClick={() => setTab(value)}
              className={`group flex min-h-12 min-w-[150px] items-center gap-3 rounded-xl border px-3 py-2 text-left transition-all duration-200 lg:w-full lg:min-w-0 ${tab === value ? 'border-green/15 bg-surface text-green shadow-[0_8px_24px_rgba(0,59,27,.08)]' : 'border-transparent text-ink-variant hover:border-line hover:bg-surface/70 hover:text-green'}`}
            >
              <span className={`flex h-8 w-8 shrink-0 items-center justify-center rounded-lg text-base transition-colors ${tab === value ? 'bg-green text-white' : 'bg-surface text-green group-hover:bg-green/10'}`}><i className={icon} aria-hidden="true" /></span>
              <span className="min-w-0 flex-1 truncate text-[10px] font-bold uppercase tracking-[0.11em]">{label}</span>
              {count > 0 && <span className="rounded-full bg-gold px-2 py-0.5 text-[10px] font-bold text-green-deep">{count}</span>}
            </button>
          ))}
        </div>
        <div className="mt-6 hidden rounded-2xl border border-line bg-surface p-4 lg:block">
          <i className="ri-shield-check-line text-xl text-green" aria-hidden="true" />
          <p className="mt-3 text-[9px] font-bold uppercase tracking-[0.14em] text-green">{copy.privateLabel}</p>
          <p className="mt-2 text-xs leading-5 text-ink-variant">{copy.privacyNote}</p>
        </div>
      </nav>

      <div className="min-w-0 bg-canvas/25 p-4 sm:p-7 lg:p-8 xl:p-10 2xl:p-12">
        {notice && <div className="mb-6 flex items-start gap-3 rounded-2xl border border-gold/30 bg-gold/[0.07] px-4 py-3 text-sm text-ink-variant"><i className="ri-information-line mt-0.5 text-lg text-green" aria-hidden="true" /><p>{notice}</p></div>}

        {loading && (
          <div className="grid gap-5 lg:grid-cols-3" aria-label="Loading">
            {[0, 1, 2].map((item) => <div key={item} className="h-36 animate-pulse rounded-3xl border border-line bg-canvas" />)}
          </div>
        )}

        {!loading && tab === 'overview' && <MemberDashboardPanel onNavigate={(section) => setTab(section)} />}

        {!loading && false && tab === 'overview' && (
          <div className="space-y-7">
            <div className="grid gap-5 xl:grid-cols-[minmax(0,1.45fr)_minmax(280px,.75fr)]">
              <section className="relative overflow-hidden rounded-[26px] bg-green-deep p-6 text-white sm:p-8">
                <div className="absolute -bottom-20 -right-16 h-56 w-56 rounded-full border-[38px] border-gold/[0.09]" aria-hidden="true" />
                <div className="relative max-w-2xl">
                  <p className="text-[9px] font-bold uppercase tracking-[0.2em] text-gold">{copy.overview}</p>
                  <h2 className="mt-3 font-display text-3xl font-bold leading-tight text-white sm:text-4xl">{copy.overviewTitle}</h2>
                  <p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{copy.overviewIntro}</p>
                  <div className="mt-7 inline-flex flex-col items-start gap-3 rounded-2xl border border-white/10 bg-white/[0.055] p-4 sm:flex-row sm:items-center sm:gap-5">
                    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-gold text-xl text-green-deep"><i className="ri-compass-3-line" aria-hidden="true" /></span>
                    <div>
                      <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-green-dim">{copy.nextStep}</p>
                      <button type="button" onClick={() => profileCompletion < 100 || !profile.isVisible ? focusProfile() : setTab('network')} className="mt-1 inline-flex items-center gap-2 text-left text-sm font-bold text-white hover:text-gold">
                        {recommendedAction}<i className="ri-arrow-right-line" aria-hidden="true" />
                      </button>
                    </div>
                  </div>
                </div>
              </section>

              <section className="rounded-[26px] border border-line bg-surface p-5 sm:p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.profileProgress}</p>
                    <h3 className="mt-1 font-display text-2xl font-bold text-green-deep">{profileCompletion}%</h3>
                  </div>
                  <span className="flex h-11 w-11 items-center justify-center rounded-2xl bg-green/10 text-xl text-green"><i className="ri-user-smile-line" aria-hidden="true" /></span>
                </div>
                <div className="mt-5 h-2 overflow-hidden rounded-full bg-line"><div className="h-full rounded-full bg-gold transition-[width] duration-500" style={{ width: `${profileCompletion}%` }} /></div>
                <p className="mt-4 text-sm leading-6 text-ink-variant">{copy.profileIntro}</p>
                <button type="button" onClick={focusProfile} className="mt-5 inline-flex items-center gap-2 text-[10px] font-bold uppercase tracking-[0.13em] text-red-link hover:text-green">{copy.manageProfile}<i className="ri-arrow-right-line" /></button>
              </section>
            </div>

            <section>
              <div className="mb-4 flex items-center gap-3"><h3 className="font-display text-xl font-bold text-green-deep">{copy.activity}</h3><span className="h-px flex-1 bg-line" /></div>
              <div className="grid gap-4 sm:grid-cols-3">
                {[
                  ['ri-group-line', directory.length, copy.communityStat, 'network' as Tab],
                  ['ri-mail-open-line', pendingRequestCount, pendingRequestCount ? copy.pendingLabel : copy.noPending, 'requests' as Tab],
                  ['ri-message-3-line', unreadMessages, unreadMessages ? copy.unreadLabel : copy.noUnread, 'messages' as Tab],
                ].map(([icon, value, label, destination]) => (
                  <button key={String(destination)} type="button" onClick={() => setTab(destination as Tab)} className="group rounded-2xl border border-line bg-surface p-5 text-left transition-all hover:-translate-y-0.5 hover:border-green/20 hover:shadow-[0_12px_30px_rgba(0,59,27,.07)]">
                    <div className="flex items-start justify-between"><span className="flex h-10 w-10 items-center justify-center rounded-xl bg-green/8 text-lg text-green"><i className={String(icon)} aria-hidden="true" /></span><i className="ri-arrow-right-up-line text-ink-variant transition-transform group-hover:-translate-y-0.5 group-hover:translate-x-0.5" /></div>
                    <strong className="mt-5 block font-display text-3xl text-green-deep">{value}</strong>
                    <span className="mt-1 block text-xs leading-5 text-ink-variant">{label}</span>
                  </button>
                ))}
              </div>
            </section>

            <section>
              <div className="mb-4 flex items-center gap-3"><h3 className="font-display text-xl font-bold text-green-deep">{copy.quickAccess}</h3><span className="h-px flex-1 bg-line" /></div>
              <div className="grid gap-3 md:grid-cols-3">
                <PortalShortcut icon="ri-team-line" title={copy.browseDirectory} onClick={() => setTab('network')} />
                <PortalShortcut icon="ri-links-line" title={copy.viewConnections} onClick={() => setTab('requests')} />
                <PortalShortcut icon="ri-user-heart-line" title={copy.startMentorship} onClick={() => setTab('mentorship')} />
              </div>
            </section>
          </div>
        )}

        {!loading && tab === 'network' && (
          <div>
            <section>
              <div className="relative overflow-hidden rounded-[26px] bg-green-deep px-6 py-7 text-white sm:px-8">
                <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[0.09]" aria-hidden="true" />
                <div className="relative flex flex-col gap-6 lg:flex-row lg:items-end lg:justify-between">
                  <div className="max-w-xl">
                    <div className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-[9px] font-bold uppercase tracking-[0.16em] text-gold"><i className="ri-shield-user-line text-sm" aria-hidden="true" />{copy.directoryEyebrow}</div>
                    <h2 className="mt-4 font-display text-3xl font-bold text-white sm:text-4xl">{copy.directory}</h2>
                    <p className="mt-3 text-sm leading-6 text-green-dim">{copy.directoryIntro}</p>
                    <p className="mt-3 flex items-start gap-2 text-xs leading-5 text-white/60"><i className="ri-lock-2-line mt-0.5 text-gold" aria-hidden="true" />{copy.directoryPrivacy}</p>
                  </div>
                  <div className="w-full lg:max-w-sm">
                    <div className="mb-2 flex items-center justify-between text-[9px] font-bold uppercase tracking-[0.13em] text-green-dim"><span>{copy.communityStat}</span><strong className="font-display text-lg text-gold">{directory.length}</strong></div>
                    <label className="relative block"><i className="ri-search-2-line absolute left-4 top-1/2 -translate-y-1/2 text-lg text-green" aria-hidden="true" /><input aria-label={copy.search} className={`${inputClasses} border-white/10 bg-white text-ink pl-11 shadow-[0_10px_28px_rgba(0,0,0,.14)]`} value={search} onChange={(e) => setSearch(e.target.value)} placeholder={copy.search} /></label>
                  </div>
                </div>
              </div>
              <div className="mt-7 grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                {filteredDirectory.length === 0 ? (
                  <div className="col-span-full overflow-hidden rounded-[24px] border border-dashed border-line bg-canvas/55 px-6 py-10 text-center sm:px-10">
                    <div className="mx-auto flex w-fit -space-x-3" aria-hidden="true">
                      {['AS', 'MO', 'BK'].map((initials, index) => <span key={initials} className={`flex h-11 w-11 items-center justify-center rounded-full border-4 border-surface text-xs font-bold ${index === 1 ? 'bg-gold text-green-deep' : 'bg-green text-white'}`}>{initials}</span>)}
                    </div>
                    <h4 className="mt-5 font-display text-2xl font-bold text-green-deep">{copy.emptyDirectoryTitle}</h4>
                    <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-ink-variant">{search ? copy.noMembers : copy.emptyDirectoryText}</p>
                    {!search && <Button type="button" variant="tertiary" className="mt-5" onClick={focusProfile}><i className="ri-user-add-line" aria-hidden="true" />{copy.completeProfile}</Button>}
                  </div>
                ) : filteredDirectory.map((item) => (
                  <article key={item.id} className="group flex flex-col rounded-[22px] border border-line bg-surface p-5 transition-all duration-200 hover:-translate-y-1 hover:border-green/25 hover:shadow-[0_16px_40px_rgba(0,59,27,.09)]">
                    <div className="flex items-center gap-3"><span className="flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-green text-sm font-bold text-white shadow-[0_8px_20px_rgba(0,59,27,.18)]">{item.memberName.split(' ').map((part) => part[0]).slice(0, 2).join('')}</span><div className="min-w-0"><h4 className="truncate font-display text-lg font-bold text-green-deep">{item.memberName}</h4><p className="truncate text-xs text-ink-variant">{item.headline}</p></div></div>
                    <p className="mt-4 line-clamp-3 flex-1 text-sm leading-6 text-ink-variant">{item.bio}</p>
                    <div className="mt-4 flex flex-wrap gap-2"><span className="rounded-full bg-green/5 px-2.5 py-1 text-xs text-green">{item.expertise}</span>{item.province && <span className="rounded-full bg-canvas px-2.5 py-1 text-xs text-ink-variant"><i className="ri-map-pin-2-line mr-1" />{item.city ? `${item.city}, ` : ''}{item.province}</span>}</div>
                    <button type="button" onClick={() => setRequestTarget(item)} className="mt-5 flex items-center justify-between border-t border-line pt-4 text-left text-[10px] font-bold uppercase tracking-[0.13em] text-red-link"><span>{copy.connect}</span><i className="ri-arrow-right-line text-base transition-transform group-hover:translate-x-1" /></button>
                  </article>
                ))}
              </div>
            </section>
          </div>
        )}

        {!loading && tab === 'profile' && (
          <div className="space-y-7">
            <div className="max-w-2xl">
              <p className="text-[10px] font-bold uppercase tracking-[0.18em] text-red-link">{copy.profile}</p>
              <h2 className="mt-1 font-display text-3xl font-bold text-green-deep">{copy.accountTitle}</h2>
              <p className="mt-2 text-sm leading-6 text-ink-variant">{copy.accountIntro}</p>
            </div>
            {accountPanel}

            <form id="member-network-profile" onSubmit={saveProfile} className="scroll-mt-6 overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.07)]">
              <header className="relative overflow-hidden bg-green-deep px-6 py-7 text-white sm:px-8 sm:py-8">
                <div className="absolute -right-16 -top-24 h-56 w-56 rounded-full border-[38px] border-gold/[0.09]" aria-hidden="true" />
                <div className="relative flex flex-col gap-6 sm:flex-row sm:items-end sm:justify-between">
                  <div className="max-w-2xl">
                    <div className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-[9px] font-bold uppercase tracking-[0.15em] text-gold">
                      <i className="ri-id-card-line text-sm" aria-hidden="true" />{copy.profilePresenceEyebrow}
                    </div>
                    <h3 className="mt-4 font-display text-3xl font-bold text-white">{copy.communityProfileTitle}</h3>
                    <p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{copy.profilePresenceIntro}</p>
                  </div>
                  <div className="min-w-52 rounded-2xl border border-white/10 bg-white/[0.055] p-4">
                    <div className="flex items-center justify-between text-[9px] font-bold uppercase tracking-[0.12em] text-green-dim"><span>{copy.profileProgress}</span><strong className="font-display text-lg text-gold">{profileCompletion}%</strong></div>
                    <div className="mt-3 h-2 overflow-hidden rounded-full bg-white/10"><div className="h-full rounded-full bg-gold transition-[width] duration-500" style={{ width: `${profileCompletion}%` }} /></div>
                    <p className="mt-3 text-xs text-white/65">{profile.isVisible ? copy.profileLive : copy.profileDraft}</p>
                  </div>
                </div>
              </header>

              <div className="p-6 sm:p-8">
                <section>
                  <div className="mb-5 flex items-center gap-3"><span className="flex h-8 w-8 items-center justify-center rounded-lg bg-green/[0.08] text-green"><i className="ri-briefcase-4-line" aria-hidden="true" /></span><h4 className="font-display text-xl font-bold text-green-deep">{copy.identitySection}</h4><span className="h-px flex-1 bg-line" /></div>
                  <div className="grid gap-5 sm:grid-cols-2">
                    <div className="sm:col-span-2"><Field label={copy.headline} htmlFor="network-headline"><input id="network-headline" required className={inputClasses} value={profile.headline} onChange={(e) => setProfile({ ...profile, headline: e.target.value })} /></Field></div>
                    <Field label={copy.expertise} htmlFor="network-expertise"><input id="network-expertise" required className={inputClasses} value={profile.expertise} onChange={(e) => setProfile({ ...profile, expertise: e.target.value })} /></Field>
                    <Field label={copy.sectors} htmlFor="network-sectors"><input id="network-sectors" required className={inputClasses} value={profile.sectors} onChange={(e) => setProfile({ ...profile, sectors: e.target.value })} /></Field>
                    <div className="sm:col-span-2"><Field label={copy.bio} htmlFor="network-bio"><textarea id="network-bio" required minLength={20} rows={5} className={`${inputClasses} resize-y`} value={profile.bio} onChange={(e) => setProfile({ ...profile, bio: e.target.value })} /></Field></div>
                  </div>
                </section>

                <section className="mt-8 border-t border-line pt-7">
                  <div className="mb-5 flex items-center gap-3"><span className="flex h-8 w-8 items-center justify-center rounded-lg bg-green/[0.08] text-green"><i className="ri-map-pin-2-line" aria-hidden="true" /></span><h4 className="font-display text-xl font-bold text-green-deep">{copy.locationSection}</h4><span className="h-px flex-1 bg-line" /></div>
                  <div className="grid gap-5 sm:grid-cols-2">
                    <Field label={copy.city} htmlFor="network-city"><input id="network-city" className={inputClasses} value={profile.city || ''} onChange={(e) => setProfile({ ...profile, city: e.target.value })} /></Field>
                    <Field label={copy.province} htmlFor="network-province"><input id="network-province" className={inputClasses} value={profile.province || ''} onChange={(e) => setProfile({ ...profile, province: e.target.value })} /></Field>
                  </div>
                </section>

                <section className="mt-8 overflow-hidden rounded-[22px] border border-line bg-canvas/45">
                  <div className="border-b border-line px-5 py-4 sm:px-6"><h4 className="font-display text-xl font-bold text-green-deep">{copy.visibilitySection}</h4><p className="mt-1 text-xs leading-5 text-ink-variant">{copy.visibilityHint}</p></div>
                  <div className="grid gap-3 p-4 sm:grid-cols-2 sm:p-5">
                    <ProfileToggle icon="ri-eye-line" label={copy.visible} description={copy.visibleHint} checked={profile.isVisible} onChange={(checked) => setProfile({ ...profile, isVisible: checked })} />
                    <ProfileToggle icon="ri-links-line" label={copy.allow} description={copy.allowHint} checked={profile.allowContactRequests} onChange={(checked) => setProfile({ ...profile, allowContactRequests: checked })} />
                  </div>
                </section>

                <div className="mt-7 flex flex-col gap-4 border-t border-line pt-6 sm:flex-row sm:items-center sm:justify-between">
                  <p className="flex items-center gap-2 text-xs text-ink-variant"><i className="ri-shield-check-line text-base text-green" aria-hidden="true" />{copy.privacyNote}</p>
                  <Button type="submit" variant="secondary" disabled={busy} className="w-full sm:w-auto sm:min-w-64"><i className="ri-save-line" aria-hidden="true" />{copy.save}</Button>
                </div>
              </div>
            </form>
          </div>
        )}

        {!loading && tab === 'mentorship' && (
          <div className="space-y-7">
            <section className="relative overflow-hidden rounded-[26px] bg-green-deep px-6 py-7 text-white sm:px-8 sm:py-8">
              <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[0.09]" aria-hidden="true" />
              <div className="relative grid gap-7 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
                <div className="max-w-2xl">
                  <div className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-[9px] font-bold uppercase tracking-[0.16em] text-gold">
                    <i className="ri-heart-pulse-line text-sm" aria-hidden="true" />{copy.mentorshipJourney}
                  </div>
                  <h2 className="mt-4 font-display text-3xl font-bold leading-tight text-white sm:text-4xl">{copy.mentorshipTitle}</h2>
                  <p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{copy.mentorshipDescription}</p>
                </div>
                <div className="grid grid-cols-3 gap-2">
                  {[
                    ['01', copy.stepProfile],
                    ['02', copy.stepReview],
                    ['03', copy.stepMatch],
                  ].map(([step, label]) => (
                    <div key={step} className="min-w-0 rounded-2xl border border-white/10 bg-white/[0.055] px-3 py-3 sm:min-w-28">
                      <span className="font-display text-lg font-bold text-gold">{step}</span>
                      <p className="mt-1 text-[8px] font-bold uppercase leading-4 tracking-[0.11em] text-green-dim">{label}</p>
                    </div>
                  ))}
                </div>
              </div>
            </section>

            <div className="grid gap-7 2xl:grid-cols-[minmax(0,1.35fr)_minmax(320px,.65fr)]">
              <form onSubmit={submitApplication} className="overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.07)]">
                <div className="border-b border-line bg-green/[0.045] px-6 py-5 sm:px-7">
                  <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.apply}</p>
                  <h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{copy.chooseRole}</h3>
                </div>

                <div className="p-6 sm:p-7">
                  <div className="grid gap-3 sm:grid-cols-2">
                    {(['Mentor', 'Mentee'] as const).map((role) => {
                      const selected = application.role === role;
                      return (
                        <button key={role} type="button" onClick={() => setApplication({ ...application, role })} className={`group flex min-h-28 items-start gap-4 rounded-2xl border p-4 text-left transition-all ${selected ? 'border-green bg-green text-white shadow-[0_12px_28px_rgba(0,59,27,.14)]' : 'border-line bg-canvas/50 text-ink-variant hover:border-green/25 hover:bg-surface'}`}>
                          <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-lg ${selected ? 'bg-white/12 text-gold' : 'bg-green/[0.08] text-green'}`}>
                            <i className={role === 'Mentor' ? 'ri-user-star-line' : 'ri-seedling-line'} aria-hidden="true" />
                          </span>
                          <span>
                            <strong className={`block font-display text-lg ${selected ? 'text-white' : 'text-green-deep'}`}>{role === 'Mentor' ? copy.mentor : copy.mentee}</strong>
                            <span className={`mt-1 block text-xs leading-5 ${selected ? 'text-white/70' : 'text-ink-variant'}`}>{role === 'Mentor' ? copy.mentorDescription : copy.menteeDescription}</span>
                          </span>
                        </button>
                      );
                    })}
                  </div>

                  <div className="mt-8 border-t border-line pt-7">
                    <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.applicationDetails}</p>
                    <p className="mt-2 max-w-2xl text-sm leading-6 text-ink-variant">{copy.applicationHint}</p>
                  </div>

                  <div className="mt-6 grid gap-5 lg:grid-cols-2">
                    <div className="lg:col-span-2"><Field label={copy.summary} htmlFor="mentor-summary"><textarea id="mentor-summary" required minLength={20} rows={4} className={`${inputClasses} resize-y`} value={application.professionalSummary} onChange={(e) => setApplication({ ...application, professionalSummary: e.target.value })} /></Field></div>
                    <Field label={copy.expertise} htmlFor="mentor-expertise"><textarea id="mentor-expertise" required minLength={10} rows={4} className={`${inputClasses} resize-y`} value={application.expertise} onChange={(e) => setApplication({ ...application, expertise: e.target.value })} /></Field>
                    <Field label={copy.objectives} htmlFor="mentor-objectives"><textarea id="mentor-objectives" required minLength={20} rows={4} className={`${inputClasses} resize-y`} value={application.objectives} onChange={(e) => setApplication({ ...application, objectives: e.target.value })} /></Field>
                    <Field label={copy.availability} htmlFor="mentor-availability"><input id="mentor-availability" required className={inputClasses} value={application.availability} onChange={(e) => setApplication({ ...application, availability: e.target.value })} /></Field>
                    <Field label={copy.language} htmlFor="mentor-language"><select id="mentor-language" className={`${inputClasses} cursor-pointer`} value={application.preferredLanguage} onChange={(e) => setApplication({ ...application, preferredLanguage: e.target.value as 'fr' | 'en' })}><option value="fr">Français</option><option value="en">English</option></select></Field>
                  </div>

                  <label className={`mt-6 flex cursor-pointer items-start gap-4 rounded-2xl border p-4 transition-colors ${application.consentToShare ? 'border-green/25 bg-green/[0.055]' : 'border-line bg-canvas/45 hover:border-green/20'}`}>
                    <input type="checkbox" required checked={application.consentToShare} onChange={(e) => setApplication({ ...application, consentToShare: e.target.checked })} className="mt-1 h-4 w-4 shrink-0 accent-green" />
                    <span><strong className="block text-xs font-semibold text-green-deep">{copy.voluntary}</strong><span className="mt-1 block text-xs leading-5 text-ink-variant">{copy.consent}</span></span>
                  </label>

                  <div className="mt-6 flex justify-end border-t border-line pt-5">
                    <Button type="submit" variant="secondary" className="w-full sm:w-auto sm:min-w-64" disabled={busy}>
                      <i className="ri-send-plane-line" aria-hidden="true" />{copy.submit}
                    </Button>
                  </div>
                </div>
              </form>

              <aside className="self-start overflow-hidden rounded-[26px] border border-line bg-surface shadow-[0_16px_45px_rgba(0,59,27,.06)]">
                <div className="border-b border-line px-6 py-5 sm:px-7">
                  <p className="text-[9px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.activity}</p>
                  <h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{copy.journeyStatus}</h3>
                  <p className="mt-2 text-sm leading-6 text-ink-variant">{copy.journeyStatusHint}</p>
                </div>
                <div className="space-y-7 p-6 sm:p-7">
                  <MentorshipRecordSection title={copy.myApplications} count={applications.length} emptyTitle={copy.applicationEmptyTitle} emptyText={copy.applicationEmptyText} icon="ri-file-list-3-line">
                    {applications.map((item) => (
                      <article key={item.id} className="rounded-2xl border border-line bg-canvas/45 p-4">
                        <div className="flex flex-wrap items-center justify-between gap-2"><h4 className="font-semibold text-green-deep">{item.role === 'Mentor' ? copy.mentor : copy.mentee}</h4><Badge value={item.status} /></div>
                        <p className="mt-2 line-clamp-3 text-sm leading-6 text-ink-variant">{item.objectives}</p>
                        {item.committeeNotes && <p className="mt-3 border-l-2 border-gold pl-3 text-xs leading-5 text-ink-variant">{item.committeeNotes}</p>}
                        {['Pending', 'Approved'].includes(item.status) && <button type="button" disabled={busy} onClick={() => void run(() => communityApi.withdraw(item.id))} className="mt-4 text-[10px] font-bold uppercase tracking-[0.12em] text-red-link">{copy.withdraw}</button>}
                      </article>
                    ))}
                  </MentorshipRecordSection>

                  <MentorshipRecordSection title={copy.myMatches} count={matches.length} emptyTitle={copy.matchEmptyTitle} emptyText={copy.matchEmptyText} icon="ri-user-heart-line">
                    {matches.map((match) => (
                      <article key={match.id} className="rounded-2xl border border-line bg-canvas/45 p-4">
                        <div className="flex flex-wrap items-center justify-between gap-2"><h4 className="font-display text-lg font-bold text-green-deep">{match.counterpartName || `${match.mentorName} / ${match.menteeName}`}</h4><Badge value={match.status} /></div>
                        {match.committeeNotes && <p className="mt-2 text-sm leading-6 text-ink-variant">{match.committeeNotes}</p>}
                        {match.status === 'Proposed' && <div className="mt-4 flex gap-2"><Button type="button" variant="primary" disabled={busy} onClick={() => void run(() => communityApi.respondToMatch(match.id, 'Accept'))}>{copy.accept}</Button><Button type="button" variant="tertiary" disabled={busy} onClick={() => void run(() => communityApi.respondToMatch(match.id, 'Decline'))}>{copy.decline}</Button></div>}
                        {match.counterpartEmail && <p className="mt-4 rounded-lg bg-green/5 px-3 py-2 text-sm text-green"><span className="font-semibold">{copy.contact}:</span> {match.counterpartEmail}</p>}
                        {match.status === 'Active' && <MentorshipJourneyPanel matchId={match.id} />}
                      </article>
                    ))}
                  </MentorshipRecordSection>
                </div>
              </aside>
            </div>
          </div>
        )}

        {!loading && tab === 'requests' && (
          <div className="space-y-7">
            <section className="relative overflow-hidden rounded-[26px] bg-green-deep px-6 py-7 text-white sm:px-8">
              <div className="absolute -right-16 -top-20 h-52 w-52 rounded-full border-[34px] border-gold/[0.09]" aria-hidden="true" />
              <div className="relative grid gap-7 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-end">
                <div className="max-w-2xl">
                  <div className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.06] px-3 py-1.5 text-[9px] font-bold uppercase tracking-[0.16em] text-gold"><i className="ri-links-line text-sm" aria-hidden="true" />{copy.connectionCenter}</div>
                  <h2 className="mt-4 font-display text-3xl font-bold leading-tight text-white sm:text-4xl">{copy.connectionsTitle}</h2>
                  <p className="mt-3 max-w-xl text-sm leading-6 text-green-dim">{copy.connectionsDescription}</p>
                </div>
                <div className="grid grid-cols-3 gap-2">
                  {[
                    [requests.filter((item) => item.direction === 'Received').length, copy.received],
                    [requests.filter((item) => item.direction === 'Sent').length, copy.sent],
                    [pendingRequestCount, copy.pending],
                  ].map(([value, label]) => (
                    <div key={String(label)} className="min-w-20 rounded-2xl border border-white/10 bg-white/[0.055] px-3 py-3 text-center sm:min-w-24"><strong className="font-display text-2xl text-gold">{value}</strong><p className="mt-1 text-[8px] font-bold uppercase tracking-[0.11em] text-green-dim">{label}</p></div>
                  ))}
                </div>
              </div>
            </section>

            <div className="grid gap-6 xl:grid-cols-2">
              {(['Received', 'Sent'] as const).map((direction) => {
                const directionalRequests = requests.filter((item) => item.direction === direction);
                const received = direction === 'Received';
                return (
                  <section key={direction} className="overflow-hidden rounded-[24px] border border-line bg-surface shadow-[0_14px_38px_rgba(0,59,27,.055)]">
                    <header className="flex items-start gap-4 border-b border-line bg-green/[0.04] px-5 py-5 sm:px-6">
                      <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-green text-xl text-white"><i className={received ? 'ri-inbox-archive-line' : 'ri-send-plane-line'} aria-hidden="true" /></span>
                      <div className="min-w-0 flex-1"><div className="flex items-center gap-3"><h3 className="font-display text-2xl font-bold text-green-deep">{received ? copy.received : copy.sent}</h3><span className="rounded-full bg-gold px-2.5 py-1 text-[10px] font-bold text-green-deep">{directionalRequests.length}</span></div><p className="mt-1 text-xs leading-5 text-ink-variant">{received ? copy.receivedDescription : copy.sentDescription}</p></div>
                    </header>
                    <div className="space-y-3 p-4 sm:p-5">
                      {directionalRequests.length === 0 ? (
                        <div className="rounded-2xl border border-dashed border-line bg-canvas/45 px-5 py-8 text-center"><span className="mx-auto flex h-11 w-11 items-center justify-center rounded-xl bg-surface text-xl text-green"><i className="ri-mail-open-line" aria-hidden="true" /></span><p className="mt-4 text-sm text-ink-variant">{copy.noRequests}</p></div>
                      ) : directionalRequests.map((item) => {
                        const personName = received ? item.requesterName : item.recipientName;
                        const initials = personName.split(' ').map((part) => part[0]).slice(0, 2).join('');
                        return (
                          <article key={item.id} className="rounded-2xl border border-line bg-canvas/35 p-4 transition-colors hover:border-green/20">
                            <div className="flex items-start gap-3"><span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-green/[0.09] text-xs font-bold text-green">{initials}</span><div className="min-w-0 flex-1"><div className="flex flex-wrap items-center justify-between gap-2"><h4 className="font-display text-lg font-bold text-green-deep">{personName}</h4><Badge value={item.status} /></div><p className="mt-2 text-sm leading-6 text-ink-variant">{item.message}</p></div></div>
                            {item.sharedEmail && <p className="mt-4 rounded-xl border border-green/10 bg-green/[0.055] px-3 py-2 text-sm text-green"><i className="ri-mail-line mr-2" />{item.sharedEmail}</p>}
                            {received && item.status === 'Pending' && <div className="mt-4 flex flex-wrap justify-end gap-2 border-t border-line pt-4"><Button type="button" variant="tertiary" disabled={busy} onClick={() => void run(() => communityApi.respondToRequest(item.id, 'Declined'))}>{copy.decline}</Button><Button type="button" variant="primary" disabled={busy} onClick={() => void run(() => communityApi.respondToRequest(item.id, 'Accepted'))}>{copy.accept}</Button></div>}
                          </article>
                        );
                      })}
                    </div>
                  </section>
                );
              })}
            </div>
          </div>
        )}

        {!loading && tab === 'messages' && <MemberMessagingPanel onUnreadChange={setUnreadMessages} />}
        {!loading && tab === 'notifications' && <MemberNotificationsPanel onUnreadChange={setUnreadNotifications} />}
        {!loading && tab === 'membership' && <MemberFinancePanel member={member} />}
        {!loading && tab === 'services' && <MemberServiceCasesPanel />}
        {!loading && tab === 'preferences' && <MemberPreferencesPanel />}
        {!loading && tab === 'associations' && <MemberAssociationsPanel />}
        {!loading && tab === 'opportunities' && <MemberOpportunitiesPanel />}
      </div>
      </div>

      {requestTarget && <div className="fixed inset-0 z-[80] flex items-center justify-center bg-green-deep/80 p-4 backdrop-blur-sm" role="dialog" aria-modal="true"><form onSubmit={sendRequest} className="w-full max-w-lg overflow-hidden rounded-[24px] border border-white/10 bg-surface shadow-2xl"><div className="bg-green/[0.055] p-6"><p className="text-[10px] font-bold uppercase tracking-[0.16em] text-red-link">{copy.connect}</p><h3 className="mt-2 font-display text-2xl font-bold text-green-deep">{requestTarget.memberName}</h3></div><div className="p-6"><Field label={copy.message} htmlFor="connection-message"><textarea id="connection-message" autoFocus required minLength={10} rows={5} className={inputClasses} value={requestMessage} onChange={(e) => setRequestMessage(e.target.value)} /></Field><div className="mt-5 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end"><Button type="button" variant="tertiary" onClick={() => setRequestTarget(null)}>{copy.cancel}</Button><Button type="submit" variant="secondary" disabled={busy}>{copy.send}</Button></div></div></form></div>}
    </section>
  );
};

const PortalShortcut = ({ icon, title, onClick }: { icon: string; title: string; onClick: () => void }) => (
  <button type="button" onClick={onClick} className="group flex items-center gap-4 rounded-2xl border border-line bg-surface p-4 text-left transition-all hover:border-green/20 hover:shadow-[0_12px_30px_rgba(0,59,27,.07)]">
    <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-green/[0.07] text-xl text-green transition-colors group-hover:bg-green group-hover:text-white"><i className={icon} aria-hidden="true" /></span>
    <span className="min-w-0 flex-1 text-sm font-semibold text-green-deep">{title}</span>
    <i className="ri-arrow-right-line text-ink-variant transition-transform group-hover:translate-x-1 group-hover:text-red-link" aria-hidden="true" />
  </button>
);

const ProfileToggle = ({ icon, label, description, checked, onChange }: { icon: string; label: string; description?: string; checked: boolean; onChange: (checked: boolean) => void }) => (
  <label className={`flex cursor-pointer items-start gap-3 rounded-2xl border p-4 transition-all ${checked ? 'border-green/25 bg-green/[0.06] shadow-[0_8px_24px_rgba(0,59,27,.05)]' : 'border-line bg-surface hover:border-green/20'}`}>
    <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl text-lg ${checked ? 'bg-green text-white' : 'bg-green/[0.07] text-green'}`}><i className={icon} aria-hidden="true" /></span>
    <span className="min-w-0 flex-1"><strong className="block text-xs font-semibold leading-5 text-green-deep">{label}</strong>{description && <span className="mt-1 block text-[11px] leading-5 text-ink-variant">{description}</span>}</span>
    <span className={`relative h-6 w-11 shrink-0 rounded-full transition-colors ${checked ? 'bg-green' : 'bg-line'}`} aria-hidden="true"><span className={`absolute top-1 h-4 w-4 rounded-full bg-white shadow-sm transition-transform ${checked ? 'translate-x-6' : 'translate-x-1'}`} /></span>
    <input type="checkbox" className="sr-only" checked={checked} onChange={(event) => onChange(event.target.checked)} />
  </label>
);

const MentorshipRecordSection = ({
  title,
  count,
  emptyTitle,
  emptyText,
  icon,
  children,
}: {
  title: string;
  count: number;
  emptyTitle: string;
  emptyText: string;
  icon: string;
  children: ReactNode[];
}) => (
  <section>
    <div className="mb-3 flex items-center gap-3">
      <h4 className="font-display text-xl font-bold text-green-deep">{title}</h4>
      <span className="h-px flex-1 bg-line" />
      <span className="flex h-7 min-w-7 items-center justify-center rounded-full bg-green/[0.08] px-2 text-[10px] font-bold text-green">{count}</span>
    </div>
    <div className="space-y-3">
      {children.length > 0 ? children : (
        <div className="rounded-2xl border border-line bg-canvas/45 p-5">
          <span className="flex h-10 w-10 items-center justify-center rounded-xl bg-surface text-lg text-green shadow-[0_6px_18px_rgba(0,59,27,.06)]"><i className={icon} aria-hidden="true" /></span>
          <h5 className="mt-4 font-display text-lg font-bold text-green-deep">{emptyTitle}</h5>
          <p className="mt-2 text-xs leading-5 text-ink-variant">{emptyText}</p>
        </div>
      )}
    </div>
  </section>
);

const RecordSection = ({ title, empty, children }: { title: string; empty: string; children: ReactNode[] }) => (
  <section>
    <div className="mb-3 flex items-center gap-3"><h3 className="font-display text-xl font-bold text-green-deep">{title}</h3><span className="h-px flex-1 bg-line" /></div>
    <div className="space-y-3">{children.length > 0 ? children : <div className="rounded-2xl border border-dashed border-line bg-canvas/45 p-6 text-center"><span className="mx-auto flex h-10 w-10 items-center justify-center rounded-full bg-surface text-lg text-green"><i className="ri-inbox-2-line" /></span><p className="mt-3 text-sm text-ink-variant">{empty}</p></div>}</div>
  </section>
);

export default MemberCommunityWorkspace;

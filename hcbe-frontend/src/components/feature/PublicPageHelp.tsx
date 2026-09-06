import { useEffect, useId, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

type Locale = 'fr' | 'en';
type Localized = Record<Locale, string>;
type LocalizedList = Record<Locale, string[]>;

interface PageGuide {
  match: (pathname: string) => boolean;
  title: Localized;
  description: Localized;
  points: LocalizedList;
}

const exact = (path: string) => (pathname: string) => pathname === path;
const starts = (path: string) => (pathname: string) => pathname.startsWith(path);

const guides: PageGuide[] = [
  {
    match: starts('/actualites/evenements/'),
    title: { fr: 'Comprendre cette activité', en: 'About this activity' },
    description: { fr: 'Cette fiche rassemble tout ce qu’il faut savoir avant de participer.', en: 'This page brings together everything you need before taking part.' },
    points: {
      fr: ['Consultez la date, le lieu, les intervenants et la capacité.', 'Utilisez le bouton principal pour vous inscrire ou acheter un billet.', 'Pour une activité en ligne, le lien de réunion est cliquable lorsqu’il est disponible.'],
      en: ['Check the date, location, speakers, and capacity.', 'Use the main button to register or buy a ticket.', 'For an online activity, the meeting link is clickable when available.'],
    },
  },
  {
    match: exact('/actualites/evenements'),
    title: { fr: 'Trouver un événement', en: 'Find an event' },
    description: { fr: 'Découvrez les rencontres, formations, festivals et activités du réseau.', en: 'Discover meetings, training, festivals, and community activities.' },
    points: {
      fr: ['Filtrez par catégorie, zone ou date.', 'Ouvrez une fiche pour voir les détails et les intervenants.', 'Inscrivez-vous avant la date limite lorsqu’elle est indiquée.'],
      en: ['Filter by category, zone, or date.', 'Open a page to see details and speakers.', 'Register before the deadline when one is shown.'],
    },
  },
  {
    match: starts('/billets/commande/'),
    title: { fr: 'Votre commande de billets', en: 'Your ticket order' },
    description: { fr: 'Finalisez ou consultez une commande sécurisée liée à un événement.', en: 'Complete or review a secure order linked to an event.' },
    points: {
      fr: ['Vérifiez l’événement, les quantités et le montant.', 'Utilisez uniquement les moyens de paiement affichés.', 'Conservez le courriel et les billets QR reçus après confirmation.'],
      en: ['Verify the event, quantities, and amount.', 'Use only the payment methods shown.', 'Keep the confirmation email and QR tickets you receive.'],
    },
  },
  {
    match: starts('/actualites/annonces/'),
    title: { fr: 'À propos de cette annonce', en: 'About this announcement' },
    description: { fr: 'Retrouvez le contenu complet, la date de publication et les liens utiles.', en: 'Find the full story, publication date, and useful links.' },
    points: { fr: ['Vérifiez la date et la source.', 'Utilisez les liens officiels fournis dans l’article.'], en: ['Check the date and source.', 'Use the official links provided in the article.'] },
  },
  {
    match: starts('/actualites'),
    title: { fr: 'Suivre la communauté', en: 'Follow the community' },
    description: { fr: 'Actualités, annonces, événements et souvenirs sont regroupés dans cet espace.', en: 'News, announcements, events, and memories are gathered here.' },
    points: { fr: ['Choisissez le type de contenu qui vous intéresse.', 'Les contenus les plus récents apparaissent en premier.'], en: ['Choose the type of content that interests you.', 'The most recent content appears first.'] },
  },
  {
    match: exact('/services/documents-officiels'),
    title: { fr: 'Télécharger un document', en: 'Download a document' },
    description: { fr: 'Accédez aux formulaires, rapports et ressources officielles publiés par le HCBE.', en: 'Access official forms, reports, and resources published by HCBE.' },
    points: { fr: ['Filtrez ou recherchez le document.', 'Vérifiez sa date et son format avant téléchargement.'], en: ['Filter or search for the document.', 'Check its date and format before downloading.'] },
  },
  {
    match: exact('/services/comites'),
    title: { fr: 'Contacter un comité', en: 'Contact a committee' },
    description: { fr: 'Chaque comité prend en charge un domaine de service précis.', en: 'Each committee is responsible for a specific service area.' },
    points: { fr: ['Choisissez le comité qui correspond à votre besoin.', 'Connectez-vous pour transmettre une demande privée et en suivre le traitement.'], en: ['Choose the committee that matches your need.', 'Sign in to submit a private request and track its progress.'] },
  },
  {
    match: exact('/services/bourses'),
    title: { fr: 'Trouver du financement', en: 'Find funding' },
    description: { fr: 'Consultez les bourses et subventions pertinentes pour la communauté.', en: 'Browse grants and subsidies relevant to the community.' },
    points: { fr: ['Vérifiez les critères et la date limite.', 'Préparez les documents demandés avant de suivre le lien de candidature.'], en: ['Check eligibility and the deadline.', 'Prepare required documents before following the application link.'] },
  },
  {
    match: starts('/services'),
    title: { fr: 'Utiliser les services', en: 'Use HCBE services' },
    description: { fr: 'Cet espace vous oriente vers les ressources, comités et accompagnements disponibles.', en: 'This area guides you to available resources, committees, and support.' },
    points: { fr: ['Choisissez un service pour connaître les conditions.', 'Connectez-vous à l’espace membre lorsque le suivi est personnel.'], en: ['Choose a service to review its requirements.', 'Sign in to the member portal when personal follow-up is required.'] },
  },
  {
    match: starts('/engagement/consultations/'),
    title: { fr: 'Participer à cette consultation', en: 'Take part in this consultation' },
    description: { fr: 'Lisez la proposition, les règles d’admissibilité et la période de participation.', en: 'Read the proposal, eligibility rules, and participation period.' },
    points: { fr: ['Connectez-vous pour vérifier votre admissibilité.', 'Votre choix peut être anonyme ou nominatif selon les règles affichées.', 'Une participation confirmée ne peut pas toujours être modifiée.'], en: ['Sign in to confirm your eligibility.', 'Your choice may be anonymous or named according to the displayed rules.', 'A confirmed response cannot always be changed.'] },
  },
  {
    match: exact('/engagement/consultations'),
    title: { fr: 'Consultations communautaires', en: 'Community consultations' },
    description: { fr: 'Exprimez-vous sur les propositions ouvertes à votre profil de membre.', en: 'Have your say on proposals open to your member profile.' },
    points: { fr: ['Consultez la période et le statut.', 'Ouvrez la proposition pour connaître les règles et voter.'], en: ['Check the participation period and status.', 'Open a proposal to review its rules and vote.'] },
  },
  {
    match: exact('/engagement/annuaire'),
    title: { fr: 'Annuaire des associations', en: 'Association directory' },
    description: { fr: 'Découvrez les organismes burkinabè et communautaires présents au Canada.', en: 'Discover Burkinabè and community organizations across Canada.' },
    points: { fr: ['Recherchez par nom ou territoire.', 'Ouvrez une fiche pour voir les contacts et activités.'], en: ['Search by name or territory.', 'Open a profile to view contacts and activities.'] },
  },
  {
    match: exact('/engagement/projets'),
    title: { fr: 'Projets communautaires', en: 'Community projects' },
    description: { fr: 'Suivez les initiatives en cours et découvrez comment y contribuer.', en: 'Follow active initiatives and discover how to contribute.' },
    points: { fr: ['Ouvrez un projet pour voir ses objectifs et son avancement.', 'Utilisez le contact ou l’appel à participation indiqué.'], en: ['Open a project to see its goals and progress.', 'Use the listed contact or participation call.'] },
  },
  {
    match: starts('/projet/'),
    title: { fr: 'Comprendre ce projet', en: 'About this project' },
    description: { fr: 'Cette fiche présente le porteur, les objectifs, l’échéancier et les besoins du projet.', en: 'This page presents the owner, goals, timeline, and needs of the project.' },
    points: { fr: ['Consultez son statut et sa progression.', 'Participez uniquement par les coordonnées officielles indiquées.'], en: ['Review its status and progress.', 'Participate only through the official contact details shown.'] },
  },
  {
    match: starts('/engagement'),
    title: { fr: 'S’engager dans la communauté', en: 'Get involved in the community' },
    description: { fr: 'Trouvez une association, un projet, une occasion de bénévolat ou une consultation.', en: 'Find an association, project, volunteering opportunity, or consultation.' },
    points: { fr: ['Explorez les parcours proposés.', 'Connectez-vous pour candidater ou suivre vos participations.'], en: ['Explore the available pathways.', 'Sign in to apply or track your participation.'] },
  },
  {
    match: exact('/communaute/ressources'),
    title: { fr: 'Ressources et entreprises', en: 'Resources and businesses' },
    description: { fr: 'Trouvez des ressources, professionnels, entreprises et avantages de la communauté.', en: 'Find community resources, professionals, businesses, and benefits.' },
    points: { fr: ['Filtrez par besoin, secteur ou région.', 'Vérifiez les coordonnées et conditions directement auprès du fournisseur.'], en: ['Filter by need, sector, or region.', 'Confirm details and conditions directly with the provider.'] },
  },
  {
    match: exact('/espace-membre'),
    title: { fr: 'Votre espace membre', en: 'Your member portal' },
    description: { fr: 'Gérez votre adhésion, votre profil, vos demandes, activités et préférences au même endroit.', en: 'Manage your membership, profile, requests, activities, and preferences in one place.' },
    points: { fr: ['Utilisez les onglets pour changer de rubrique.', 'Activez votre adhésion depuis Mon adhésion si elle est inactive.', 'Vos droits de confidentialité et la suppression du compte se trouvent dans les paramètres.'], en: ['Use the tabs to move between sections.', 'Activate your membership under My membership if it is inactive.', 'Privacy rights and account deletion are available in settings.'] },
  },
  {
    match: exact('/contribuer'),
    title: { fr: 'Faire une contribution', en: 'Make a contribution' },
    description: { fr: 'Soutenez les activités du HCBE au moyen d’un paiement sécurisé.', en: 'Support HCBE activities through a secure payment.' },
    points: { fr: ['Choisissez le montant et la campagne.', 'Vérifiez vos coordonnées avant le paiement.', 'Téléchargez votre reçu PDF après confirmation.'], en: ['Choose an amount and campaign.', 'Verify your details before payment.', 'Download your PDF receipt after confirmation.'] },
  },
  {
    match: exact('/paiement/merci'),
    title: { fr: 'Confirmation de paiement', en: 'Payment confirmation' },
    description: { fr: 'Cette page confirme le résultat de votre transaction.', en: 'This page confirms the result of your transaction.' },
    points: { fr: ['Conservez la référence affichée.', 'Téléchargez le reçu PDF lorsqu’il est disponible.', 'Contactez le HCBE si le statut reste indéterminé.'], en: ['Keep the displayed reference.', 'Download the PDF receipt when available.', 'Contact HCBE if the status remains unclear.'] },
  },
  {
    match: exact('/contact'),
    title: { fr: 'Nous joindre', en: 'Contact us' },
    description: { fr: 'Choisissez le bon canal pour que votre message soit traité efficacement.', en: 'Choose the right channel so your message can be handled efficiently.' },
    points: { fr: ['Sélectionnez le sujet correspondant à votre demande.', 'Évitez d’inclure des renseignements sensibles non nécessaires.', 'Un membre peut suivre ses demandes depuis son espace privé.'], en: ['Select the subject that matches your request.', 'Avoid including unnecessary sensitive information.', 'Members can track requests from their private portal.'] },
  },
  {
    match: exact('/confidentialite'),
    title: { fr: 'Vos renseignements personnels', en: 'Your personal information' },
    description: { fr: 'Découvrez comment vos données sont utilisées et exercez vos droits.', en: 'Learn how your data is used and exercise your rights.' },
    points: { fr: ['Vous pouvez retirer certains consentements.', 'Vous pouvez demander l’accès, la correction ou la suppression de vos données.', 'Les demandes sont traitées selon les obligations applicables de la Loi 25.'], en: ['You can withdraw certain consents.', 'You can request access, correction, or deletion of your data.', 'Requests are handled under applicable Law 25 obligations.'] },
  },
  {
    match: exact('/'),
    title: { fr: 'Bienvenue sur HCBE Canada', en: 'Welcome to HCBE Canada' },
    description: { fr: 'Le site rassemble services, actualités, engagement et outils destinés à la communauté.', en: 'The website brings together services, news, engagement, and tools for the community.' },
    points: { fr: ['Explorez librement les contenus publics.', 'Devenez membre ou connectez-vous pour accéder aux services personnalisés.'], en: ['Explore public content freely.', 'Become a member or sign in to access personalized services.'] },
  },
];

const fallbackGuide: PageGuide = {
  match: () => true,
  title: { fr: 'Besoin d’aide sur cette page?', en: 'Need help on this page?' },
  description: { fr: 'Utilisez la navigation pour découvrir les services et contenus du HCBE Canada.', en: 'Use the navigation to discover HCBE Canada services and content.' },
  points: { fr: ['Les fonctionnalités personnelles nécessitent une connexion.', 'Utilisez la page Contact si vous avez besoin d’accompagnement.'], en: ['Personal features require you to sign in.', 'Use the Contact page if you need assistance.'] },
};

const PublicPageHelp = () => {
  const { pathname } = useLocation();
  const { i18n } = useTranslation();
  const locale: Locale = i18n.resolvedLanguage?.startsWith('en') ? 'en' : 'fr';
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);
  const titleId = useId();
  const guide = guides.find((item) => item.match(pathname)) ?? fallbackGuide;

  useEffect(() => setOpen(false), [pathname, locale]);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (event: PointerEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false);
    };
    document.addEventListener('pointerdown', onPointerDown);
    window.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  if (pathname.startsWith('/admin') || pathname.startsWith('/adhesion/verifier')) return null;

  const buttonLabel = locale === 'fr' ? 'Aide pour cette page' : 'Help for this page';
  const guideLabel = locale === 'fr' ? 'Guide de cette page' : 'Page guide';

  return (
    <div ref={containerRef} className="fixed bottom-5 left-4 z-[95] sm:bottom-8 sm:left-8" data-testid="public-page-help">
      {open && (
        <section role="dialog" aria-modal="false" aria-labelledby={titleId} className="absolute bottom-[62px] left-0 w-[min(370px,calc(100vw-2rem))] overflow-hidden rounded-[20px] border border-line/70 bg-surface shadow-[0_24px_70px_rgba(0,59,27,.22)]">
          <div className="relative overflow-hidden bg-green-deep px-5 pb-5 pt-4 text-white">
            <span className="pointer-events-none absolute -right-9 -top-12 h-32 w-32 rounded-full border-[22px] border-gold/[0.09]" aria-hidden="true" />
            <div className="relative flex items-start justify-between gap-4">
              <div><p className="text-[9px] font-bold uppercase tracking-[0.2em] text-gold">{guideLabel}</p><h2 id={titleId} className="mt-2 font-display text-[23px] font-bold leading-[1.08]">{guide.title[locale]}</h2></div>
              <button type="button" onClick={() => setOpen(false)} className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full border border-white/15 text-green-dim transition hover:bg-white/10 hover:text-white" aria-label={locale === 'fr' ? 'Fermer l’aide' : 'Close help'}><i className="ri-close-line text-lg" aria-hidden="true" /></button>
            </div>
          </div>
          <div className="px-5 py-5">
            <p className="text-sm leading-6 text-ink-variant">{guide.description[locale]}</p>
            <ul className="mt-4 space-y-3">{guide.points[locale].map((point) => <li key={point} className="grid grid-cols-[22px_1fr] gap-2 text-[13px] leading-5 text-ink"><span className="mt-0.5 flex h-[18px] w-[18px] items-center justify-center rounded-full bg-gold/20 text-green"><i className="ri-check-line text-xs" aria-hidden="true" /></span><span>{point}</span></li>)}</ul>
          </div>
        </section>
      )}
      <button type="button" onClick={() => setOpen((value) => !value)} aria-expanded={open} aria-haspopup="dialog" aria-label={buttonLabel} title={buttonLabel} className="group flex h-12 w-12 items-center justify-center rounded-full border border-gold/55 bg-gold text-green-deep shadow-[0_12px_30px_rgba(0,59,27,.2)] transition hover:-translate-y-0.5 hover:shadow-[0_16px_34px_rgba(0,59,27,.25)] focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-3 focus-visible:outline-green" data-testid="public-page-help-button"><span className="flex h-7 w-7 items-center justify-center rounded-full border-2 border-green-deep font-display text-base font-bold leading-none" aria-hidden="true">i</span></button>
    </div>
  );
};

export default PublicPageHelp;

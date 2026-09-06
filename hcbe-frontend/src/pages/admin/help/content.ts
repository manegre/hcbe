export type HelpLocale = 'fr' | 'en';

type LocalizedText = Record<HelpLocale, string>;
type LocalizedList = Record<HelpLocale, string[]>;

export type HelpCategoryId =
  | 'start'
  | 'content'
  | 'community'
  | 'members'
  | 'finance'
  | 'governance';

export interface HelpCategory {
  id: HelpCategoryId;
  icon: string;
  label: LocalizedText;
}

export interface HelpArticle {
  id: string;
  category: HelpCategoryId;
  icon: string;
  path: string;
  permission?: string;
  title: LocalizedText;
  summary: LocalizedText;
  steps: LocalizedList;
  tips: LocalizedList;
  keywords: LocalizedList;
}

export const helpCategories: HelpCategory[] = [
  { id: 'start', icon: 'ri-compass-3-line', label: { fr: 'Bien démarrer', en: 'Getting started' } },
  { id: 'content', icon: 'ri-quill-pen-line', label: { fr: 'Contenu et publications', en: 'Content and publishing' } },
  { id: 'community', icon: 'ri-community-line', label: { fr: 'Vie communautaire', en: 'Community operations' } },
  { id: 'members', icon: 'ri-group-line', label: { fr: 'Membres et communications', en: 'Members and communications' } },
  { id: 'finance', icon: 'ri-bank-card-line', label: { fr: 'Finances et commerce', en: 'Finance and commerce' } },
  { id: 'governance', icon: 'ri-shield-check-line', label: { fr: 'Gouvernance et sécurité', en: 'Governance and security' } },
];

export const helpArticles: HelpArticle[] = [
  {
    id: 'dashboard', category: 'start', icon: 'ri-dashboard-line', path: '/admin/dashboard', permission: 'dashboard.view',
    title: { fr: 'Tableau de bord', en: 'Dashboard' },
    summary: { fr: 'Votre vue opérationnelle des demandes, activités, membres et tâches prioritaires.', en: 'Your operational overview of requests, activities, members, and priority work.' },
    steps: {
      fr: ['Consultez les indicateurs pour repérer les éléments à traiter.', 'Utilisez Actions rapides pour démarrer une tâche fréquente.', 'Ouvrez une ligne ou Tout voir pour accéder au registre complet.'],
      en: ['Review the indicators to identify items requiring attention.', 'Use Quick actions to start a frequent task.', 'Open a row or View all to access the full register.'],
    },
    tips: { fr: ['Les données sont actualisées depuis les registres correspondants.'], en: ['Figures are refreshed from their corresponding registers.'] },
    keywords: { fr: ['accueil', 'indicateurs', 'actions rapides', 'priorités'], en: ['home', 'metrics', 'quick actions', 'priorities'] },
  },
  {
    id: 'events', category: 'content', icon: 'ri-calendar-event-line', path: '/admin/events', permission: 'events.manage',
    title: { fr: 'Événements et billetterie', en: 'Events and ticketing' },
    summary: { fr: 'Créez des activités bilingues, gérez les intervenants, inscriptions, billets, présences et communications.', en: 'Create bilingual activities and manage speakers, registrations, tickets, attendance, and communications.' },
    steps: {
      fr: ['Créez l’événement et complétez les contenus français et anglais.', 'Définissez dates, lieu, capacité, intervenants, prix et période d’inscription.', 'Publiez, puis suivez les inscriptions, la liste d’attente et les présences depuis la fiche.'],
      en: ['Create the event and complete its French and English content.', 'Set dates, location, capacity, speakers, price, and registration period.', 'Publish, then track registrations, the waitlist, and attendance from the event record.'],
    },
    tips: { fr: ['Vérifiez toujours le lien de réunion et l’aperçu public avant publication.', 'Utilisez les catégories pour garder les filtres publics cohérents.'], en: ['Always verify the meeting link and public preview before publishing.', 'Use categories to keep public filters consistent.'] },
    keywords: { fr: ['événement', 'billet', 'qr', 'présence', 'liste attente', 'intervenant', 'calendrier'], en: ['event', 'ticket', 'qr', 'attendance', 'waitlist', 'speaker', 'calendar'] },
  },
  {
    id: 'announcements', category: 'content', icon: 'ri-article-line', path: '/admin/news', permission: 'content.manage',
    title: { fr: 'Annonces et actualités', en: 'Announcements and news' },
    summary: { fr: 'Rédigez, illustrez et publiez les nouvelles visibles dans l’espace Actualités.', en: 'Write, illustrate, and publish stories shown in the News area.' },
    steps: {
      fr: ['Créez une annonce et choisissez une catégorie.', 'Rédigez les versions française et anglaise avec une image adaptée.', 'Enregistrez en brouillon, vérifiez l’aperçu, puis publiez.'],
      en: ['Create an announcement and select a category.', 'Write French and English versions and add a suitable image.', 'Save as a draft, review the preview, then publish.'],
    },
    tips: { fr: ['Un titre court et un résumé précis améliorent la lisibilité mobile.'], en: ['A short title and precise summary improve mobile readability.'] },
    keywords: { fr: ['actualité', 'annonce', 'article', 'brouillon', 'publication'], en: ['news', 'announcement', 'article', 'draft', 'publish'] },
  },
  {
    id: 'documents', category: 'content', icon: 'ri-file-text-line', path: '/admin/documents', permission: 'content.manage',
    title: { fr: 'Documents', en: 'Documents' },
    summary: { fr: 'Centralisez les formulaires, rapports et ressources téléchargeables du HCBE.', en: 'Centralize HCBE forms, reports, and downloadable resources.' },
    steps: {
      fr: ['Ajoutez le fichier, son titre bilingue et sa catégorie.', 'Choisissez sa visibilité et son ordre d’affichage.', 'Publiez, puis testez le téléchargement depuis le site public.'],
      en: ['Add the file, its bilingual title, and category.', 'Choose its visibility and display order.', 'Publish, then test the download from the public website.'],
    },
    tips: { fr: ['Utilisez des noms de fichiers explicites et retirez les données personnelles inutiles.'], en: ['Use descriptive file names and remove unnecessary personal data.'] },
    keywords: { fr: ['fichier', 'pdf', 'rapport', 'formulaire', 'téléchargement'], en: ['file', 'pdf', 'report', 'form', 'download'] },
  },
  {
    id: 'site-content', category: 'content', icon: 'ri-layout-4-line', path: '/admin/site-content', permission: 'content.manage',
    title: { fr: 'Contenu du site (CMS)', en: 'Website content (CMS)' },
    summary: { fr: 'Modifiez les textes bilingues du site public sans changer le code.', en: 'Edit bilingual public-site text without changing code.' },
    steps: {
      fr: ['Filtrez par page ou recherchez le texte à modifier.', 'Mettez à jour les deux langues en conservant les variables comme {{count}}.', 'Enregistrez puis ouvrez la page publique pour valider le résultat.'],
      en: ['Filter by page or search for the text to edit.', 'Update both languages while preserving variables such as {{count}}.', 'Save, then open the public page to validate the result.'],
    },
    tips: { fr: ['Ne modifiez jamais le contenu entre doubles accolades : il est remplacé automatiquement.'], en: ['Never edit content inside double braces: it is replaced automatically.'] },
    keywords: { fr: ['cms', 'texte', 'traduction', 'variable', 'page publique'], en: ['cms', 'text', 'translation', 'variable', 'public page'] },
  },
  {
    id: 'partners', category: 'content', icon: 'ri-shake-hands-line', path: '/admin/partners', permission: 'content.manage',
    title: { fr: 'Partenaires du site', en: 'Website partners' },
    summary: { fr: 'Gérez les logos, liens et l’ordre des partenaires présentés publiquement.', en: 'Manage the logos, links, and order of publicly featured partners.' },
    steps: {
      fr: ['Ajoutez le nom, le logo et le site Web du partenaire.', 'Choisissez s’il apparaît dans le bandeau.', 'Réorganisez les partenaires et vérifiez l’aperçu.'],
      en: ['Add the partner name, logo, and website.', 'Choose whether it appears in the marquee.', 'Reorder partners and check the preview.'],
    },
    tips: { fr: ['Privilégiez un logo transparent, net et bien recadré.'], en: ['Prefer a crisp, well-cropped logo with a transparent background.'] },
    keywords: { fr: ['partenaire', 'logo', 'bandeau', 'commanditaire'], en: ['partner', 'logo', 'marquee', 'sponsor'] },
  },
  {
    id: 'team', category: 'content', icon: 'ri-team-line', path: '/admin/team-members', permission: 'content.manage',
    title: { fr: 'Équipe et représentants', en: 'Team and representatives' },
    summary: { fr: 'Tenez à jour les profils, fonctions, zones, photos et coordonnées publiques des représentants.', en: 'Maintain public representative profiles, roles, zones, photos, and contact details.' },
    steps: {
      fr: ['Ajoutez le nom, la fonction et la zone de responsabilité.', 'Importez une photo nette et complétez les informations bilingues.', 'Définissez l’ordre d’affichage et vérifiez la fiche publique.'],
      en: ['Add the name, role, and area of responsibility.', 'Upload a clear photo and complete bilingual information.', 'Set the display order and review the public profile.'],
    },
    tips: { fr: ['Obtenez l’accord de la personne avant de publier sa photo ou ses coordonnées.'], en: ['Obtain the person’s consent before publishing their photo or contact details.'] },
    keywords: { fr: ['équipe', 'représentant', 'délégué', 'suppléant', 'photo'], en: ['team', 'representative', 'delegate', 'deputy', 'photo'] },
  },
  {
    id: 'associations', category: 'community', icon: 'ri-building-line', path: '/admin/associations', permission: 'community.manage',
    title: { fr: 'Associations et comités', en: 'Associations and committees' },
    summary: { fr: 'Administrez les organisations, responsables, membres, documents, calendrier et accès privés.', en: 'Administer organizations, leaders, members, documents, calendars, and private access.' },
    steps: {
      fr: ['Créez la fiche bilingue et désignez les responsables.', 'Ajoutez les membres et configurez les permissions de l’organisation.', 'Publiez les coordonnées utiles et alimentez documents et calendrier.'],
      en: ['Create the bilingual record and designate its leaders.', 'Add members and configure organization permissions.', 'Publish useful contact information and maintain documents and calendars.'],
    },
    tips: { fr: ['N’accordez que les permissions nécessaires à chaque responsable.'], en: ['Grant each leader only the permissions they need.'] },
    keywords: { fr: ['association', 'comité', 'responsable', 'portail', 'permission'], en: ['association', 'committee', 'leader', 'portal', 'permission'] },
  },
  {
    id: 'association-requests', category: 'community', icon: 'ri-building-2-line', path: '/admin/association-requests', permission: 'community.manage',
    title: { fr: 'Demandes d’association', en: 'Association requests' },
    summary: { fr: 'Examinez les demandes d’adhésion ou de prise en charge d’une organisation.', en: 'Review requests to join or manage an organization.' },
    steps: {
      fr: ['Vérifiez l’identité et le motif du demandeur.', 'Confirmez l’organisation et le rôle demandé.', 'Approuvez ou refusez en documentant la décision.'],
      en: ['Verify the requester’s identity and reason.', 'Confirm the requested organization and role.', 'Approve or decline while documenting the decision.'],
    },
    tips: { fr: ['Validez les responsabilités avec l’organisation avant d’accorder un accès.'], en: ['Validate responsibilities with the organization before granting access.'] },
    keywords: { fr: ['demande', 'association', 'validation', 'accès'], en: ['request', 'association', 'approval', 'access'] },
  },
  {
    id: 'projects', category: 'community', icon: 'ri-hammer-line', path: '/admin/projects', permission: 'community.manage',
    title: { fr: 'Projets communautaires', en: 'Community projects' },
    summary: { fr: 'Publiez les initiatives, objectifs, échéanciers et façons de participer.', en: 'Publish initiatives, goals, timelines, and ways to participate.' },
    steps: {
      fr: ['Créez le projet et précisez son état, ses dates et son porteur.', 'Expliquez clairement l’impact attendu et les besoins.', 'Publiez et mettez à jour la progression régulièrement.'],
      en: ['Create the project and specify its status, dates, and owner.', 'Clearly explain the expected impact and needs.', 'Publish and update progress regularly.'],
    },
    tips: { fr: ['Fermez ou archivez les projets terminés pour garder le répertoire actuel.'], en: ['Close or archive completed projects to keep the directory current.'] },
    keywords: { fr: ['projet', 'initiative', 'participation', 'impact'], en: ['project', 'initiative', 'participation', 'impact'] },
  },
  {
    id: 'opportunities', category: 'community', icon: 'ri-briefcase-4-line', path: '/admin/opportunities', permission: 'community.manage',
    title: { fr: 'Occasions et bénévolat', en: 'Opportunities and volunteering' },
    summary: { fr: 'Gérez emplois, bénévolat, formations, candidatures, heures et attestations.', en: 'Manage jobs, volunteering, training, applications, hours, and certificates.' },
    steps: {
      fr: ['Publiez l’occasion avec les compétences, la région et la disponibilité recherchées.', 'Examinez les candidatures et leurs documents.', 'Suivez la participation, les heures et émettez les attestations.'],
      en: ['Publish the opportunity with required skills, region, and availability.', 'Review applications and their documents.', 'Track participation and hours, then issue certificates.'],
    },
    tips: { fr: ['Ne téléchargez les pièces jointes que si elles sont nécessaires au traitement.'], en: ['Download attachments only when needed for processing.'] },
    keywords: { fr: ['emploi', 'bénévolat', 'formation', 'candidature', 'attestation'], en: ['job', 'volunteer', 'training', 'application', 'certificate'] },
  },
  {
    id: 'grants', category: 'community', icon: 'ri-hand-coin-line', path: '/admin/grants', permission: 'community.manage',
    title: { fr: 'Bourses et subventions', en: 'Grants and subsidies' },
    summary: { fr: 'Publiez les programmes de financement et accompagnez les demandes.', en: 'Publish funding programs and support applications.' },
    steps: {
      fr: ['Ajoutez les critères, montants, dates et documents requis.', 'Publiez la fiche bilingue et vérifiez les liens externes.', 'Mettez à jour l’état lorsque la période est terminée.'],
      en: ['Add eligibility criteria, amounts, dates, and required documents.', 'Publish the bilingual record and verify external links.', 'Update the status when the application period ends.'],
    },
    tips: { fr: ['Indiquez un contact et une date limite non ambiguë.'], en: ['Provide a contact and an unambiguous deadline.'] },
    keywords: { fr: ['bourse', 'subvention', 'financement', 'éligibilité'], en: ['grant', 'subsidy', 'funding', 'eligibility'] },
  },
  {
    id: 'consultations', category: 'community', icon: 'ri-chat-poll-line', path: '/admin/consultations', permission: 'community.manage',
    title: { fr: 'Consultations et votes', en: 'Consultations and voting' },
    summary: { fr: 'Organisez propositions, commentaires et votes avec admissibilité, quorum et piste d’audit.', en: 'Run proposals, comments, and votes with eligibility, quorum, and an audit trail.' },
    steps: {
      fr: ['Définissez la question, les choix, la période et les personnes admissibles.', 'Choisissez un vote anonyme ou nominatif et le quorum.', 'Publiez, surveillez la participation puis clôturez et diffusez les résultats.'],
      en: ['Define the question, choices, period, and eligible participants.', 'Choose anonymous or named voting and set quorum.', 'Publish, monitor participation, then close and release results.'],
    },
    tips: { fr: ['Les règles ne devraient plus changer après le premier vote.'], en: ['Rules should not change after the first vote is cast.'] },
    keywords: { fr: ['sondage', 'vote', 'quorum', 'proposition', 'audit'], en: ['survey', 'vote', 'quorum', 'proposal', 'audit'] },
  },
  {
    id: 'members', category: 'members', icon: 'ri-user-line', path: '/admin/members', permission: 'members.manage',
    title: { fr: 'Membres', en: 'Members' },
    summary: { fr: 'Consultez les profils, statuts d’adhésion, préférences et activités des membres.', en: 'Review member profiles, membership status, preferences, and activity.' },
    steps: {
      fr: ['Recherchez par nom, courriel, région ou statut.', 'Ouvrez la fiche pour vérifier les informations et l’historique.', 'Modifiez seulement les données nécessaires ou utilisez l’action de promotion admin.'],
      en: ['Search by name, email, region, or status.', 'Open the record to review information and history.', 'Edit only necessary data or use the admin promotion action.'],
    },
    tips: { fr: ['Respectez le consentement et le principe d’accès minimal prévu par la Loi 25.'], en: ['Respect consent and Law 25’s least-access principle.'] },
    keywords: { fr: ['membre', 'profil', 'adhésion', 'statut', 'promotion admin'], en: ['member', 'profile', 'membership', 'status', 'promote admin'] },
  },
  {
    id: 'applications', category: 'members', icon: 'ri-user-add-line', path: '/admin/membership-applications', permission: 'members.manage',
    title: { fr: 'Demandes d’adhésion', en: 'Membership applications' },
    summary: { fr: 'Validez les nouvelles adhésions et conservez une décision traçable.', en: 'Review new memberships and keep decisions traceable.' },
    steps: {
      fr: ['Vérifiez les renseignements et le consentement transmis.', 'Approuvez, refusez ou demandez un complément.', 'Après approbation, confirmez que le statut et la durée d’adhésion sont corrects.'],
      en: ['Review submitted information and consent.', 'Approve, decline, or request additional information.', 'After approval, confirm the membership status and term are correct.'],
    },
    tips: { fr: ['Évitez de copier les données personnelles dans des outils externes.'], en: ['Avoid copying personal information into external tools.'] },
    keywords: { fr: ['adhésion', 'candidature', 'approbation', 'consentement'], en: ['membership', 'application', 'approval', 'consent'] },
  },
  {
    id: 'communications', category: 'members', icon: 'ri-mail-send-line', path: '/admin/newsletter', permission: 'communications.manage',
    title: { fr: 'Communications et infolettres', en: 'Communications and newsletters' },
    summary: { fr: 'Segmentez les destinataires et préparez des campagnes bilingues programmées.', en: 'Segment recipients and prepare scheduled bilingual campaigns.' },
    steps: {
      fr: ['Choisissez le segment selon consentement, région ou profil.', 'Rédigez et prévisualisez les deux langues.', 'Programmez ou envoyez, puis consultez livraison, ouverture et désabonnement.'],
      en: ['Choose the segment by consent, region, or profile.', 'Write and preview both languages.', 'Schedule or send, then review delivery, opens, and unsubscribes.'],
    },
    tips: { fr: ['Faites toujours un envoi test et n’incluez que les personnes consentantes.'], en: ['Always send a test and include only consenting recipients.'] },
    keywords: { fr: ['infolettre', 'campagne', 'segment', 'brevo', 'désabonnement'], en: ['newsletter', 'campaign', 'segment', 'brevo', 'unsubscribe'] },
  },
  {
    id: 'support', category: 'members', icon: 'ri-customer-service-2-line', path: '/admin/service-cases', permission: 'service-cases.manage',
    title: { fr: 'Demandes de services', en: 'Service requests' },
    summary: { fr: 'Assignez, priorisez et résolvez les demandes reçues des membres.', en: 'Assign, prioritize, and resolve requests received from members.' },
    steps: {
      fr: ['Ouvrez une demande et confirmez sa catégorie et sa priorité.', 'Assignez-la au comité ou responsable approprié.', 'Documentez les échanges, puis résolvez et fermez le dossier.'],
      en: ['Open a request and confirm its category and priority.', 'Assign it to the appropriate committee or owner.', 'Document communications, then resolve and close the case.'],
    },
    tips: { fr: ['N’inscrivez pas de renseignements sensibles dans un commentaire non nécessaire.'], en: ['Do not include sensitive information in an unnecessary comment.'] },
    keywords: { fr: ['service', 'dossier', 'assignation', 'comité', 'résolution'], en: ['service', 'case', 'assignment', 'committee', 'resolution'] },
  },
  {
    id: 'mentorship', category: 'members', icon: 'ri-user-heart-line', path: '/admin/mentorship', permission: 'community.manage',
    title: { fr: 'Mentorat et échanges', en: 'Mentorship and exchanges' },
    summary: { fr: 'Supervisez les candidatures, jumelages et signalements entre membres.', en: 'Oversee applications, matches, and reports between members.' },
    steps: {
      fr: ['Vérifiez les profils et objectifs des participants.', 'Créez un jumelage compatible et informez les personnes.', 'Suivez le jumelage et traitez rapidement tout signalement.'],
      en: ['Review participant profiles and goals.', 'Create a compatible match and notify participants.', 'Follow the match and address reports promptly.'],
    },
    tips: { fr: ['Consultez aussi Modération des échanges pour les conversations signalées.'], en: ['Also review Message moderation for reported conversations.'] },
    keywords: { fr: ['mentorat', 'jumelage', 'message', 'signalement', 'modération'], en: ['mentorship', 'match', 'message', 'report', 'moderation'] },
  },
  {
    id: 'submissions', category: 'members', icon: 'ri-inbox-archive-line', path: '/admin/submissions', permission: 'community.manage',
    title: { fr: 'Messages et demandes reçus', en: 'Received messages and requests' },
    summary: { fr: 'Centralisez les formulaires publics, demandes générales et propositions envoyées au HCBE.', en: 'Centralize public forms, general requests, and proposals sent to HCBE.' },
    steps: {
      fr: ['Filtrez les nouvelles soumissions et vérifiez leur sujet.', 'Acheminez la demande au responsable approprié.', 'Documentez le suivi et marquez la soumission comme traitée.'],
      en: ['Filter new submissions and review their subject.', 'Route the request to the appropriate owner.', 'Document follow-up and mark the submission as handled.'],
    },
    tips: { fr: ['Transformez en dossier de service toute demande qui nécessite un suivi prolongé.'], en: ['Turn any request requiring extended follow-up into a service case.'] },
    keywords: { fr: ['contact', 'message', 'formulaire', 'soumission', 'demande'], en: ['contact', 'message', 'form', 'submission', 'request'] },
  },
  {
    id: 'finance', category: 'finance', icon: 'ri-secure-payment-line', path: '/admin/finance', permission: 'finance.manage',
    title: { fr: 'Finances, paiements et reçus', en: 'Finance, payments, and receipts' },
    summary: { fr: 'Suivez cotisations, contributions, remboursements, rapprochements et reçus PDF.', en: 'Track dues, contributions, refunds, reconciliation, and PDF receipts.' },
    steps: {
      fr: ['Filtrez le registre par période, type ou statut.', 'Ouvrez une transaction pour vérifier le paiement et son reçu.', 'Effectuez les remboursements avec justification et rapprochez les écarts.'],
      en: ['Filter the ledger by period, type, or status.', 'Open a transaction to verify the payment and its receipt.', 'Issue justified refunds and reconcile discrepancies.'],
    },
    tips: { fr: ['Un remboursement est une action sensible : vérifiez montant et destinataire avant confirmation.'], en: ['A refund is sensitive: verify the amount and recipient before confirming.'] },
    keywords: { fr: ['paiement', 'stripe', 'reçu pdf', 'remboursement', 'rapprochement'], en: ['payment', 'stripe', 'pdf receipt', 'refund', 'reconciliation'] },
  },
  {
    id: 'marketplace', category: 'finance', icon: 'ri-store-2-line', path: '/admin/marketplace', permission: 'finance.manage',
    title: { fr: 'Organisateurs et publicité', en: 'Organizers and advertising' },
    summary: { fr: 'Validez les organisateurs, paiements Stripe Connect, publicités et commandites.', en: 'Review organizers, Stripe Connect payments, advertisements, and sponsorships.' },
    steps: {
      fr: ['Vérifiez l’identité et le statut du compte organisateur.', 'Contrôlez offres, placements, dates et visuels publicitaires.', 'Approuvez seulement après conformité du contenu et du paiement.'],
      en: ['Verify the organizer identity and account status.', 'Review offers, placements, dates, and advertising visuals.', 'Approve only after content and payment are compliant.'],
    },
    tips: { fr: ['Les fonctionnalités Stripe en production dépendent de l’activation du compte HCBE.'], en: ['Production Stripe features depend on activation of the HCBE account.'] },
    keywords: { fr: ['organisateur', 'billetterie', 'stripe connect', 'publicité', 'commandite'], en: ['organizer', 'ticketing', 'stripe connect', 'advertising', 'sponsorship'] },
  },
  {
    id: 'programs', category: 'finance', icon: 'ri-compass-discover-line', path: '/admin/community-programs', permission: 'community.manage',
    title: { fr: 'Programmes communautaires', en: 'Community programs' },
    summary: { fr: 'Pilotez entreprises, rendez-vous, familles, avantages partenaires et financement.', en: 'Operate business, appointment, family, partner-benefit, and funding programs.' },
    steps: {
      fr: ['Choisissez le programme concerné et consultez les demandes.', 'Validez les critères et assignez un responsable si nécessaire.', 'Mettez à jour le statut et informez le membre de la suite.'],
      en: ['Select the relevant program and review requests.', 'Validate criteria and assign an owner when needed.', 'Update the status and inform the member of next steps.'],
    },
    tips: { fr: ['Les accès et avantages doivent avoir une date de fin explicite.'], en: ['Access and benefits should have an explicit end date.'] },
    keywords: { fr: ['entreprise', 'rendez-vous', 'famille', 'avantage', 'bourse'], en: ['business', 'appointment', 'family', 'benefit', 'funding'] },
  },
  {
    id: 'users', category: 'governance', icon: 'ri-admin-line', path: '/admin/users', permission: 'users.manage',
    title: { fr: 'Utilisateurs admin et rôles', en: 'Admin users and roles' },
    summary: { fr: 'Invitez des administrateurs, attribuez des permissions et imposez la sécurité du premier accès.', en: 'Invite administrators, assign permissions, and secure their first access.' },
    steps: {
      fr: ['Créez le compte avec son courriel professionnel et le rôle minimal requis.', 'Générez un mot de passe temporaire et exigez son remplacement au premier accès.', 'Vérifiez l’envoi de bienvenue et révisez périodiquement les accès.'],
      en: ['Create the account with a professional email and the minimum required role.', 'Generate a temporary password and require replacement at first sign-in.', 'Verify the welcome email and periodically review access.'],
    },
    tips: { fr: ['Réservez le rôle super administrateur aux personnes qui gèrent les accès.'], en: ['Reserve the super administrator role for people who manage access.'] },
    keywords: { fr: ['admin', 'utilisateur', 'rôle', 'permission', 'mot de passe temporaire'], en: ['admin', 'user', 'role', 'permission', 'temporary password'] },
  },
  {
    id: 'security', category: 'governance', icon: 'ri-shield-keyhole-line', path: '/admin/security', permission: 'security.manage',
    title: { fr: 'Sécurité et Loi 25', en: 'Security and Law 25' },
    summary: { fr: 'Gérez MFA, sessions, appareils, incidents, consentements et demandes relatives aux données.', en: 'Manage MFA, sessions, devices, incidents, consent, and privacy requests.' },
    steps: {
      fr: ['Surveillez les alertes et les actions sensibles.', 'Fermez les sessions inconnues et ouvrez un incident au besoin.', 'Traitez les demandes d’accès, correction, retrait ou suppression dans les délais applicables.'],
      en: ['Monitor alerts and sensitive actions.', 'End unknown sessions and open an incident when needed.', 'Handle access, correction, withdrawal, or deletion requests within applicable timelines.'],
    },
    tips: { fr: ['Ne partagez jamais un code OTP, un mot de passe ou une clé API.'], en: ['Never share an OTP, password, or API key.'] },
    keywords: { fr: ['mfa', 'otp', 'session', 'incident', 'loi 25', 'suppression données'], en: ['mfa', 'otp', 'session', 'incident', 'law 25', 'data deletion'] },
  },
  {
    id: 'monitoring', category: 'governance', icon: 'ri-pulse-line', path: '/admin/monitoring', permission: 'analytics.view',
    title: { fr: 'Surveillance et santé du service', en: 'Monitoring and service health' },
    summary: { fr: 'Contrôlez la disponibilité, les erreurs, les sauvegardes et les signaux opérationnels.', en: 'Monitor availability, errors, backups, and operational signals.' },
    steps: {
      fr: ['Repérez les indicateurs dégradés ou indisponibles.', 'Consultez le détail et les journaux sans exposer de secrets.', 'Ouvrez un incident, appliquez la procédure puis documentez la résolution.'],
      en: ['Identify degraded or unavailable indicators.', 'Review details and logs without exposing secrets.', 'Open an incident, follow the procedure, then document the resolution.'],
    },
    tips: { fr: ['Une sauvegarde n’est fiable qu’après un test de restauration réussi.'], en: ['A backup is reliable only after a successful restore test.'] },
    keywords: { fr: ['monitoring', 'disponibilité', 'erreur', 'sauvegarde', 'incident'], en: ['monitoring', 'uptime', 'error', 'backup', 'incident'] },
  },
  {
    id: 'impact', category: 'governance', icon: 'ri-line-chart-line', path: '/admin/impact', permission: 'analytics.view',
    title: { fr: 'Impact et rapports', en: 'Impact and reporting' },
    summary: { fr: 'Mesurez les activités communautaires et produisez les rapports de suivi.', en: 'Measure community activities and produce operational reports.' },
    steps: {
      fr: ['Choisissez la période et les indicateurs à analyser.', 'Vérifiez les tendances et la provenance des données.', 'Exportez le rapport en évitant les données personnelles non nécessaires.'],
      en: ['Choose the period and indicators to analyze.', 'Review trends and data provenance.', 'Export the report while excluding unnecessary personal information.'],
    },
    tips: { fr: ['Interprétez les variations avec le contexte des activités de la période.'], en: ['Interpret changes alongside the context of activities during the period.'] },
    keywords: { fr: ['impact', 'statistique', 'rapport', 'export', 'annuel'], en: ['impact', 'statistics', 'report', 'export', 'annual'] },
  },
];

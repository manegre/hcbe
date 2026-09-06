export type AdminPermissionLocale = 'fr' | 'en';

export const adminPermissionLabels: Record<string, Record<AdminPermissionLocale, string>> = {
  'dashboard.view': { fr: 'Accès au tableau de bord', en: 'Dashboard access' },
  'content.manage': { fr: 'Gestion du contenu du site', en: 'Website content management' },
  'events.manage': { fr: 'Gestion des événements', en: 'Event management' },
  'members.manage': { fr: 'Gestion des membres', en: 'Member management' },
  'community.manage': { fr: 'Gestion des programmes communautaires', en: 'Community program management' },
  'communications.manage': { fr: 'Gestion des communications', en: 'Communication management' },
  'service-cases.manage': { fr: 'Traitement des demandes de service', en: 'Service request management' },
  'moderation.manage': { fr: 'Modération des échanges', en: 'Conversation moderation' },
  'analytics.view': { fr: 'Accès aux analyses et rapports', en: 'Analytics and reporting access' },
  'users.manage': { fr: 'Gestion des administrateurs', en: 'Administrator management' },
  'settings.manage': { fr: 'Gestion des paramètres techniques', en: 'Technical settings management' },
  'finance.manage': { fr: 'Gestion des finances', en: 'Finance management' },
  'security.manage': { fr: 'Gestion de la sécurité et des incidents', en: 'Security and incident management' },
  'privacy.manage': { fr: 'Supervision des contrôles Loi 25', en: 'Law 25 compliance oversight' },
};

export const getAdminPermissionLabel = (permission: string, locale: AdminPermissionLocale) =>
  adminPermissionLabels[permission]?.[locale] ?? permission;

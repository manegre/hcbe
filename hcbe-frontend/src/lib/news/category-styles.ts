export const NEWS_CATEGORIES = [
  'Communiqué Officiel',
  'Éducation',
  'Événement',
  'Service',
  'Solidarité',
  'Formation',
  'Annonce',
  'Partenariat',
] as const;

export type NewsCategory = (typeof NEWS_CATEGORIES)[number];

const NEWS_CATEGORY_LABEL_KEYS: Record<string, string> = {
  'Communiqué Officiel': 'public.news.categories.official.label',
  Éducation: 'public.news.categories.education.label',
  Événement: 'public.news.categories.event.label',
  Service: 'public.news.categories.service.label',
  Solidarité: 'public.news.categories.solidarity.label',
  Formation: 'public.news.categories.training.label',
  Annonce: 'public.news.categories.announcement.label',
  Partenariat: 'public.news.categories.partnership.label',
};

export const getNewsCategoryLabelKey = (category?: string): string | undefined => {
  if (!category) return undefined;
  return NEWS_CATEGORY_LABEL_KEYS[category];
};

const EVENT_TYPE_LABEL_KEYS: Record<string, string> = {
  workshop: 'admin.events.type.workshop',
  conference: 'admin.events.type.conference',
  networking: 'admin.events.type.networking',
  training: 'admin.events.type.training',
  social: 'admin.events.type.social',
  other: 'admin.events.type.other',
};

export const getEventTypeLabelKey = (type?: string): string | undefined => {
  if (!type) return undefined;
  return EVENT_TYPE_LABEL_KEYS[type.toLowerCase()];
};


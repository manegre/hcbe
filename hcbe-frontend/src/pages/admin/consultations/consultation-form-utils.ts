export const CONSULTATION_ICON_OPTIONS = [
  { value: 'ri-chat-poll-line', label: 'Sondage' },
  { value: 'ri-questionnaire-line', label: 'Questionnaire' },
  { value: 'ri-feedback-line', label: 'Feedback' },
  { value: 'ri-discuss-line', label: 'Discussion' },
  { value: 'ri-megaphone-line', label: 'Annonce' },
  { value: 'ri-survey-line', label: 'Enquête' },
];

export const CONSULTATION_LAYOUT_OPTIONS = [
  { value: 'featured', labelKey: 'admin.consultations.layoutFeatured' },
  { value: 'card', labelKey: 'admin.consultations.layoutCard' },
] as const;

export const CONSULTATION_ACCENT_OPTIONS = [
  { value: 'emerald', labelKey: 'admin.consultations.accentEmerald' },
  { value: 'amber', labelKey: 'admin.consultations.accentAmber' },
] as const;

export const GRANT_ICON_OPTIONS = [
  { value: 'ri-graduation-cap-line', label: 'Graduation cap' },
  { value: 'ri-briefcase-line', label: 'Briefcase' },
  { value: 'ri-community-line', label: 'Community' },
  { value: 'ri-rocket-line', label: 'Rocket' },
  { value: 'ri-microscope-line', label: 'Microscope' },
  { value: 'ri-map-pin-user-line', label: 'Mobility' },
  { value: 'ri-hand-heart-line', label: 'Hand heart' },
  { value: 'ri-funds-line', label: 'Funds' },
];

export const parseCriteriaText = (text: string): string[] =>
  text
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean);

export const formatCriteriaText = (criteria: string[]): string => criteria.join('\n');

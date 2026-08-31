/**
 * Returns the EN value when the active language is English and EN is non-empty;
 * otherwise returns the French (primary) value. Field-level silent fallback.
 */
export const localized = (
  french: string | null | undefined,
  english: string | null | undefined,
  language: string,
): string => {
  const fr = french?.trim() ?? '';
  const en = english?.trim() ?? '';
  const isEnglish = language.toLowerCase().startsWith('en');
  if (isEnglish && en) {
    return en;
  }
  return fr;
};

export const localizedOptional = (
  french: string | null | undefined,
  english: string | null | undefined,
  language: string,
): string | undefined => {
  const value = localized(french, english, language);
  return value || undefined;
};

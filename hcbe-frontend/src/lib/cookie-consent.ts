export const COOKIE_CONSENT_NAME = 'hcbe_cookie_consent';
export const COOKIE_CONSENT_VERSION = 1;
export const COOKIE_SETTINGS_EVENT = 'hcbe:open-cookie-settings';

export interface CookieConsentRecord {
  version: number;
  essential: true;
  acceptedAt: string;
}

export const readCookieConsent = (): CookieConsentRecord | null => {
  if (typeof document === 'undefined') return null;
  const value = document.cookie
    .split('; ')
    .find((entry) => entry.startsWith(`${COOKIE_CONSENT_NAME}=`))
    ?.slice(COOKIE_CONSENT_NAME.length + 1);
  if (!value) return null;

  try {
    const parsed = JSON.parse(decodeURIComponent(value)) as Partial<CookieConsentRecord>;
    return parsed.version === COOKIE_CONSENT_VERSION && parsed.essential === true && typeof parsed.acceptedAt === 'string'
      ? parsed as CookieConsentRecord
      : null;
  } catch {
    return null;
  }
};

export const saveEssentialCookieConsent = (): CookieConsentRecord => {
  const consent: CookieConsentRecord = {
    version: COOKIE_CONSENT_VERSION,
    essential: true,
    acceptedAt: new Date().toISOString(),
  };
  const secure = window.location.protocol === 'https:' ? '; Secure' : '';
  document.cookie = `${COOKIE_CONSENT_NAME}=${encodeURIComponent(JSON.stringify(consent))}; Path=/; Max-Age=15552000; SameSite=Lax${secure}`;
  return consent;
};

export const openCookieSettings = () => {
  window.dispatchEvent(new Event(COOKIE_SETTINGS_EVENT));
};

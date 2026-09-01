import { useEffect, useRef, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  COOKIE_SETTINGS_EVENT,
  readCookieConsent,
  saveEssentialCookieConsent,
} from '../../lib/cookie-consent';

const CookieConsent = () => {
  const { t } = useTranslation();
  const location = useLocation();
  const [visible, setVisible] = useState(() => !readCookieConsent());
  const [showDetails, setShowDetails] = useState(false);
  const detailsButtonRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const openSettings = () => {
      setVisible(true);
      setShowDetails(true);
      window.setTimeout(() => detailsButtonRef.current?.focus(), 0);
    };
    window.addEventListener(COOKIE_SETTINGS_EVENT, openSettings);
    return () => window.removeEventListener(COOKIE_SETTINGS_EVENT, openSettings);
  }, []);

  if (location.pathname.startsWith('/admin') || !visible) return null;

  const accept = () => {
    saveEssentialCookieConsent();
    setVisible(false);
    setShowDetails(false);
  };

  return (
    <aside
      className="fixed inset-x-0 bottom-0 z-[90] px-3 pb-3 sm:px-5 sm:pb-5"
      aria-label={t('public.cookies.ariaLabel')}
      aria-live="polite"
    >
      <div className="mx-auto max-w-[1100px] overflow-hidden rounded-[18px] border border-white/15 bg-green-deep text-white shadow-[0_24px_80px_rgba(0,24,11,.34)]">
        <div className="flex h-1" aria-hidden="true">
          <span className="w-1/3 bg-red" />
          <span className="w-1/3 bg-gold" />
          <span className="w-1/3 bg-green-dim" />
        </div>

        <div className="relative p-5 sm:p-6 lg:px-8">
          <div className="pointer-events-none absolute -right-10 -top-16 h-40 w-40 rounded-full border-[28px] border-white/[0.035]" aria-hidden="true" />
          <div className="relative grid items-center gap-5 lg:grid-cols-[minmax(0,1fr)_auto] lg:gap-8">
            <div className="flex items-start gap-4">
              <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-gold/35 bg-gold/10 text-xl text-gold" aria-hidden="true">
                <i className="ri-shield-check-line" />
              </span>
              <div>
                <p className="text-[10px] font-bold uppercase tracking-[0.2em] text-gold">{t('public.cookies.eyebrow')}</p>
                <h2 className="mt-1 font-display text-xl font-bold leading-tight text-white sm:text-2xl">{t('public.cookies.title')}</h2>
                <p className="mt-2 max-w-3xl text-sm leading-6 text-white/72">
                  {t('public.cookies.description')}{' '}
                  <Link to="/confidentialite#cookies" className="font-semibold text-white underline decoration-gold/70 underline-offset-4 hover:text-gold">
                    {t('public.cookies.privacyLink')}
                  </Link>
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-2 sm:flex-row lg:justify-end">
              <button
                ref={detailsButtonRef}
                type="button"
                aria-expanded={showDetails}
                aria-controls="cookie-consent-details"
                onClick={() => setShowDetails((current) => !current)}
                className="inline-flex min-h-11 items-center justify-center rounded-control border border-white/30 px-5 text-[11px] font-bold uppercase tracking-[0.09em] text-white transition-colors hover:border-white hover:bg-white/10 focus-visible:outline-white"
              >
                {t(showDetails ? 'public.cookies.hideDetails' : 'public.cookies.showDetails')}
              </button>
              <button
                type="button"
                onClick={accept}
                className="inline-flex min-h-11 items-center justify-center rounded-control border border-gold bg-gold px-5 text-[11px] font-bold uppercase tracking-[0.09em] text-green-deep transition-colors hover:bg-gold-dim focus-visible:outline-gold"
              >
                {t('public.cookies.accept')}
              </button>
            </div>
          </div>

          {showDetails && (
            <div id="cookie-consent-details" className="relative mt-5 grid gap-px overflow-hidden rounded-[12px] border border-white/12 bg-white/10 md:grid-cols-3">
              <ConsentDetail icon="ri-lock-2-line" title={t('public.cookies.essentialTitle')} body={t('public.cookies.essentialBody')} status={t('public.cookies.alwaysActive')} />
              <ConsentDetail icon="ri-palette-line" title={t('public.cookies.preferencesTitle')} body={t('public.cookies.preferencesBody')} status={t('public.cookies.localOnly')} />
              <ConsentDetail icon="ri-bar-chart-2-line" title={t('public.cookies.analyticsTitle')} body={t('public.cookies.analyticsBody')} status={t('public.cookies.inactive')} muted />
            </div>
          )}
        </div>
      </div>
    </aside>
  );
};

const ConsentDetail = ({ icon, title, body, status, muted = false }: { icon: string; title: string; body: string; status: string; muted?: boolean }) => (
  <div className={`bg-green-deep p-4 sm:p-5 ${muted ? 'opacity-75' : ''}`}>
    <div className="flex items-center justify-between gap-3">
      <span className="flex items-center gap-2 text-sm font-semibold text-white"><i className={`${icon} text-gold`} aria-hidden="true" />{title}</span>
      <span className="rounded-full border border-white/15 bg-white/[0.06] px-2.5 py-1 text-[9px] font-bold uppercase tracking-[0.12em] text-green-dim">{status}</span>
    </div>
    <p className="mt-3 text-xs leading-5 text-white/62">{body}</p>
  </div>
);

export default CookieConsent;

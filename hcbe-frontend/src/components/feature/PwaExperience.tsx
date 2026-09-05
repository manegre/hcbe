import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';

interface InstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

type MobilePlatform = 'ios' | 'android' | 'other';

const DISMISS_KEY = 'hcbe:pwa-install-dismissed-at';
const DISMISS_FOR_MS = 14 * 24 * 60 * 60 * 1000;

const isStandalone = () => window.matchMedia('(display-mode: standalone)').matches
  || Boolean((navigator as Navigator & { standalone?: boolean }).standalone);

const getPlatform = (): MobilePlatform => {
  const userAgent = navigator.userAgent.toLowerCase();
  const ipad = navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1;
  if (/iphone|ipad|ipod/.test(userAgent) || ipad) return 'ios';
  if (/android/.test(userAgent)) return 'android';
  return 'other';
};

export default function PwaExperience() {
  const { i18n } = useTranslation();
  const french = !i18n.language.startsWith('en');
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const [online, setOnline] = useState(() => navigator.onLine);
  const [installPrompt, setInstallPrompt] = useState<InstallPromptEvent | null>(null);
  const [updateReady, setUpdateReady] = useState(false);
  const [installed, setInstalled] = useState(isStandalone);
  const [showInstallNotice, setShowInstallNotice] = useState(false);
  const [showGuide, setShowGuide] = useState(false);
  const platform = useMemo(getPlatform, []);
  const mobile = platform !== 'other' || window.matchMedia('(max-width: 767px)').matches;

  useEffect(() => {
    const onOnline = () => setOnline(true);
    const onOffline = () => setOnline(false);
    const onInstall = (event: Event) => {
      event.preventDefault();
      setInstallPrompt(event as InstallPromptEvent);
      if (mobile && !isStandalone()) setShowInstallNotice(true);
    };
    const onInstalled = () => {
      setInstalled(true);
      setInstallPrompt(null);
      setShowInstallNotice(false);
      setShowGuide(false);
      localStorage.removeItem(DISMISS_KEY);
    };
    const onUpdate = () => setUpdateReady(true);
    const onOpenGuide = () => {
      if (!isStandalone()) {
        setShowInstallNotice(false);
        setShowGuide(true);
      }
    };

    window.addEventListener('online', onOnline);
    window.addEventListener('offline', onOffline);
    window.addEventListener('beforeinstallprompt', onInstall);
    window.addEventListener('appinstalled', onInstalled);
    window.addEventListener('hcbe:sw-update', onUpdate);
    window.addEventListener('hcbe:open-install-guide', onOpenGuide);

    let reminder: number | undefined;
    if (mobile && !isStandalone()) {
      const dismissedAt = Number(localStorage.getItem(DISMISS_KEY) || 0);
      if (!dismissedAt || Date.now() - dismissedAt >= DISMISS_FOR_MS) {
        reminder = window.setTimeout(() => setShowInstallNotice(true), 4500);
      }
    }

    return () => {
      window.clearTimeout(reminder);
      window.removeEventListener('online', onOnline);
      window.removeEventListener('offline', onOffline);
      window.removeEventListener('beforeinstallprompt', onInstall);
      window.removeEventListener('appinstalled', onInstalled);
      window.removeEventListener('hcbe:sw-update', onUpdate);
      window.removeEventListener('hcbe:open-install-guide', onOpenGuide);
    };
  }, [mobile]);

  useEffect(() => {
    if (!showGuide) return;
    closeButtonRef.current?.focus();
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setShowGuide(false);
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [showGuide]);

  const install = async () => {
    if (!installPrompt) {
      setShowInstallNotice(false);
      setShowGuide(true);
      return;
    }
    await installPrompt.prompt();
    const choice = await installPrompt.userChoice;
    if (choice.outcome === 'accepted') {
      setInstallPrompt(null);
      setShowInstallNotice(false);
    }
  };

  const dismissInstallNotice = () => {
    localStorage.setItem(DISMISS_KEY, String(Date.now()));
    setShowInstallNotice(false);
  };

  const refresh = async () => {
    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration?.waiting) {
      window.location.reload();
      return;
    }
    navigator.serviceWorker.addEventListener('controllerchange', () => window.location.reload(), { once: true });
    registration.waiting.postMessage({ type: 'SKIP_WAITING' });
  };

  const guideSteps = platform === 'ios'
    ? [
        { icon: 'ri-safari-line', fr: 'Ouvrez hcbe.ca dans Safari.', en: 'Open hcbe.ca in Safari.' },
        { icon: 'ri-share-forward-line', fr: 'Touchez le bouton Partager en bas de l’écran.', en: 'Tap the Share button at the bottom of the screen.' },
        { icon: 'ri-add-box-line', fr: 'Choisissez « Sur l’écran d’accueil », puis « Ajouter ».', en: 'Choose “Add to Home Screen”, then “Add”.' },
      ]
    : [
        { icon: 'ri-more-2-fill', fr: 'Ouvrez le menu du navigateur.', en: 'Open your browser menu.' },
        { icon: 'ri-add-box-line', fr: 'Choisissez « Installer l’application » ou « Ajouter à l’écran d’accueil ».', en: 'Choose “Install app” or “Add to Home screen”.' },
        { icon: 'ri-check-line', fr: 'Confirmez avec « Installer ».', en: 'Confirm by tapping “Install”.' },
      ];

  const showStatus = !online || updateReady;
  const showInstall = online && !updateReady && !installed && showInstallNotice;

  return <>
    {(showStatus || showInstall) && <aside aria-live="polite" className="fixed inset-x-3 bottom-3 z-[90] mx-auto max-w-[420px] overflow-hidden rounded-[24px] border border-white/15 bg-green-deep text-white shadow-[0_24px_75px_rgba(0,30,14,.38)] sm:bottom-5">
      {showInstall && <div className="h-1 bg-gradient-to-r from-gold via-[#ffe86f] to-red-link" aria-hidden="true" />}
      <div className="flex items-center gap-3 p-3.5 sm:p-4">
        {showInstall
          ? <img src="/hcbe-app-icon-192.png" alt="" className="h-12 w-12 shrink-0 rounded-[14px] border border-white/15 shadow-lg" />
          : <span className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-white/10 text-gold" aria-hidden="true"><i className={updateReady ? 'ri-refresh-line' : 'ri-wifi-off-line'} /></span>}
        <div className="min-w-0 flex-1">
          <strong className="block font-display text-[17px] leading-tight">
            {updateReady ? (french ? 'Mise à jour disponible' : 'Update available') : showInstall ? (french ? 'HCBE, toujours à portée de main' : 'Keep HCBE close at hand') : (french ? 'Vous êtes hors ligne' : 'You are offline')}
          </strong>
          <span className="mt-1 block text-[11px] leading-[1.45] text-white/70">
            {updateReady ? (french ? 'Rechargez pour utiliser la nouvelle version.' : 'Reload to use the latest version.') : showInstall ? (french ? 'Installez gratuitement l’application sur votre téléphone.' : 'Install the free app on your phone.') : (french ? 'Les pages déjà consultées restent disponibles.' : 'Pages you already visited remain available.')}
          </span>
        </div>
        {showInstall && <button type="button" onClick={dismissInstallNotice} aria-label={french ? 'Masquer le rappel' : 'Dismiss reminder'} className="absolute right-2 top-2 flex h-8 w-8 items-center justify-center rounded-full text-white/55 transition hover:bg-white/10 hover:text-white"><i className="ri-close-line" aria-hidden="true" /></button>}
      </div>
      {(showInstall || updateReady) && <div className="flex gap-2 border-t border-white/10 px-3.5 pb-3.5 pt-3 sm:px-4 sm:pb-4">
        {showInstall && <button type="button" onClick={() => setShowGuide(true)} className="min-h-11 flex-1 rounded-xl border border-white/20 px-3 text-[10px] font-bold uppercase tracking-[.1em] text-white transition hover:border-white/45 hover:bg-white/[.06]">{french ? 'Voir comment' : 'See how'}</button>}
        <button type="button" onClick={updateReady ? refresh : install} className="min-h-11 flex-1 rounded-xl bg-gold px-3 text-[10px] font-bold uppercase tracking-[.1em] text-green-deep transition hover:bg-[#ffe467]">{updateReady ? (french ? 'Actualiser' : 'Reload') : installPrompt ? (french ? 'Installer' : 'Install') : (french ? 'Instructions' : 'Instructions')}</button>
      </div>}
    </aside>}

    {showGuide && !installed && <div className="fixed inset-0 z-[120] flex items-end justify-center sm:items-center sm:p-6" role="dialog" aria-modal="true" aria-labelledby="pwa-guide-title">
      <button type="button" tabIndex={-1} aria-label={french ? 'Fermer' : 'Close'} onClick={() => setShowGuide(false)} className="absolute inset-0 bg-green-deep/70 backdrop-blur-[3px]" />
      <section className="relative max-h-[92dvh] w-full overflow-y-auto rounded-t-[30px] border border-line bg-background px-5 pb-[max(1.5rem,env(safe-area-inset-bottom))] pt-5 shadow-[0_-24px_80px_rgba(0,30,14,.35)] sm:max-w-[520px] sm:rounded-[30px] sm:p-7">
        <div className="mx-auto mb-5 h-1 w-12 rounded-full bg-line sm:hidden" aria-hidden="true" />
        <div className="flex items-start gap-4">
          <img src="/hcbe-app-icon-192.png" alt="" className="h-16 w-16 shrink-0 rounded-[18px] shadow-[0_12px_30px_rgba(0,59,27,.16)]" />
          <div className="min-w-0 flex-1 pt-1"><p className="text-[9px] font-bold uppercase tracking-[.2em] text-red-link">HCBE Canada</p><h2 id="pwa-guide-title" className="mt-1 font-display text-2xl font-bold leading-tight text-green-deep">{french ? 'Installez l’application' : 'Install the app'}</h2><p className="mt-1 text-xs leading-5 text-ink-variant">{french ? 'Un accès rapide, sécurisé et sans téléchargement depuis une boutique.' : 'Fast, secure access without downloading from an app store.'}</p></div>
          <button ref={closeButtonRef} type="button" onClick={() => setShowGuide(false)} aria-label={french ? 'Fermer les instructions' : 'Close instructions'} className="flex h-11 w-11 shrink-0 items-center justify-center rounded-full border border-line bg-surface text-ink transition hover:border-green hover:text-green"><i className="ri-close-line text-xl" aria-hidden="true" /></button>
        </div>

        <div className="mt-6 rounded-[22px] border border-green/10 bg-surface p-4 sm:p-5">
          <div className="mb-4 flex items-center justify-between gap-3"><span className="text-[10px] font-bold uppercase tracking-[.15em] text-green">{platform === 'ios' ? 'iPhone · iPad' : platform === 'android' ? 'Android' : (french ? 'Votre téléphone' : 'Your phone')}</span><span className="rounded-full bg-gold/20 px-3 py-1 text-[9px] font-bold uppercase tracking-[.1em] text-gold-ink">{french ? '≈ 30 secondes' : '≈ 30 seconds'}</span></div>
          <ol className="space-y-3">
            {guideSteps.map((step, index) => <li key={step.icon} className="grid grid-cols-[38px_1fr] items-center gap-3 rounded-2xl border border-line/65 bg-background p-3"><span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green text-base text-gold"><i className={step.icon} aria-hidden="true" /></span><div><span className="text-[8px] font-bold uppercase tracking-[.14em] text-ink-variant">{french ? 'Étape' : 'Step'} {index + 1}</span><p className="mt-0.5 text-sm font-medium leading-5 text-ink">{french ? step.fr : step.en}</p></div></li>)}
          </ol>
        </div>

        {installPrompt && <button type="button" onClick={install} className="mt-4 flex min-h-12 w-full items-center justify-center gap-2 rounded-xl bg-gold px-5 text-[10px] font-bold uppercase tracking-[.12em] text-green-deep shadow-[0_12px_30px_rgba(252,209,22,.22)] transition hover:-translate-y-0.5 hover:bg-[#ffe467]"><i className="ri-download-cloud-2-line text-lg" aria-hidden="true" />{french ? 'Installer maintenant' : 'Install now'}</button>}
        <p className="mt-4 text-center text-[10px] leading-4 text-ink-variant"><i className="ri-shield-check-line mr-1 text-green" aria-hidden="true" />{french ? 'L’installation est gratuite et utilise très peu d’espace.' : 'Installation is free and uses very little storage.'}</p>
      </section>
    </div>}
  </>;
}

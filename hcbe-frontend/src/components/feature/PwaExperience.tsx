import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';

interface InstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

export default function PwaExperience() {
  const { i18n } = useTranslation();
  const french = !i18n.language.startsWith('en');
  const [online, setOnline] = useState(() => navigator.onLine);
  const [installPrompt, setInstallPrompt] = useState<InstallPromptEvent | null>(null);
  const [updateReady, setUpdateReady] = useState(false);

  useEffect(() => {
    const onOnline = () => setOnline(true);
    const onOffline = () => setOnline(false);
    const onInstall = (event: Event) => { event.preventDefault(); setInstallPrompt(event as InstallPromptEvent); };
    const onInstalled = () => setInstallPrompt(null);
    const onUpdate = () => setUpdateReady(true);
    window.addEventListener('online', onOnline); window.addEventListener('offline', onOffline);
    window.addEventListener('beforeinstallprompt', onInstall); window.addEventListener('appinstalled', onInstalled);
    window.addEventListener('hcbe:sw-update', onUpdate);
    return () => { window.removeEventListener('online', onOnline); window.removeEventListener('offline', onOffline); window.removeEventListener('beforeinstallprompt', onInstall); window.removeEventListener('appinstalled', onInstalled); window.removeEventListener('hcbe:sw-update', onUpdate); };
  }, []);

  const install = async () => {
    if (!installPrompt) return;
    await installPrompt.prompt();
    if ((await installPrompt.userChoice).outcome === 'accepted') setInstallPrompt(null);
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

  if (online && !installPrompt && !updateReady) return null;
  return <aside aria-live="polite" className="fixed inset-x-3 bottom-3 z-[90] mx-auto flex max-w-xl items-center gap-3 rounded-2xl border border-white/15 bg-green-deep p-3 text-white shadow-[0_20px_60px_rgba(0,30,14,.28)] sm:bottom-5 sm:p-4">
    <span className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${online ? 'bg-gold text-green-deep' : 'bg-white/10 text-gold'}`} aria-hidden="true"><i className={updateReady ? 'ri-refresh-line' : installPrompt ? 'ri-download-cloud-2-line' : 'ri-wifi-off-line'} /></span>
    <div className="min-w-0 flex-1"><strong className="block text-sm">{updateReady ? (french ? 'Mise à jour disponible' : 'Update available') : installPrompt ? (french ? 'Installer HCBE Canada' : 'Install HCBE Canada') : (french ? 'Vous êtes hors ligne' : 'You are offline')}</strong><span className="mt-0.5 block text-xs leading-5 text-white/70">{updateReady ? (french ? 'Rechargez pour utiliser la nouvelle version.' : 'Reload to use the latest version.') : installPrompt ? (french ? 'Ajoutez l’application à votre écran d’accueil.' : 'Add the app to your home screen.') : (french ? 'Les pages déjà consultées restent disponibles.' : 'Pages you already visited remain available.')}</span></div>
    {(installPrompt || updateReady) && <button type="button" onClick={updateReady ? refresh : install} className="min-h-11 shrink-0 rounded-xl bg-gold px-4 text-[10px] font-bold uppercase tracking-[.1em] text-green-deep">{updateReady ? (french ? 'Actualiser' : 'Reload') : (french ? 'Installer' : 'Install')}</button>}
  </aside>;
}

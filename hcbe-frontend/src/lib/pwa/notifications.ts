const preferenceKey = 'hcbe_push_notifications';

export const setAppNotificationsEnabled = (enabled: boolean) => {
  localStorage.setItem(preferenceKey, enabled ? 'true' : 'false');
};

export const notifyFromApp = async (title: string, body: string, url = '/espace-membre') => {
  if (localStorage.getItem(preferenceKey) !== 'true' || !('Notification' in window) || Notification.permission !== 'granted') return;

  if ('serviceWorker' in navigator) {
    const registration = await navigator.serviceWorker.getRegistration().catch(() => undefined);
    const worker = registration?.active;
    if (worker) {
      worker.postMessage({ type: 'HCBE_NOTIFICATION', title, body, url });
      return;
    }
  }

  new Notification(title, { body, icon: '/hcbe-app-icon.svg' });
};

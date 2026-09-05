import { pushApi } from '../api/push';

const preferenceKey = 'hcbe_push_notifications';

const toUint8Array = (value: string) => {
  const padding = '='.repeat((4 - (value.length % 4)) % 4);
  const decoded = atob((value + padding).replace(/-/g, '+').replace(/_/g, '/'));
  return Uint8Array.from(decoded, (character) => character.charCodeAt(0));
};

const serializeSubscription = (subscription: PushSubscription) => {
  const json = subscription.toJSON();
  if (!json.endpoint || !json.keys?.p256dh || !json.keys?.auth) throw new Error('Invalid push subscription');
  return { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth };
};

export const setAppNotificationsEnabled = (enabled: boolean) => {
  localStorage.setItem(preferenceKey, enabled ? 'true' : 'false');
};

export const supportsPushNotifications = () =>
  'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;

export const enablePushNotifications = async () => {
  if (!supportsPushNotifications()) throw new Error('Push notifications are not supported');
  const configuration = await pushApi.configuration();
  if (!configuration.data?.enabled || !configuration.data.publicKey) throw new Error('Push notifications are not configured');
  const permission = await Notification.requestPermission();
  if (permission !== 'granted') throw new Error('Notification permission was not granted');
  const registration = await navigator.serviceWorker.ready;
  let subscription = await registration.pushManager.getSubscription();
  subscription ??= await registration.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: toUint8Array(configuration.data.publicKey) });
  await pushApi.subscribe(serializeSubscription(subscription));
  setAppNotificationsEnabled(true);
  return subscription;
};

export const disablePushNotifications = async () => {
  setAppNotificationsEnabled(false);
  if (!supportsPushNotifications()) return;
  const registration = await navigator.serviceWorker.getRegistration();
  const subscription = await registration?.pushManager.getSubscription();
  if (!subscription) return;
  await pushApi.unsubscribe(subscription.endpoint).catch(() => undefined);
  await subscription.unsubscribe();
};

export const sendTestPushNotification = (language: string) => pushApi.test(language);

export const notifyFromApp = async (title: string, body: string, url = '/espace-membre') => {
  if (localStorage.getItem(preferenceKey) !== 'true' || !('Notification' in window) || Notification.permission !== 'granted') return;
  if ('serviceWorker' in navigator) {
    const registration = await navigator.serviceWorker.getRegistration().catch(() => undefined);
    if (registration) {
      await registration.showNotification(title, { body, icon: '/hcbe-app-icon.svg', badge: '/hcbe-app-icon-maskable.svg', data: { url } });
      return;
    }
  }
  new Notification(title, { body, icon: '/hcbe-app-icon.svg' });
};

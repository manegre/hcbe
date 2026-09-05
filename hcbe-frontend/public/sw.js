const CACHE_VERSION = 'hcbe-pwa-v2';
const APP_SHELL = ['/', '/offline.html', '/manifest.webmanifest', '/hcbe-app-icon.svg', '/hcbe-app-icon-maskable.svg', '/hcbe-app-icon-180.png', '/hcbe-app-icon-192.png', '/hcbe-app-icon-512.png', '/hcbe-app-icon-maskable-512.png'];
const localDevelopment = self.location.hostname === 'localhost' || self.location.hostname === '127.0.0.1';

self.addEventListener('install', (event) => {
  event.waitUntil(caches.open(CACHE_VERSION).then((cache) => cache.addAll(APP_SHELL)).then(() => self.skipWaiting()));
});

self.addEventListener('activate', (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_VERSION).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener('message', (event) => {
  if (event.data?.type === 'SKIP_WAITING') self.skipWaiting();
});

self.addEventListener('fetch', (event) => {
  if (localDevelopment) return;
  const request = event.request;
  if (request.method !== 'GET') return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin || url.pathname.startsWith('/api/') || url.pathname.startsWith('/uploads/') || url.pathname.startsWith('/hubs/')) return;

  if (request.mode === 'navigate') {
    event.respondWith(fetch(request).then((response) => {
      if (response.ok) caches.open(CACHE_VERSION).then((cache) => cache.put(request, response.clone()));
      return response;
    }).catch(async () => (await caches.match(request)) || (await caches.match('/')) || caches.match('/offline.html')));
    return;
  }

  if (['style', 'script', 'image', 'font'].includes(request.destination)) {
    event.respondWith(caches.match(request).then((cached) => {
      const refreshed = fetch(request).then((response) => {
        if (response.ok) caches.open(CACHE_VERSION).then((cache) => cache.put(request, response.clone()));
        return response;
      }).catch(() => cached);
      return cached || refreshed;
    }));
  }
});

self.addEventListener('push', (event) => {
  let payload = {};
  try { payload = event.data?.json() || {}; } catch { payload = { body: event.data?.text() }; }
  event.waitUntil(self.registration.showNotification(payload.title || 'HCBE Canada', {
    body: payload.body || '',
    icon: payload.icon || '/hcbe-app-icon.svg',
    badge: '/hcbe-app-icon-maskable.svg',
    tag: payload.tag || 'hcbe-community-update',
    renotify: Boolean(payload.renotify),
    data: { url: payload.url || '/espace-membre?section=notifications' },
  }));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  const destination = new URL(event.notification.data?.url || '/', self.location.origin).href;
  event.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(async (windows) => {
    const current = windows.find((client) => new URL(client.url).origin === self.location.origin);
    if (current) { await current.navigate(destination); return current.focus(); }
    return self.clients.openWindow(destination);
  }));
});

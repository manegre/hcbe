const CACHE = 'hcbe-shell-v1';
const SHELL = ['/', '/manifest.webmanifest', '/hcbe-app-icon.svg'];
self.addEventListener('install', (event) => event.waitUntil(caches.open(CACHE).then((cache) => cache.addAll(SHELL)).then(() => self.skipWaiting())));
self.addEventListener('activate', (event) => event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE).map((key) => caches.delete(key)))).then(() => self.clients.claim())));
self.addEventListener('fetch', (event) => {
  const request = event.request;
  if (request.method !== 'GET' || new URL(request.url).pathname.startsWith('/api/')) return;
  event.respondWith(fetch(request).then((response) => { const copy = response.clone(); caches.open(CACHE).then((cache) => cache.put(request, copy)); return response; }).catch(() => caches.match(request).then((cached) => cached || caches.match('/'))));
});
self.addEventListener('message', (event) => {
  if (event.data?.type === 'HCBE_NOTIFICATION') self.registration.showNotification(event.data.title || 'HCBE Canada', { body: event.data.body, icon: '/hcbe-app-icon.svg', badge: '/hcbe-app-icon.svg', data: { url: event.data.url || '/espace-membre' } });
});
self.addEventListener('notificationclick', (event) => { event.notification.close(); event.waitUntil(clients.matchAll({ type: 'window', includeUncontrolled: true }).then((windows) => { const target = windows.find((client) => 'focus' in client); return target ? target.focus().then(() => target.navigate(event.notification.data?.url || '/')) : clients.openWindow(event.notification.data?.url || '/'); })); });

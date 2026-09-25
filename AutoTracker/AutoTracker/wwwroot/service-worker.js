const STATIC_CACHE = 'autotracker-static-v2';
const STATIC_ASSETS = ['/css/site.css', '/css/site-improvements.css', '/js/site.js', '/js/zxing-browser.min.js', '/icons/icon-192.png', '/icons/icon-512.png'];
self.addEventListener('install', event => event.waitUntil(caches.open(STATIC_CACHE).then(cache => cache.addAll(STATIC_ASSETS)).then(() => self.skipWaiting())));
self.addEventListener('activate', event => event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key !== STATIC_CACHE).map(key => caches.delete(key)))).then(() => self.clients.claim())));
self.addEventListener('fetch', event => {
  const request = event.request;
  if (request.method !== 'GET' || request.mode === 'navigate' || new URL(request.url).origin !== location.origin) return;
  if (!STATIC_ASSETS.some(asset => new URL(request.url).pathname === asset)) return;
  event.respondWith(caches.match(request).then(cached => cached || fetch(request).then(response => {
    const copy = response.clone();
    caches.open(STATIC_CACHE).then(cache => cache.put(request, copy));
    return response;
  })));
});

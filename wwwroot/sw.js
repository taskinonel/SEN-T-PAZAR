const CACHE_NAME = 'sen-t-v3';
const ASSETS = [
    '/',
    '/css/site.css',
    '/js/site.js',
    '/js/compare.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/lib/bootstrap/dist/js/bootstrap.bundle.min.js'
];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))
    );
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)));
        await self.clients.claim();
    })());
});

self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }

    if (event.data && event.data.type === 'SHOW_NOTIFICATION') {
        const title = event.data.title || 'SEN-T PAZAR';
        const body = event.data.body || 'Yeni güncellemeler var.';

        self.registration.showNotification(title, {
            body,
            icon: '/img/logo.png',
            badge: '/img/logo.png',
            tag: 'sent-notification',
            renotify: true,
            data: { url: '/' }
        });
    }
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const targetUrl = event.notification?.data?.url || '/';

    event.waitUntil((async () => {
        const clientList = await clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const client of clientList) {
            if ('focus' in client) {
                client.navigate(targetUrl);
                return client.focus();
            }
        }
        if (clients.openWindow) {
            return clients.openWindow(targetUrl);
        }
    })());
});

self.addEventListener('fetch', (event) => {
    if (event.request.method !== 'GET') {
        return;
    }

    if (event.request.mode === 'navigate') {
        event.respondWith((async () => {
            try {
                return await fetch(event.request);
            } catch {
                const cached = await caches.match('/');
                return cached || Response.error();
            }
        })());
        return;
    }

    const requestUrl = new URL(event.request.url);
    const isLocal = requestUrl.origin === self.location.origin;
    const isStyleOrScript = event.request.destination === 'style' || event.request.destination === 'script';

    // Keep CSS/JS fresh by preferring network, then falling back to cache.
    if (isLocal && isStyleOrScript) {
        event.respondWith((async () => {
            const cache = await caches.open(CACHE_NAME);
            try {
                const response = await fetch(event.request, { cache: 'no-cache' });
                if (response.ok) {
                    cache.put(event.request, response.clone());
                }
                return response;
            } catch {
                const cached = await cache.match(event.request);
                return cached || Response.error();
            }
        })());
        return;
    }

    event.respondWith((async () => {
        const cache = await caches.open(CACHE_NAME);
        const cached = await cache.match(event.request);
        if (cached) {
            return cached;
        }

        const response = await fetch(event.request);
        if (response.ok && event.request.url.startsWith(self.location.origin)) {
            cache.put(event.request, response.clone());
        }
        return response;
    })());
});

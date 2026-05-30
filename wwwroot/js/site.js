document.addEventListener('DOMContentLoaded', function () {
    // cache-bust: 2026-04-14-ui-restore
    // Global dil değiştirme fonksiyonu
    window.setLang = function (culture) {
        var url = new URL(window.location.href);
        url.searchParams.set('culture', culture);
        window.location.href = url.toString();
    };
    const searchForm = document.querySelector('.search-panel[role="search"]');
    const saveSearchBtn = document.getElementById('saveSearchBtn');
    const savedSearchFeedback = document.getElementById('savedSearchFeedback');
    const categorySelect = document.getElementById('category');
    const advancedButton = document.getElementById('btn-advanced-search');
    const advancedFieldsEl = document.getElementById('advanced-search-fields');
    const advancedFields = [
        document.getElementById('priceRange'),
        document.getElementById('keyword')
    ];
    const backLink = document.querySelector('[data-back-link]');
    const cookieBanner = document.getElementById('cookieConsentBanner');
    const cookieAcceptBtn = document.getElementById('cookieAcceptBtn');
    const cookieRejectBtn = document.getElementById('cookieRejectBtn');

    if (advancedButton && advancedFieldsEl) {
        advancedButton.addEventListener('click', function () {
            const isOpen = advancedFieldsEl.classList.toggle('open');
            advancedButton.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
            if (isOpen) {
                advancedFields[0]?.focus();
            }
        });
    }

    const listingTabs = document.querySelectorAll('.search-panel__listing-tab');
    const listingTypeSelect = document.getElementById('listingType');
    const subCategorySelect = document.getElementById('subCategory');

    // Listing type tab handlers are in Index.cshtml inline script - this provides fallback state sync
    listingTabs.forEach(function (tab) {
        tab.addEventListener('click', function () {
            const listingType = tab.dataset.listingType;
            if (listingTypeSelect && listingType) {
                listingTypeSelect.value = listingType;
            }
            listingTabs.forEach(function (t) {
                t.classList.remove('is-active');
                t.setAttribute('aria-pressed', 'false');
            });
            tab.classList.add('is-active');
            tab.setAttribute('aria-pressed', 'true');
        });
    });

    if (categorySelect) {
        categorySelect.addEventListener('change', function () {
            if (subCategorySelect) {
                subCategorySelect.value = 'all';
            }
        });
    }

    const NAV_STACK_KEY = 'sentpazar.navStack.v1';
    const NAV_STACK_MAX = 25;

    function getCurrentPath() {
        return window.location.pathname + window.location.search + window.location.hash;
    }

    function readNavStack() {
        try {
            const raw = window.sessionStorage.getItem(NAV_STACK_KEY);
            if (!raw) {
                return [];
            }

            const parsed = JSON.parse(raw);
            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed
                .filter((x) => typeof x === 'string' && x.length > 0 && x.startsWith('/'))
                .slice(-NAV_STACK_MAX);
        } catch {
            return [];
        }
    }

    function writeNavStack(stack) {
        try {
            const trimmed = Array.isArray(stack) ? stack.slice(-NAV_STACK_MAX) : [];
            window.sessionStorage.setItem(NAV_STACK_KEY, JSON.stringify(trimmed));
        } catch {
            // Ignore sessionStorage errors (private mode, disabled storage, etc.)
        }
    }

    function trackInternalNavigation() {
        const current = getCurrentPath();
        const stack = readNavStack();

        const lastIndex = stack.lastIndexOf(current);
        if (lastIndex >= 0) {
            // Back/forward navigation: trim anything after current
            writeNavStack(stack.slice(0, lastIndex + 1));
            return;
        }

        stack.push(current);
        writeNavStack(stack);
    }

    trackInternalNavigation();

    if (backLink) {
        backLink.addEventListener('click', function (event) {
            const fallbackUrl = backLink.getAttribute('data-fallback-url') || '/';
            const currentPath = window.location.pathname + window.location.search + window.location.hash;
            let sameOriginReferrer = '';

            if (document.referrer) {
                try {
                    const referrerUrl = new URL(document.referrer);
                    const currentUrl = new URL(window.location.href);

                    if (referrerUrl.origin === currentUrl.origin) {
                        sameOriginReferrer = referrerUrl.pathname + referrerUrl.search + referrerUrl.hash;
                    }
                } catch (error) {
                    sameOriginReferrer = '';
                }
            }

            event.preventDefault();

            const stack = readNavStack();
            if (stack.length > 1) {
                // Pop current
                if (stack[stack.length - 1] === currentPath) {
                    stack.pop();
                }

                const target = stack.pop();
                writeNavStack(stack);

                if (target && target !== currentPath) {
                    window.location.href = target;
                    return;
                }
            }

            // Fallback: never bounce between pages; prefer history back only for same-origin referrer
            if (sameOriginReferrer && sameOriginReferrer !== currentPath) {
                if (window.history.length > 1) {
                    window.history.back();
                    return;
                }

                window.location.href = sameOriginReferrer;
                return;
            }

            window.location.href = fallbackUrl;
        });
    }

    const heroRotator = document.querySelector('[data-hero-rotator]');

    if (heroRotator) {
        const heroEl = heroRotator.closest('.hero') || heroRotator.parentElement;
        let images = [];

        try {
            const rawImages = heroRotator.dataset.images;
            if (rawImages) {
                images = JSON.parse(rawImages);
            }
        } catch (error) {
            console.warn('Hero rotator: Failed to parse images data', error);
            images = [];
        }

        // Filter valid image URLs
        images = images.filter(function (image) {
            return typeof image === 'string' && image.trim().length > 0;
        });

        // --- Brightness detection for adaptive text colour ---
        var brightnessMap = {};
        var BRIGHTNESS_THRESHOLD = 132;
        var brightnessCanvas = document.createElement('canvas');
        brightnessCanvas.width = 64;
        brightnessCanvas.height = 64;
        var brightnessCtx = brightnessCanvas.getContext('2d');

        function detectBrightness(src, callback) {
            if (brightnessMap[src] !== undefined) {
                callback(brightnessMap[src]);
                return;
            }
            var img = new Image();
            img.crossOrigin = 'anonymous';
            img.onload = function () {
                try {
                    brightnessCtx.clearRect(0, 0, 64, 64);
                    brightnessCtx.drawImage(img, 0, 0, 64, 64);
                    var data = brightnessCtx.getImageData(0, 0, 64, 64).data;
                    var total = 0;
                    for (var i = 0; i < data.length; i += 4) {
                        total += 0.299 * data[i] + 0.587 * data[i + 1] + 0.114 * data[i + 2];
                    }
                    var avg = total / (data.length / 4);
                    brightnessMap[src] = avg;
                    callback(avg);
                } catch (e) {
                    brightnessMap[src] = BRIGHTNESS_THRESHOLD - 1;
                    callback(brightnessMap[src]);
                }
            };
            img.onerror = function () {
                brightnessMap[src] = BRIGHTNESS_THRESHOLD - 1;
                callback(brightnessMap[src]);
            };
            img.src = src;
        }

        function applyTextTheme(src) {
            detectBrightness(src, function (brightness) {
                if (brightness > BRIGHTNESS_THRESHOLD) {
                    heroEl.classList.remove('hero--light-text');
                    heroEl.classList.add('hero--dark-text');
                } else {
                    heroEl.classList.remove('hero--dark-text');
                    heroEl.classList.add('hero--light-text');
                }
            });
        }

        // Keep the first frame fixed to the configured KKTC panorama.
        var initialImage = heroRotator.dataset.initialImage;
        var currentIndex = -1;

        // Apply the configured opening image first, then rotate category visuals.
        if (initialImage && initialImage.trim().length > 0) {
            heroRotator.style.backgroundImage = "url('" + initialImage + "')";
            heroRotator.style.backgroundSize = 'cover';
            heroRotator.style.backgroundPosition = 'center';
            applyTextTheme(initialImage);
        }

        if (images.length > 0) {
            var preloadLink = document.createElement('link');
            preloadLink.rel = 'preload';
            preloadLink.as = 'image';
            preloadLink.href = images[0];
            document.head.appendChild(preloadLink);

            if (!initialImage || initialImage.trim().length === 0) {
                heroRotator.style.backgroundImage = "url('" + images[0] + "')";
                heroRotator.style.backgroundSize = 'cover';
                heroRotator.style.backgroundPosition = 'center';
                applyTextTheme(images[0]);
                currentIndex = 0;
            }
        } else if (!initialImage || initialImage.trim().length === 0) {
            // Fallback for no images
            heroRotator.style.background = 'linear-gradient(135deg, var(--primary-color) 0%, var(--accent-color) 100%)';
        }

        if (images.length > 1) {
            var interval = Number.parseInt(heroRotator.dataset.interval || '6500', 10);

            function preloadImage(source) {
                var img = new Image();
                img.src = source;
            }

            function showNextImage() {
                currentIndex = (currentIndex + 1) % images.length;
                var nextImage = images[currentIndex];

                // Preload next image
                preloadImage(images[(currentIndex + 1) % images.length]);
                heroRotator.classList.add('is-swapping');

                window.setTimeout(function () {
                    heroRotator.style.backgroundImage = "url('" + nextImage + "')";
                    heroRotator.classList.remove('is-swapping');

                    // Update text colour for new image
                    applyTextTheme(nextImage);
                }, 220);
            }

            // Preload first rotating image right away
            var nextIndex = (currentIndex + 1 + images.length) % images.length;
            if (images[nextIndex]) {
                preloadImage(images[nextIndex]);
            }

            // Start rotator
            window.setInterval(showNextImage, Number.isNaN(interval) ? 6500 : interval);
        }
    }

    if (searchForm && categorySelect) {
        searchForm.addEventListener('submit', function (event) {
            const selectedOption = categorySelect.selectedOptions[0];

            if (!selectedOption) {
                return;
            }

            const selectedCategory = categorySelect.value;
            const isCategoryPage = searchForm.dataset.isCategoryPage === 'true';
            const currentCategory = searchForm.dataset.currentCategory || 'all';

            function buildQuery(excludedKeys) {
                const params = new URLSearchParams();
                const formData = new FormData(searchForm);

                for (const [key, value] of formData.entries()) {
                    if (excludedKeys.includes(key)) {
                        continue;
                    }

                    const normalizedValue = typeof value === 'string' ? value.trim() : value;

                    if (normalizedValue) {
                        params.set(key, normalizedValue.toString());
                    }
                }

                return params.toString();
            }

            if (selectedCategory !== 'all' && (!isCategoryPage || selectedCategory !== currentCategory)) {
                event.preventDefault();

                const targetUrl = selectedOption.dataset.url;
                if (!targetUrl) {
                    return;
                }

                const query = buildQuery(['slug', 'category']);
                const url = new URL(targetUrl, window.location.origin);
                url.search = query;
                window.location.assign(`${url.pathname}${url.search}`);
                return;
            }

            if (isCategoryPage && selectedCategory === 'all') {
                event.preventDefault();

                const homeUrl = searchForm.dataset.homeUrl || '/';
                const query = buildQuery(['slug', 'category']);
                const url = new URL(homeUrl, window.location.origin);
                url.search = query;
                window.location.assign(`${url.pathname}${url.search}`);
            }
        });

        if (saveSearchBtn) {
            saveSearchBtn.addEventListener('click', async function () {
                const formData = new FormData(searchForm);
                const payload = {};

                formData.forEach(function (value, key) {
                    if (typeof value === 'string' && value.trim().length > 0) {
                        payload[key] = value.trim();
                    }
                });

                const storageKey = 'sent-saved-searches';
                const saved = JSON.parse(localStorage.getItem(storageKey) || '[]');
                saved.unshift({
                    createdAt: new Date().toISOString(),
                    query: payload,
                    path: window.location.pathname
                });

                localStorage.setItem(storageKey, JSON.stringify(saved.slice(0, 10)));

                try {
                    const response = await fetch('/Home/SaveSearch', {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json'
                        },
                        body: JSON.stringify({
                            query: payload,
                            path: window.location.pathname
                        })
                    });

                    if (response.ok && savedSearchFeedback) {
                        savedSearchFeedback.textContent = 'Arama kriterleri hesabiniza kaydedildi.';
                        savedSearchFeedback.style.display = 'block';
                        window.setTimeout(function () {
                            savedSearchFeedback.style.display = 'none';
                        }, 2500);
                        return;
                    }
                } catch (error) {
                    // Silent fallback to local storage only.
                }

                if (savedSearchFeedback) {
                    savedSearchFeedback.textContent = 'Arama kriterleri kaydedildi.';
                    savedSearchFeedback.style.display = 'block';
                    window.setTimeout(function () {
                        savedSearchFeedback.style.display = 'none';
                    }, 2500);
                }
            });
        }
    }

    if (cookieBanner) {
        const consentKey = 'sent-cookie-consent';
        const savedConsent = localStorage.getItem(consentKey);

        if (!savedConsent) {
            cookieBanner.hidden = false;
            document.body.classList.add('cookie-consent-open');
        }

        const storeConsent = function (value) {
            localStorage.setItem(consentKey, value);
            document.cookie = 'sent_cookie_consent=' + encodeURIComponent(value) + ';path=/;max-age=' + (60 * 60 * 24 * 365) + ';samesite=lax';
            cookieBanner.hidden = true;
            document.body.classList.remove('cookie-consent-open');
        };

        if (cookieAcceptBtn) {
            cookieAcceptBtn.addEventListener('click', function () {
                storeConsent('all');
            });
        }

        if (cookieRejectBtn) {
            cookieRejectBtn.addEventListener('click', function () {
                storeConsent('required');
            });
        }
    }

    /* ----------------------------------------
       Scroll Reveal Animations
       ---------------------------------------- */
    const revealElements = document.querySelectorAll('.reveal, .reveal-left, .reveal-right, .reveal-scale, .reveal-stagger');

    if (revealElements.length > 0 && 'IntersectionObserver' in window) {
        const revealObserver = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add('revealed');
                    revealObserver.unobserve(entry.target);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -40px 0px'
        });

        revealElements.forEach(function (el) {
            revealObserver.observe(el);
        });
    } else {
        // Fallback: show all elements immediately
        revealElements.forEach(function (el) {
            el.classList.add('revealed');
        });
    }
});

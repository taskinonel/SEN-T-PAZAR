document.addEventListener('DOMContentLoaded', function () {
    const searchForm = document.querySelector('.search-panel[role="search"]');
    const categorySelect = document.getElementById('category');
    const advancedButton = document.getElementById('btn-advanced-search');
    const advancedFieldsEl = document.getElementById('advanced-search-fields');
    const advancedFields = [
        document.getElementById('priceRange'),
        document.getElementById('sortBy'),
        document.getElementById('keyword')
    ];

    if (advancedButton && advancedFieldsEl) {
        advancedButton.addEventListener('click', function () {
            const isOpen = advancedFieldsEl.classList.toggle('open');
            advancedButton.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
            if (isOpen) {
                advancedFields[0]?.focus();
            }
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

        // Preload first image immediately
        if (images.length > 0) {
            var preloadLink = document.createElement('link');
            preloadLink.rel = 'preload';
            preloadLink.as = 'image';
            preloadLink.href = images[0];
            document.head.appendChild(preloadLink);

            // Set initial background image
            heroRotator.style.backgroundImage = "url('" + images[0] + "')";
            heroRotator.style.backgroundSize = 'cover';
            heroRotator.style.backgroundPosition = 'center';

            // Detect brightness for first image
            applyTextTheme(images[0]);
        } else {
            // Fallback for no images
            heroRotator.style.background = 'linear-gradient(135deg, var(--primary-color) 0%, var(--accent-color) 100%)';
        }

        if (images.length > 1) {
            var currentIndex = 0;
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

            // Preload second image
            if (images[1]) {
                preloadImage(images[1]);
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

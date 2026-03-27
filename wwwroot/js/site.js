document.addEventListener('DOMContentLoaded', function () {
    const searchForm = document.querySelector('.search-panel[role="search"]');
    const categorySelect = document.getElementById('category');
    const advancedButton = document.getElementById('btn-advanced-search');
    const advancedModal = document.getElementById('advanced-search-modal');
    const closeButton = advancedModal?.querySelector('.advanced-search-close');
    const resetButton = document.getElementById('reset-advanced-search');
    const advancedFields = [
        document.getElementById('priceRange'),
        document.getElementById('sortBy'),
        document.getElementById('keyword')
    ];

    function toggleAdvancedSearch(isOpen) {
        if (!advancedModal) {
            return;
        }

        advancedModal.classList.toggle('active', isOpen);
        advancedModal.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
        document.body.classList.toggle('modal-open', isOpen);

        if (isOpen) {
            advancedFields[0]?.focus();
        }
    }

    if (advancedButton && advancedModal) {
        advancedButton.addEventListener('click', function () {
            toggleAdvancedSearch(true);
        });

        closeButton?.addEventListener('click', function () {
            toggleAdvancedSearch(false);
        });

        advancedModal.addEventListener('click', function (event) {
            if (event.target === advancedModal) {
                toggleAdvancedSearch(false);
            }
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape' && advancedModal.classList.contains('active')) {
                toggleAdvancedSearch(false);
            }
        });
    }

    resetButton?.addEventListener('click', function () {
        const priceRange = document.getElementById('priceRange');
        const sortBy = document.getElementById('sortBy');
        const keyword = document.getElementById('keyword');

        if (priceRange) {
            priceRange.value = 'any';
        }

        if (sortBy) {
            sortBy.value = 'latest';
        }

        if (keyword) {
            keyword.value = '';
        }
    });

    const heroRotator = document.querySelector('[data-hero-rotator]');

    if (heroRotator) {
        let images = [];

        try {
            images = JSON.parse(heroRotator.dataset.images || '[]');
        } catch (error) {
            images = [];
        }

        images = images.filter(function (image) {
            return typeof image === 'string' && image.trim().length > 0;
        });

        if (images.length > 1) {
            let currentIndex = 0;
            const interval = Number.parseInt(heroRotator.dataset.interval || '6500', 10);

            function preloadImage(source) {
                const image = new Image();
                image.src = source;
            }

            function showNextImage() {
                currentIndex = (currentIndex + 1) % images.length;
                const nextImage = images[currentIndex];

                preloadImage(images[(currentIndex + 1) % images.length]);
                heroRotator.classList.add('is-swapping');

                window.setTimeout(function () {
                    heroRotator.style.backgroundImage = `url('${nextImage}')`;
                    heroRotator.classList.remove('is-swapping');
                }, 220);
            }

            preloadImage(images[1]);
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
});

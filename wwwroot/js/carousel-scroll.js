/**
 * SEN-T PAZAR - Enhanced Carousel Component
 * Features: Touch/swipe support, keyboard navigation, auto-hide arrows, smooth scrolling
 */

(function() {
    'use strict';

    // Configuration
    const CONFIG = {
        scrollBehavior: 'smooth',
        touchThreshold: 50,      // Minimum swipe distance
        touchVelocity: 0.5,      // Velocity multiplier for momentum scrolling
        keyScrollAmount: 300,    // Pixels to scroll on arrow keys
        resizeDebounce: 150      // ms to wait before recalculating on resize
    };

    /**
     * Initialize all carousels on the page
     */
    function initCarousels() {
        const carousels = document.querySelectorAll('.featured-carousel');
        carousels.forEach(initCarousel);
    }

    /**
     * Initialize a single carousel instance
     */
    function initCarousel(carousel) {
        const track = carousel.querySelector('.featured-carousel-track');
        const leftBtn = carousel.querySelector('.carousel-arrow--left');
        const rightBtn = carousel.querySelector('.carousel-arrow--right');
        
        if (!track) return;

        // Store references for event handlers
        const state = {
            isDragging: false,
            startX: 0,
            scrollLeft: 0,
            velocity: 0,
            lastX: 0,
            lastTime: 0,
            cardWidth: 0,
            visibleCount: 1
        };

        // Calculate dimensions
        function updateDimensions() {
            const card = track.querySelector('.project-card');
            if (card) {
                // Include gap in calculation
                const gap = parseInt(getComputedStyle(track).gap) || 16;
                state.cardWidth = card.offsetWidth + gap;
            }
            
            const viewport = track.parentElement;
            if (viewport) {
                state.visibleCount = Math.max(1, Math.floor(viewport.offsetWidth / (state.cardWidth || 1)));
            }
            
            updateArrowVisibility();
        }

        // Update arrow visibility based on scroll position
        function updateArrowVisibility() {
            if (leftBtn) {
                leftBtn.disabled = track.scrollLeft <= 0;
                leftBtn.style.opacity = leftBtn.disabled ? '0.3' : '1';
            }
            if (rightBtn) {
                const maxScroll = track.scrollWidth - track.clientWidth;
                rightBtn.disabled = track.scrollLeft >= maxScroll - 1;
                rightBtn.style.opacity = rightBtn.disabled ? '0.3' : '1';
            }
        }

        // Scroll handlers
        function scrollLeft() {
            track.scrollBy({
                left: -state.cardWidth * state.visibleCount,
                behavior: CONFIG.scrollBehavior
            });
        }

        function scrollRight() {
            track.scrollBy({
                left: state.cardWidth * state.visibleCount,
                behavior: CONFIG.scrollBehavior
            });
        }

        // Touch / Mouse drag handlers
        function handleDragStart(e) {
            state.isDragging = true;
            state.startX = (e.pageX || e.touches[0].pageX) - track.offsetLeft;
            state.scrollLeft = track.scrollLeft;
            state.lastX = state.startX;
            state.lastTime = Date.now();
            velocity = 0;
            
            track.style.cursor = 'grabbing';
            track.style.userSelect = 'none';
        }

        function handleDragMove(e) {
            if (!state.isDragging) return;
            
            e.preventDefault();
            const x = (e.pageX || e.touches[0].pageX) - track.offsetLeft;
            const walk = (x - state.startX) * 1.5; // Multiplier for faster scrolling
            
            // Calculate velocity for momentum
            const now = Date.now();
            const dt = now - state.lastTime;
            if (dt > 0) {
                state.velocity = (x - state.lastX) / dt;
            }
            state.lastX = x;
            state.lastTime = now;
            
            track.scrollLeft = state.scrollLeft - walk;
        }

        function handleDragEnd() {
            if (!state.isDragging) return;
            
            state.isDragging = false;
            track.style.cursor = '';
            track.style.userSelect = '';
            
            // Apply momentum scrolling
            if (Math.abs(state.velocity) > 0.1) {
                const momentum = state.velocity * CONFIG.touchVelocity * 100;
                track.scrollBy({
                    left: -momentum,
                    behavior: 'smooth'
                });
            }
            
            updateArrowVisibility();
        }

        // Keyboard navigation
        function handleKeydown(e) {
            // Only handle if carousel is in viewport
            const rect = carousel.getBoundingClientRect();
            const isInViewport = rect.top < window.innerHeight && rect.bottom > 0;
            
            if (!isInViewport) return;
            
            switch(e.key) {
                case 'ArrowLeft':
                    e.preventDefault();
                    scrollLeft();
                    break;
                case 'ArrowRight':
                    e.preventDefault();
                    scrollRight();
                    break;
                case 'Home':
                    e.preventDefault();
                    track.scrollTo({ left: 0, behavior: CONFIG.scrollBehavior });
                    break;
                case 'End':
                    e.preventDefault();
                    track.scrollTo({ left: track.scrollWidth, behavior: CONFIG.scrollBehavior });
                    break;
            }
        }

        // Event listeners
        if (leftBtn) {
            leftBtn.addEventListener('click', () => {
                scrollLeft();
                setTimeout(updateArrowVisibility, 350);
            });
        }

        if (rightBtn) {
            rightBtn.addEventListener('click', () => {
                scrollRight();
                setTimeout(updateArrowVisibility, 350);
            });
        }

        // Mouse events for dragging
        track.addEventListener('mousedown', handleDragStart);
        track.addEventListener('mousemove', handleDragMove);
        track.addEventListener('mouseup', handleDragEnd);
        track.addEventListener('mouseleave', handleDragEnd);

        // Touch events
        track.addEventListener('touchstart', handleDragStart, { passive: true });
        track.addEventListener('touchmove', handleDragMove, { passive: false });
        track.addEventListener('touchend', handleDragEnd);
        track.addEventListener('touchcancel', handleDragEnd);

        // Scroll event for arrow visibility
        let scrollTimeout;
        track.addEventListener('scroll', () => {
            clearTimeout(scrollTimeout);
            scrollTimeout = setTimeout(updateArrowVisibility, 50);
        }, { passive: true });

        // Keyboard navigation (global, but checks visibility)
        document.addEventListener('keydown', handleKeydown);

        // Resize handler with debounce
        let resizeTimeout;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTimeout);
            resizeTimeout = setTimeout(updateDimensions, CONFIG.resizeDebounce);
        });

        // Intersection Observer for performance (pause when not visible)
        if ('IntersectionObserver' in window) {
            const observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        updateDimensions();
                    }
                });
            }, { threshold: 0.1 });
            
            observer.observe(carousel);
        }

        // Initial setup
        updateDimensions();
        
        // Expose API for external control
        carousel.carouselAPI = {
            scrollLeft,
            scrollRight,
            scrollToIndex: (index) => {
                track.scrollTo({
                    left: state.cardWidth * index,
                    behavior: CONFIG.scrollBehavior
                });
            },
            updateDimensions
        };
    }

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCarousels);
    } else {
        initCarousels();
    }

    // Re-initialize on dynamic content changes (if using HTMX, etc.)
    document.addEventListener('carousel:reinit', initCarousels);
    
    // Expose global API
    window.SENTCarousel = {
        refresh: initCarousels,
        getInstance: (element) => element?.carouselAPI
    };
})();

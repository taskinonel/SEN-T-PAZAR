/**
 * SEN-T PAZAR - Favoriler Sistemi
 * Heart button ve favoriler yönetimi
 */

class FavoritesManager {
    constructor() {
        this.apiBase = '/api/favorites';
        this.isAuthenticated = Boolean(window.sentUserState && window.sentUserState.isAuthenticated);
        this.pendingListingIds = new Set();
        this.init();
    }

    /**
     * Manager'ı başlat
     */
    init() {
        this.setupEventListeners();
        if (this.isAuthenticated) {
            this.updateAllFavoriteButtons();
        }
    }

    /**
     * Event listener'ları kur
     */
    setupEventListeners() {
        // Heart button tıklaması
        document.addEventListener('click', (e) => {
            if (e.target.closest('.btn-favorite, .heart-btn')) {
                e.preventDefault();
                e.stopPropagation();
                
                const btn = e.target.closest('.btn-favorite, .heart-btn');
                const listingId = btn.getAttribute('data-listing-id');
                if (!listingId || this.pendingListingIds.has(String(listingId))) {
                    return;
                }

                const isFavorited = btn.classList.contains('favorited');
                
                if (isFavorited) {
                    this.removeFavorite(listingId, btn);
                } else {
                    this.addFavorite(listingId, btn);
                }
            }
        });
    }

    /**
     * İlanı favoriye ekle
     * @param {number} listingId - İlan ID'si
     * @param {Element} btn - Heart button element'i
     */
    async addFavorite(listingId, btn) {
        if (!this.isAuthenticated) {
            this.showNotification('Lütfen önce giriş yapınız', 'warning');
            window.location.href = '/Account/Login';
            return;
        }

        try {
            this.pendingListingIds.add(String(listingId));
            btn.disabled = true;
            btn.classList.add('loading');

            const response = await fetch(`${this.apiBase}/${listingId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': this.getCSRFToken()
                },
                credentials: 'include'
            });

            const data = await this.readJsonResponse(response);

            if (data?.success) {
                this.setFavoriteState(listingId, true);
                this.showNotification('İlan favorilere eklendi', 'success');
                this.updateFavoriteCount();
            } else {
                if (response.status === 401 || response.redirected) {
                    this.showNotification('Lütfen önce giriş yapınız', 'warning');
                    window.location.href = '/Account/Login';
                } else {
                    this.showNotification(data?.message || 'Hata oluştu', 'error');
                }
            }
        } catch (error) {
            console.error('Favoriye ekleme hatası:', error);
            this.showNotification('Hata oluştu: ' + error.message, 'error');
        } finally {
            this.pendingListingIds.delete(String(listingId));
            btn.disabled = false;
            btn.classList.remove('loading');
        }
    }

    /**
     * İlanı favorilerden çıkar
     * @param {number} listingId - İlan ID'si
     * @param {Element} btn - Heart button element'i
     */
    async removeFavorite(listingId, btn) {
        if (!this.isAuthenticated) {
            this.showNotification('Lütfen önce giriş yapınız', 'warning');
            window.location.href = '/Account/Login';
            return;
        }

        try {
            this.pendingListingIds.add(String(listingId));
            btn.disabled = true;
            btn.classList.add('loading');

            const response = await fetch(`${this.apiBase}/${listingId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': this.getCSRFToken()
                },
                credentials: 'include'
            });

            const data = await this.readJsonResponse(response);

            if (data?.success) {
                this.setFavoriteState(listingId, false);
                this.showNotification('İlan favorilerden çıkarıldı', 'success');
                this.updateFavoriteCount();
            } else {
                if (response.status === 401 || response.redirected) {
                    this.showNotification('Lütfen önce giriş yapınız', 'warning');
                    window.location.href = '/Account/Login';
                } else {
                    this.showNotification(data?.message || 'Hata oluştu', 'error');
                }
            }
        } catch (error) {
            console.error('Favoriden çıkarma hatası:', error);
            this.showNotification('Hata oluştu: ' + error.message, 'error');
        } finally {
            this.pendingListingIds.delete(String(listingId));
            btn.disabled = false;
            btn.classList.remove('loading');
        }
    }

    /**
     * Tüm favori button'ları kontrol et ve güncelle
     */
    async updateAllFavoriteButtons() {
        const buttons = document.querySelectorAll('.btn-favorite[data-listing-id], .heart-btn[data-listing-id]');
        
        for (const btn of buttons) {
            if (btn.closest('.favorites-section') && btn.closest('.favorite-card')) {
                continue;
            }

            const listingId = btn.getAttribute('data-listing-id');
            await this.checkIsFavorite(listingId, btn);
        }
    }

    setFavoriteState(listingId, isFavorited) {
        const selector = `.btn-favorite[data-listing-id="${listingId}"], .heart-btn[data-listing-id="${listingId}"]`;

        document.querySelectorAll(selector).forEach((button) => {
            button.classList.toggle('favorited', isFavorited);
            button.setAttribute('title', isFavorited ? 'Favorilerinden çıkar' : 'Favorilerime ekle');
            button.querySelector('.heart-icon')?.classList.toggle('active', isFavorited);
        });
    }

    /**
     * İlanın favoride olup olmadığını kontrol et
     * @param {number} listingId - İlan ID'si
     * @param {Element} btn - Heart button element'i
     */
    async checkIsFavorite(listingId, btn) {
        try {
            const cacheBust = `ts=${Date.now()}`;
            const response = await fetch(`${this.apiBase}/${listingId}/is-favorite?${cacheBust}`, {
                method: 'GET',
                cache: 'no-store',
                headers: {
                    'Content-Type': 'application/json',
                    'Cache-Control': 'no-cache, no-store, max-age=0',
                    'Pragma': 'no-cache'
                },
                credentials: 'include'
            });

            if (response.ok) {
                const data = await this.readJsonResponse(response);
                if (!data) {
                    return;
                }

                if (data.isFavorite) {
                    btn.classList.add('favorited');
                    btn.setAttribute('title', 'Favorilerinden çıkar');
                    btn.querySelector('.heart-icon')?.classList.add('active');
                } else {
                    btn.classList.remove('favorited');
                    btn.setAttribute('title', 'Favorilerime ekle');
                    btn.querySelector('.heart-icon')?.classList.remove('active');
                }
            }
        } catch (error) {
            console.warn('Favori durumu kontrol hatası:', error);
        }
    }

    /**
     * Favori sayısını güncelle ve göster
     */
    async updateFavoriteCount() {
        if (!this.isAuthenticated) {
            return;
        }

        try {
            const response = await fetch(`${this.apiBase}/count?ts=${Date.now()}`, {
                method: 'GET',
                cache: 'no-store',
                headers: {
                    'Content-Type': 'application/json',
                    'Cache-Control': 'no-cache, no-store, max-age=0',
                    'Pragma': 'no-cache'
                },
                credentials: 'include'
            });

            if (response.ok) {
                const data = await this.readJsonResponse(response);
                if (!data) {
                    return;
                }

                const countElements = document.querySelectorAll('.favorite-count');
                countElements.forEach((countElement) => {
                    // Detay sayfasındaki halka açık favori sayısı (listing bazlı) üzerine yazma
                    if (countElement.id && countElement.id.startsWith('listing-favorite')) {
                        return;
                    }
                    if (countElement.closest('.detail-summary-card__header')) {
                        return;
                    }
                    countElement.textContent = data.count;
                    countElement.style.display = data.count > 0 ? 'inline' : 'none';
                });
            }
        } catch (error) {
            console.warn('Favori sayısı güncelleme hatası:', error);
        }
    }

    async readJsonResponse(response) {
        const contentType = response.headers.get('content-type') || '';
        if (!contentType.includes('application/json')) {
            return null;
        }

        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    /**
     * CSRF token'ı al
     */
    getCSRFToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    /**
     * Bildirim göster
     * @param {string} message - Mesaj
     * @param {string} type - Tip: success, error, warning, info
     */
    showNotification(message, type = 'info') {
        // Toast notification göster
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            background: ${this.getToastColor(type)};
            color: white;
            border-radius: 6px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.15);
            z-index: 9999;
            animation: slideIn 0.3s ease-in-out;
            max-width: 400px;
            word-wrap: break-word;
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.animation = 'slideOut 0.3s ease-in-out';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    /**
     * Toast rengi al
     */
    getToastColor(type) {
        const colors = {
            'success': '#4CAF50',
            'error': '#f44336',
            'warning': '#ff9800',
            'info': '#2196F3'
        };
        return colors[type] || colors['info'];
    }
}

// Document ready
document.addEventListener('DOMContentLoaded', () => {
    // Global favoriter manager oluştur
    window.favoritesManager = new FavoritesManager();
});

// CSS Animations
const style = document.createElement('style');
style.textContent = `
@keyframes slideIn {
    from {
        transform: translateX(400px);
        opacity: 0;
    }
    to {
        transform: translateX(0);
        opacity: 1;
    }
}

@keyframes slideOut {
    from {
        transform: translateX(0);
        opacity: 1;
    }
    to {
        transform: translateX(400px);
        opacity: 0;
    }
}

.btn-favorite, .heart-btn {
    background: none;
    border: none;
    cursor: pointer;
    font-size: 1.5rem;
    transition: all 0.3s ease;
    padding: 8px;
    border-radius: 50%;
    display: inline-flex;
    align-items: center;
    justify-content: center;
}

.btn-favorite:hover, .heart-btn:hover {
    background: rgba(0,0,0,0.05);
    transform: scale(1.1);
}

.btn-favorite.favorited, .heart-btn.favorited {
    color: #ff0000;
}

.btn-favorite.loading, .heart-btn.loading {
    opacity: 0.6;
    cursor: not-allowed;
}

.heart-icon {
    transition: all 0.3s ease;
}

.heart-icon.active {
    animation: heartBeat 0.6s ease-in-out;
}

@keyframes heartBeat {
    0%, 100% { transform: scale(1); }
    14% { transform: scale(1.3); }
    28% { transform: scale(1); }
    42% { transform: scale(1.3); }
    70% { transform: scale(1); }
}

.favorite-count {
    display: none;
    background: #ff0000;
    color: white;
    border-radius: 50%;
    font-size: 0.75rem;
    font-weight: bold;
    padding: 2px 6px;
    position: absolute;
    top: -8px;
    right: -8px;
    min-width: 20px;
    text-align: center;
}
`;
document.head.appendChild(style);

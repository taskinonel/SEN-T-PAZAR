/**
 * SEN-T PAZAR - Favoriler Sistemi
 * Heart button ve favoriler yönetimi
 */

class FavoritesManager {
    constructor() {
        this.apiBase = '/api/favorites';
        this.init();
    }

    /**
     * Manager'ı başlat
     */
    init() {
        this.setupEventListeners();
        this.updateAllFavoriteButtons();
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
        try {
            btn.disabled = true;
            btn.classList.add('loading');

            const response = await fetch(`${this.apiBase}/${listingId}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': this.getCSRFToken()
                }
            });

            const data = await response.json();

            if (data.success) {
                btn.classList.add('favorited');
                btn.setAttribute('title', 'Favorilerinden çıkar');
                btn.querySelector('.heart-icon')?.classList.add('active');
                this.showNotification('İlan favorilere eklendi', 'success');
                this.updateFavoriteCount();
            } else {
                if (response.status === 401) {
                    this.showNotification('Lütfen önce giriş yapınız', 'warning');
                    window.location.href = '/Account/Login';
                } else {
                    this.showNotification(data.message || 'Hata oluştu', 'error');
                }
            }
        } catch (error) {
            console.error('Favoriye ekleme hatası:', error);
            this.showNotification('Hata oluştu: ' + error.message, 'error');
        } finally {
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
        try {
            btn.disabled = true;
            btn.classList.add('loading');

            const response = await fetch(`${this.apiBase}/${listingId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': this.getCSRFToken()
                }
            });

            const data = await response.json();

            if (data.success) {
                btn.classList.remove('favorited');
                btn.setAttribute('title', 'Favorilerime ekle');
                btn.querySelector('.heart-icon')?.classList.remove('active');
                this.showNotification('İlan favorilerden çıkarıldı', 'success');
                this.updateFavoriteCount();
            } else {
                this.showNotification(data.message || 'Hata oluştu', 'error');
            }
        } catch (error) {
            console.error('Favoriden çıkarma hatası:', error);
            this.showNotification('Hata oluştu: ' + error.message, 'error');
        } finally {
            btn.disabled = false;
            btn.classList.remove('loading');
        }
    }

    /**
     * Tüm favori button'ları kontrol et ve güncelle
     */
    async updateAllFavoriteButtons() {
        const buttons = document.querySelectorAll('[data-listing-id]');
        
        for (const btn of buttons) {
            const listingId = btn.getAttribute('data-listing-id');
            await this.checkIsFavorite(listingId, btn);
        }
    }

    /**
     * İlanın favoride olup olmadığını kontrol et
     * @param {number} listingId - İlan ID'si
     * @param {Element} btn - Heart button element'i
     */
    async checkIsFavorite(listingId, btn) {
        try {
            const response = await fetch(`${this.apiBase}/${listingId}/is-favorite`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
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
        try {
            const response = await fetch(`${this.apiBase}/count`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'include'
            });

            if (response.ok) {
                const data = await response.json();
                const countElement = document.querySelector('.favorite-count');
                if (countElement) {
                    countElement.textContent = data.count;
                    countElement.style.display = data.count > 0 ? 'inline' : 'none';
                }
            }
        } catch (error) {
            console.warn('Favori sayısı güncelleme hatası:', error);
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

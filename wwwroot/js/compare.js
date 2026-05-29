const CompareManager = {
    storageKey: 'sen_t_compare',
    maxItems: 4,

    addList(id) {
        let current = this.getList();
        if (current.includes(id)) {
            this.removeList(id);
            return false;
        }

        if (current.length >= this.maxItems) {
            alert('En fazla ' + this.maxItems + ' ilan karşılaştırabilirsiniz.');
            return false;
        }

        current.push(id);
        this.saveList(current);
        this.updateUI();
        return true;
    },

    removeList(id) {
        let current = this.getList();
        const index = current.indexOf(id);
        if (index > -1) {
            current.splice(index, 1);
            this.saveList(current);
            this.updateUI();
        }
    },

    getList() {
        const stored = localStorage.getItem(this.storageKey);
        return stored ? JSON.parse(stored) : [];
    },

    saveList(list) {
        localStorage.setItem(this.storageKey, JSON.stringify(list));
    },

    updateUI() {
        const list = this.getList();
        const count = list.length;
        
        // Update all compare buttons
        document.querySelectorAll('.btn-compare').forEach(btn => {
            const id = parseInt(btn.dataset.id);
            if (list.includes(id)) {
                btn.classList.add('active');
                btn.innerHTML = '<i class="bi bi-intersect me-1"></i>Eklendi';
            } else {
                btn.classList.remove('active');
                btn.innerHTML = '<i class="bi bi-intersect me-1"></i>Karşılaştır';
            }
        });

        // Update Floating Comparison Bar
        this.renderFloatingBar(count);
    },

    renderFloatingBar(count) {
        let bar = document.getElementById('compare-bar');
        if (!bar) {
            bar = document.createElement('div');
            bar.id = 'compare-bar';
            bar.className = 'compare-bar';
            document.body.appendChild(bar);
        }

        if (count > 0) {
            const ids = this.getList().join(',');
            bar.innerHTML = `
                <div class="container d-flex justify-content-between align-items-center h-100 px-4">
                    <div class="d-flex align-items-center">
                        <span class="compare-badge me-3">${count}</span>
                        <span class="fw-bold text-white d-none d-sm-inline">İlan seçildi</span>
                    </div>
                    <div class="d-flex gap-2">
                        <button onclick="CompareManager.clearAndClose()" class="btn btn-outline-light btn-sm rounded-pill px-3">Temizle</button>
                        <a href="/ListingCompare?ids=${ids}" class="btn btn-success btn-sm rounded-pill px-4 fw-bold shadow-sm">Kıyasla <i class="bi bi-arrow-right ms-1"></i></a>
                    </div>
                </div>
            `;
            bar.classList.add('show');
        } else {
            bar.classList.remove('show');
        }
    },

    clearAndClose() {
        this.saveList([]);
        this.updateUI();
    }
};

document.addEventListener('DOMContentLoaded', () => {
    CompareManager.updateUI();

    document.addEventListener('click', (e) => {
        const btn = e.target.closest('.btn-compare');
        if (btn) {
            const id = parseInt(btn.dataset.id);
            CompareManager.addList(id);
        }
    });
});

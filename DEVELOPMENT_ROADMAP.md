# SEN-T PAZAR - Geliştirme Yol Haritası

Tarih: 29 Mart 2026  
Durum: Aktif  
Son Güncelleme: 29 Mart 2026

---

## 🎯 Milestone 1: Kullanıcı Deneyimi Temelleri

**Hedef:** Temel kullanıcı etkileşim özellikleri ekleme  
**Tahmini Süre:** 4-6 hafta  
**Öncelik:** 🔴 YÜKSEKFaydacılık

### Görevler

- [ ] **Favoriler Özelliği**
  - Kullanıcıların ilan listelerini kaydetmesi
  - Favorilor sayfası oluşturma
  - Veritabanı şeması: UserFavorites tablosu
  - UI: Heart icon, Favorites page
  - Estimatior: 2-3 gün

- [ ] **İlan Karşılaştırma Modülü**
  - 2-4 ilan arasında yan yana kıyaslama
  - Dinamik tablo layout
  - Filtrelenebilir öznitelikler
  - Estimatior: 3-4 gün

- [ ] **Doğrulanmış Satıcı İşareti (Badge Sistemi)**
  - Satıcı doğrulama workflow
  - Badge component (CSS + Icon)
  - Admin doğrulama paneli
  - Estimatior: 2 gün

- [ ] **Puanlama ve Yorum Sistemi**
  - 5-yıldız rating (UI component)
  - Yorum ekleme/düzenleme
  - Moderasyon kuyruğu
  - Veritabanı şeması: Reviews tablosu
  - Estimatior: 4-5 gün

**Bağlantılı Model:** `Review`, `UserFavorite`, `SellerVerification`  
**API Endpoints:** GET/POST /api/favorites, /api/reviews, /api/comparison

---

## 🎨 Milestone 2: Görsel Zenginleştirme

**Hedef:** İlan görselleri için ileri media desteği  
**Tahmini Süre:** 3-5 hafta  
**Öncelik:** 🟠 ORTA

### Görevler

- [ ] **360° Tur Desteği (Emlak İlanları)**
  - 360 viewer integrasyon (three.js veya Pannellum)
  - Emlak kategorisine özel kontrol paneli
  - Upload & processing workflow
  - Estimatior: 3-4 gün

- [ ] **Video Ekleme Opsiyonu**
  - Araç ve elektronik kategorilerine video support
  - Video thumbnail + preview
  - Video hosting (AWS S3 veya CDN entegrasyonu)
  - Estimatior: 3-4 gün

- [ ] **Hizmet İlanlarında Önce/Sonra Görselleri**
  - Bölünmüş görsel comparator UI
  - Drag slider component
  - Responsive layout
  - Estimatior: 2 gün

**Frontend Kütüphaneleri:** three.js, Pannellum, swiper.js  
**Backend:** Video processing (FFmpeg), CDN integration

---

## 💼 Milestone 3: Kurumsal Paketler ve Profiller

**Hedef:** B2B paketleme ve satıcı profil zenginleştirme  
**Tahmini Süre:** 3-4 hafta  
**Öncelik:** 🟠 ORTA

### Görevler

- [ ] **Paket Farklarını İkonlarla Netleştir**
  - Pricing page redesign
  - Icon library (pricing icons)
  - Feature comparison grid
  - Estimatior: 2-3 gün

- [ ] **Deneme Süresi Ekle (7 gün Ücretsiz)**
  - Trial logic: TrialPeriod field in subscription
  - Free tier management
  - Trial expiry notifications
  - Estimatior: 2 gün

- [ ] **Mağaza Profillerine Referans ve Müşteri Yorumları**
  - Store profile page enhancement
  - Customer testimony section
  - Reference gallery
  - Estimatior: 3 gün

- [ ] **Sponsorlu Alanlarda Sayaçlı Kampanya Banner'ları**
  - Campaign banner component
  - View/Click counter
  - Campaign dashboard
  - Analytics tracking
  - Estimatior: 3-4 gün

**Veritabanı Şeması:**  
- `CorporatePackages` (PackageType, Features)
- `TrialSubscriptions` (UserId, StartDate, EndDate)
- `StoredProfiles` (Testimonials, References)
- `CampaignBanners` (Views, Clicks, Schedule)

---

## 🔌 Milestone 4: B2B Entegrasyonlar

**Hedef:** Kurumsal partner entegrasyonları  
**Tahmini Süre:** 6-8 hafta  
**Öncelik:** 🔴 YÜKSEK

### Görevler

- [ ] **Lojistik Firmaları ile Entegrasyon**
  - Partnership API endpoints
  - Shipping calculator integration
  - Carrier management dashboard
  - Tracking system
  - Estimatior: 4-5 gün

- [ ] **Ödeme Altyapısı (Güvenli Gateway)**
  - Stripe/2Checkout/Local gateway
  - PCI-DSS compliance
  - Webhook handling
  - Transaction logging
  - Estimatior: 5-6 gün

- [ ] **Sigorta Opsiyonları**
  - Araç ve emlak kategorilerine sigorta ekle
  - Partner sigorta şirketleri (Allianz, Vakıf, etc.)
  - Policy calculator
  - Estimatior: 4-5 gün

- [ ] **Kurumsal API Dokümantasyonu**
  - OpenAPI 3.0 spec (Swagger)
  - SDK examples (C#, JS, Python)
  - Rate limiting & auth
  - Developer portal
  - Estimatior: 3 gün

**Teknoloji Stack:**  
- Payment: Stripe API, OAuth 2.0
- Logistics: Custom webhook handlers
- API Docs: Swagger/OpenAPI
- Security: JWT tokens, API keys

---

## 🤖 Milestone 5: Uzun Vadeli Ekosistem

**Hedef:** Yapay zeka ve ölçeklenebilir hizmet modeli  
**Tahmini Süre:** 8-12 hafta  
**Öncelik:** 🟢 DÜŞÜK (Gelecek Dönem)

### Görevler

- [ ] **Kullanıcıya Özel Öneri Motoru (AI)**
  - ML model: Listing recommendations
  - Personalization engine
  - Training data pipeline
  - A/B testing framework
  - Estimatior: 5-7 gün

- [ ] **Abonelik Bazlı Hizmet Modeli**
  - Subscription plans (Starter, Pro, Enterprise)
  - Billing cycle management
  - Upgrade/downgrade logic
  - Invoice generation (PDF)
  - Estimatior: 4-5 gün

- [ ] **Mobil Uygulama Optimizasyonu**
  - Responsive design audit
  - Progressive Web App (PWA) setup
  - Offline capabilities
  - Push notifications
  - Estimatior: 6-7 gün

- [ ] **Bölgesel Kampanya Entegrasyonu (KKTC Odaklı)**
  - Geolocation targeting
  - Regional campaign manager
  - Localization (Turkish, English)
  - Regional analytics dashboard
  - Estimatior: 3-4 gün

**Teknoloji Stack:**  
- AI/ML: Python, scikit-learn, TensorFlow
- PWA: Service Workers, Web App Manifest
- Notifications: Firebase Cloud Messaging
- Analytics: Custom dashboard, event tracking

---

## 📊 İlerleme Özeti

| Milestone | Görev Sayısı | Tamamlanmış | % | Tahmini Süre |
|-----------|-------------|------------|---|----------------|
| 1 | 4 | 0 | 0% | 4-6 hafta |
| 2 | 3 | 0 | 0% | 3-5 hafta |
| 3 | 4 | 0 | 0% | 3-4 hafta |
| 4 | 4 | 0 | 0% | 6-8 hafta |
| 5 | 4 | 0 | 0% | 8-12 hafta |
| **TOPLAM** | **19** | **0** | **0%** | **24-35 hafta** |

---

## 🔗 İlgili Dosyalar

- 📋 [Proje Yönetimi](PROJECT_MANAGEMENT.md) - Günlük görev takibi
- 📚 [API Dokümantasyonu](docs/API.md) - Endpoint referansları
- 🏗️ [Mimarı Genel Bakış](ARCHITECTURE.md) - Sistem tasarımı
- 📝 [CHANGELOG](CHANGELOG.md) - Sürüm geçmişi

---

## 📞 İletişim ve Koordinasyon

- **Proje Yöneticisi:** [Ad Soyad]
- **Tech Lead:** [Ad Soyad]
- **Tasarım Lead:** [Ad Soyad]
- **Sprint Cycle:** 2 hafta

---

**Son Güncelleme:** 29 Mart 2026 | **Sonraki Gözden Geçirme:** 5 Nisan 2026

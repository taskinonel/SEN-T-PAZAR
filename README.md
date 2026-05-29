# Sen-T Pazar Platformu

C# ve ASP.NET Core MVC ile gelistirilmiş, tarayıcı ve platform bağımsız çok kategorili ilan platformu örneği.

## 📋 İçindekiler
- [Özellikler](#özellikler)
- [Teknoloji](#teknoloji)
- [Geliştirme Yol Haritası](#geliştirme-yol-haritası)
- [Kurulum](#kurulum)
- [Proje Yapısı](#proje-yapısı)
- [Ödeme Sistemleri](#odeme-sistemleri)

## ✨ Özellikler
- Türkçe, İngilizce, Rusça ve Arapça çoklu dil desteği
- Arapça için RTL (sağdan sola) yerleşim desteği
- Emlak, vasıta, elektronik, iş makineleri ve diğer kategoriler
- Satılık ve kiralık ilan akışı
- Kredi kartı, havale ve EFT ödeme yöntemleri
- Menü ve arama paneli üzerinden filtreleme (tip, şehir, kategori, bütçe, anahtar kelime)
- Ana sayfada satılık, kiralık, günlük, hizmet, özel ders, iş ve yardımcı akışları için ayrı arama sekmeleri
- İlan detay sayfası
- Doğrulamalı "İlan Ver" formu (server-side validation)
- Admin panelinde kategori / alt kategori / ilan tipi drift raporu
- Modern CSS tema (Primary: #007ACC, Secondary: #FF6F00)

## 🛠️ Teknoloji
- **.NET:** ASP.NET Core 8.0 MVC
- **Frontend:** Bootstrap 5, jQuery 3.6+, Swiper.js
- **Database:** SQL Server, Entity Framework Core
- **Styling:** Custom CSS3 + Design Tokens (CSS Variables)

## 📚 Geliştirme Yol Haritası

Detaylı roadmap ve görev listesi için bkz:
- 📄 [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md) - 5 Milestone geliştirim planı
- 📖 [.github/DEVELOPMENT.md](.github/DEVELOPMENT.md) - Teknik rehber
- 📋 [SPRINT_TRACKER.md](SPRINT_TRACKER.md) - Sprint takibi

### Planned Milestones

| Milestone | Başlık | Durum | Tahmini Süre |
|-----------|--------|-------|--------------|
| 1 | Kullanıcı Deneyimi Temelleri | 📋 Planning | 4-6 hafta |
| 2 | Görsel Zenginleştirme | 📋 Planning | 3-5 hafta |
| 3 | Kurumsal Paketler | 📋 Planning | 3-4 hafta |
| 4 | B2B Entegrasyonlar | 📋 Planning | 6-8 hafta |
| 5 | Uzun Vadeli Ekosistem | 📋 Planning | 8-12 hafta |

## 🚀 Kurulum
```bash
# Repo'yu klonla
git clone https://github.com/sen-t-pazar/web.git
cd web

# Bağımlılıkları yükle
dotnet restore

# Database oluştur ve migrate et
dotnet ef database update

# Uygulamayı çalıştır
dotnet run
```

Uygulama varsayilan olarak şu adreste açılır:
- `http://localhost:5080` (Production)
- `http://localhost:5000` (Development)

## 📁 Proje Yapısı
```
Controllers/
  ├── HomeController.cs          # Ana sayfa
  ├── AccountController.cs       # Girişs/Kayıt
  ├── ListingController.cs       # İlan yönetimi
  └── AdminController.cs         # Admin paneli

Models/
  ├── HomePageViewModel.cs       # Ana sayfa view modeli
  ├── ApplicationDbContext.cs    # EF Core DbContext
  ├── Listing.cs                 # İlan modeli
  ├── ApplicationUser.cs         # Kullanıcı modeli
  └── Review.cs                  # [Gelecek] Yorum modeli

Views/
  ├── Home/Index.cshtml          # Ana sayfa
  ├── Account/Login.cshtml       # Giriş
  ├── Shared/_Layout.cshtml      # Layout şablonu
  └── Shared/_CarouselPartial.cshtml  # Carousel

wwwroot/
  ├── css/site.css               # Tema stilleri
  ├── js/                        # Frontend scripts
  └── img/                       # Resimler

Migrations/
  └── [veritabanı versiyonları]  # EF Core migrations
```

## 🔧 Geliştirme Notları

## 🧰 Bakım / Deploy sırasında veri kaybını önleme

- **SQLite veritabanı (Production):** `ConnectionStrings:DefaultConnection` içindeki `Data Source` mutlak yol olmalı ve uygulama dizininin dışında tutulmalı. Uygulama artık Production ortamında SQLite DB dosyası bulunamazsa **boş DB oluşturup “kayıtlar kaybolmuş gibi” görünmesini engellemek için** açılışı durdurur.
  - İlk kurulum için istisna: `Database:AllowCreateIfMissing=true` (sadece ilk kurulum/sıfırdan kurulumda kullanın).
- **Yüklenen dosyalar:** Görseller/logolar artık uygulama klasörünün dışında tutulur. Varsayılan local yol `../uploads`, production yolu `/var/www/sentpazar/uploads` olarak yapılandırılmıştır. Uygulama bu klasörü `/uploads` URL'i altında ayrı static file provider ile servis eder.
- **Önerilen deploy akışı:** [tools/deploy-live.ps1](tools/deploy-live.ps1)
  - Uzakta eski `wwwroot/uploads` içeriğini yeni harici uploads köküne taşır/eşitler
  - Uzakta DB ve uploads yedeği alır (retention ile)
  - Deploy sonrası günlük backup timer'ı ve 30 dakikada bir çalışan disk kontrol timer'ı kurar
  - Deploy öncesi/sonrası kayıt sayısı snapshot’ını kontrol eder
- **Sunucu bakım zamanlayıcıları:**
  - `sentpazar-backup.timer`: her gün DB + uploads yedeği alır
  - `sentpazar-disk-check.timer`: her 30 dakikada bir disk doluluğunu kontrol eder, eşik aşılırsa journal'a alarm yazar ve `/var/backups/sentpazar/alerts/disk-alert-latest.log` dosyasını günceller
  - İsteğe bağlı e-posta alarmı için sunucuda `/etc/sentpazar-maintenance.env` içindeki `ALERT_EMAIL` alanı doldurulabilir
- **Yerel DB yedeği (SQLite):** [tools/backup-sqlite.ps1](tools/backup-sqlite.ps1)


### Mevcut Durum (v1.0)
- ✅ Responsive landing page
- ✅ Çoklu dil desteği
- ✅ Temel kategori ve filtreleme
- ✅ İlan detay sayfası
- ⏳ Veritabanı entegrasyonu (EF Core yapılandırıldı)

### Yapılacaklar
- [ ] Favoriler sistemi (M1-01)
- [ ] İlan karşılaştırma (M1-02)
- [ ] Yorum/Rating sistemi (M1-04)
- [ ] 360° emlak tur (M2-01)
- [x] Payment integration (M4-02)
- [ ] Mobile app (M5-03)

Detaylı görev listesi [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md)'de bulunabilir.

## 🔐 Güvenlik
- Server-side form validation
- CSRF protection (ASP.NET Core built-in)
- SQL injection prevention (Entity Framework)
- XSS protection (Razor templates)
- Admin hesaplar icin giriste iki adimli dogrulama (e-posta kodu)
- Admin paneli kritik islemlerinde audit log kaydi

### Gizli Anahtarlar
Google ve SMTP gibi gizli bilgiler appsettings dosyalarina yazilmamalidir.

PowerShell ile ortam degiskeni tanimlama ornekleri:

```powershell
$env:Authentication__Google__ClientId="YOUR_GOOGLE_CLIENT_ID"
$env:Authentication__Google__ClientSecret="YOUR_GOOGLE_CLIENT_SECRET"
$env:Smtp__Pass="YOUR_SMTP_APP_PASSWORD"
$env:Payments__Stripe__SecretKey="YOUR_STRIPE_SECRET_KEY"
$env:Payments__Stripe__WebhookSecret="YOUR_STRIPE_WEBHOOK_SECRET"
$env:Payments__BankTransfer__AccountName="SEN-T SOFTWARE"
$env:Payments__BankTransfer__BankName="YOUR_BANK_NAME"
$env:Payments__BankTransfer__Iban="TR00 0000 0000 0000 0000 0000 00"
$env:Payments__BankTransfer__SwiftCode="AAAA TR IS"
dotnet run
```

Kalici tanimlamak icin:

```powershell
setx Authentication__Google__ClientId "YOUR_GOOGLE_CLIENT_ID"
setx Authentication__Google__ClientSecret "YOUR_GOOGLE_CLIENT_SECRET"
setx Smtp__Pass "YOUR_SMTP_APP_PASSWORD"
setx Payments__Stripe__SecretKey "YOUR_STRIPE_SECRET_KEY"
setx Payments__Stripe__WebhookSecret "YOUR_STRIPE_WEBHOOK_SECRET"
setx Payments__BankTransfer__AccountName "SEN-T SOFTWARE"
setx Payments__BankTransfer__BankName "YOUR_BANK_NAME"
setx Payments__BankTransfer__Iban "TR00 0000 0000 0000 0000 0000 00"
setx Payments__BankTransfer__SwiftCode "AAAA TR IS"
```

## API ve Mobil CORS

Mobil uygulama/API istemcileri icin CORS ayarlari appsettings uzerinden yonetilir:

```json
"Cors": {
  "AllowedOrigins": [
    "https://www.sen-t.com",
    "https://sen-t.com"
  ]
}
```

Notlar:
- Tarayici tabanli istemcilerde CORS kontrolu `Origin` basligina gore yapilir.
- `SENTPAZAR-Android-App` User-Agent ile gelen API isteklerinde preflight uyumlulugu icin ek izin basliklari uygulanir.

Kayitli arama bildirim job ayarlari:

```json
"SavedSearchNotifications": {
  "Enabled": true,
  "IntervalMinutes": 30
}
```

Mobil API (v1) temel uç noktaları:
- `POST /api/v1/Account/Login`
- `POST /api/v1/Account/Register`
- `POST /api/v1/Account/DeviceToken` (JWT ile, FCM token kaydı)
- `GET /api/v1/Account/Profile`
- `GET /api/v1/Account/MyAds`
- `GET /api/v1/Account/Messages` (thread bazlı mesaj listesi)
- `POST /api/v1/Account/Messages/Reply` (mesaja cevap)
- `GET /api/v1/Ads`
- `GET /api/v1/Ads/{id}`
- `GET /api/v1/Ads/Suggest?q=...`
- `POST /api/v1/Ads` (ilan oluşturur, `pending` durumunda)
- `POST /api/v1/Ads/Upload` (çoklu görsel yükleme)
- `DELETE /api/v1/Ads/{id}` (ilan sahibi için)
- `GET /api/v1/Categories`
- `GET /api/v1/Locations`
- `GET /api/v1/Showcase`
- `GET /api/v1/Favorites`
- `POST /api/v1/Favorites/Add`

Notlar:
- Mobil JWT zorunlu uçlarda `Authorization: Bearer <token>` başlığı kullanılmalıdır.
- `POST /api/v1/Ads/Upload` dönüşündeki URL'ler tam adres olarak döner ve ilan kaydında doğrudan kullanılabilir.
- Push bildirimleri için `Fcm:ServerKey` ayarı doldurulmalı, mobil uygulama giriş sonrası `DeviceToken` endpointine cihaz tokenı göndermelidir.
- `GET /api/v1/Categories`, `GET /api/v1/Locations` ve `GET /api/v1/Ads` yanıtlarında ETag döner; istemci `If-None-Match` ile 304 alabilir.
- Android App Links için `.well-known/assetlinks.json` dosyası eklendi; `package_name` ve `sha256_cert_fingerprints` değerleri release sertifikanıza göre güncellenmelidir.
- API katmanında 4xx/5xx yanıtlar loglanır ve `api.requests.total`, `api.requests.4xx`, `api.requests.5xx` metrik sayaçları üretilir.

## Odeme Sistemleri

Aktif odeme yontemleri:
- Kredi karti (Stripe Checkout)
- Havale
- EFT

Not: Production ortaminda kredi karti odemesi sadece `sk_live_` ile baslayan Stripe canli anahtari ile acilir.

Canli ayar ve 3 yontem icin uctan uca test adimlari:
- [docs/PAYMENT_OPERATIONS.md](docs/PAYMENT_OPERATIONS.md)

Canli odeme ayarlarini tek adimda yazmak icin:
- `tools/set-production-payment-env.ps1`

Veritabani yedegi icin (Sqlite):
- `tools/backup-sqlite.ps1`

Canliya tek komut deploy (publish + upload + service restart + health check):
- `tools/deploy-live.ps1`

Ornek kullanim:
```powershell
powershell -ExecutionPolicy Bypass -File .\tools\deploy-live.ps1 `
  -SshKeyPath "C:\Users\MONSTER\Downloads\ssh-key-2026-04-08.key" `
  -ServiceName "sentpazar" `
  -RemoteAppDir "/var/www/sentpazar/app"
```

Testler:
```powershell
dotnet test .\tests\SenTPazar.Tests\SenTPazar.Tests.csproj
```

## 📊 Lisans
MIT License - Detaylar için [LICENSE](LICENSE) dosyasına bakınız.

## 👥 Katkı Süreci
1. Fork repository
2. Feature branch oluştur (`git checkout -b feature/amazing-feature`)
3. Commit et (`git commit -m 'Add amazing feature'`)
4. Push et (`git push origin feature/amazing-feature`)
5. Pull Request aç

Detaylı geliştirme süreci: [.github/DEVELOPMENT.md](.github/DEVELOPMENT.md#geliştirme-süreci)

## 📞 İletişim ve Destek
- 📧 Issues: [GitHub Issues](https://github.com/sen-t-pazar/web/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/sen-t-pazar/web/discussions)
- 📖 Dokümantasyon: [docs/](docs/) klasörü

---

**Durum:** Aktif Geliştirme | **Son Güncelleme:** 29 Mart 2026

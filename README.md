# Sen-T Pazar Platformu

C# ve ASP.NET Core MVC ile gelistirilmiş, tarayıcı ve platform bağımsız çok kategorili ilan platformu örneği.

## 📋 İçindekiler
- [Özellikler](#özellikler)
- [Teknoloji](#teknoloji)
- [Geliştirme Yol Haritası](#geliştirme-yol-haritası)
- [Kurulum](#kurulum)
- [Proje Yapısı](#proje-yapısı)

## ✨ Özellikler
- Türkçe, İngilizce, Rusça ve Arapça çoklu dil desteği
- Arapça için RTL (sağdan sola) yerleşim desteği
- Emlak, vasıta, elektronik, iş makineleri ve diğer kategoriler
- Satılık ve kiralık ilan akışı
- Menü ve arama paneli üzerinden filtreleme (tip, şehir, kategori, bütçe, anahtar kelime)
- İlan detay sayfası
- Doğrulamalı "İlan Ver" formu (server-side validation)
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
- [ ] Payment integration (M4-02)
- [ ] Mobile app (M5-03)

Detaylı görev listesi [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md)'de bulunabilir.

## 🔐 Güvenlik
- Server-side form validation
- CSRF protection (ASP.NET Core built-in)
- SQL injection prevention (Entity Framework)
- XSS protection (Razor templates)

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

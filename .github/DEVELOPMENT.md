# SEN-T PAZAR - Geliştirme Rehberi

## 📋 İçindekiler

1. [Başlama](#başlama)
2. [Geliştirme Süreci](#geliştirme-süreci)
3. [Milestone Detayları](#milestone-detayları)
4. [Teknoloji Yığını](#teknoloji-yığını)
5. [Git Workflow](#git-workflow)
6. [Testing Stratejisi](#testing-stratejisi)

---

## Başlama

### Teknoloji Gereksinmeleri

```
- .NET 8.0+
- SQL Server 2019+
- Node.js 18+ (Frontend tooling)
- Git 2.30+
```

### Kurulum

```bash
# Repo'yu klonla
git clone https://github.com/sen-t-pazar/web

# Bağımlılıkları yükle
dotnet restore

# Database oluştur
dotnet ef database update

# Uygulamayı çalıştır
dotnet run
```

---

## Geliştirme Süreci

### 1. Feature Branch Oluşturma

```bash
git checkout -b feature/favoriler-sistemi
# veya
git checkout -b feature/M1-01-favoriler
```

**Naming Convention:**
- `feature/[milestone]-[number]-[description]`
- `bugfix/[issue-number]-[description]`
- `docs/[description]`

### 2. Görev Başlama

1. **DEVELOPMENT_ROADMAP.md'de** kutucuğu işaretle
2. **Branch başlat** ve push et
3. **Draft PR** aç
4. **Günlük commit** yap

### 3. Commit Mesajları

```
[M1-01] Favoriler veritabanı şeması eklendi
[M1-01] Favoriler UI component oluşturuldu
[M1-01] Favoriler API endpoints implemented
```

### 4. Pull Request Süreci

1. ✅ Tests pass locally
2. ✅ Code review (peer)
3. ✅ Merge to develop
4. ✅ Update DEVELOPMENT_ROADMAP.md

---

## Milestone Detayları

### Milestone 1: Kullanıcı Deneyimi Temelleri [24-42 gün]

#### 1.1 Favoriler Özelliği

**Veritabanı:**
```sql
CREATE TABLE UserFavorites (
    Id INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(450) NOT NULL,
    ListingId INT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (ListingId) REFERENCES Listings(Id)
);
```

**C# Model:**
```csharp
public class UserFavorite
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public int ListingId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ApplicationUser User { get; set; }
    public Listing Listing { get; set; }
}
```

**API Endpoints:**
```
POST   /api/favorites/{listingId}        - İlan favorilere ekle
DELETE /api/favorites/{listingId}        - İlandan favorileri çıkar
GET    /api/favorites                    - Tüm favori ilanları getir
GET    /api/favorites/{listingId}/check  - İlan favoride mi kontrol et
```

**Frontend:**
```html
<!-- Heart Icon Button -->
<button class="btn-favorite" data-listing-id="123">
    <i class="icon-heart"></i> Favorilerime Ekle
</button>

<!-- Favorites Page -->
<div class="favorites-grid">
    <!-- Dinamik listeleme -->
</div>
```

#### 1.2 İlan Karşılaştırma Modülü

**Endpoint:**
```
POST /api/comparison - Ilanları karşılaştır (max 4)
GET  /api/comparison/attributes - Karşılaştırılabilir öznitelikler
```

**Örnek Response:**
```json
{
  "listings": [
    { "id": 1, "title": "...", "price": 100000 },
    { "id": 2, "title": "...", "price": 120000 }
  ],
  "attributes": [
    { "key": "price", "label": "Fiyat", "values": [100000, 120000] },
    { "key": "area", "label": "Alan", "values": [150, 180] }
  ]
}
```

#### 1.3 Doğrulanmış Satıcı İşareti

**Veritabanı:**
```sql
CREATE TABLE SellerVerifications (
    Id INT PRIMARY KEY IDENTITY,
    SellerId NVARCHAR(450) NOT NULL UNIQUE,
    VerificationStatus VARCHAR(20), -- Pending, Verified, Rejected
    VerifiedAt DATETIME,
    VerifiedBy NVARCHAR(450),
    Notes NVARCHAR(MAX),
    FOREIGN KEY (SellerId) REFERENCES AspNetUsers(Id)
);
```

**Badge CSS:**
```css
.verified-badge {
    display: inline-flex;
    align-items: center;
    gap: 4px;
    background: #4CAF50;
    color: white;
    padding: 4px 8px;
    border-radius: 12px;
    font-size: 12px;
    font-weight: bold;
}
```

#### 1.4 Puanlama ve Yorum Sistemi

**Veritabanı:**
```sql
CREATE TABLE Reviews (
    Id INT PRIMARY KEY IDENTITY,
    ListingId INT NOT NULL,
    ReviewerId NVARCHAR(450) NOT NULL,
    Rating INT CHECK (Rating >= 1 AND Rating <= 5),
    Title NVARCHAR(200),
    Comment NVARCHAR(MAX),
    Status VARCHAR(20), -- Pending, Approved, Rejected
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME,
    FOREIGN KEY (ListingId) REFERENCES Listings(Id),
    FOREIGN KEY (ReviewerId) REFERENCES AspNetUsers(Id)
);
```

**API Endpoints:**
```
POST   /api/listings/{id}/reviews      - Yorum ekle
GET    /api/listings/{id}/reviews      - Yorumları getir (paginated)
PUT    /api/reviews/{id}               - Yorumu düzenle
DELETE /api/reviews/{id}               - Yorumu sil
GET    /api/listings/{id}/rating       - Ortalama rating
```

---

### Milestone 2: Görsel Zenginleştirme [21-35 gün]

#### 2.1 360° Tur (Emlak)

**Kütüphaneler:**
- Pannellum.js (açık kaynak, hafif)
- three.js (gelişmiş kontrol için)

**Örnek Implementasyon:**
```html
<div id="panorama">
    <noscript>
        <img src="/images/panorama.jpg" alt="360 Tour">
    </noscript>
</div>

<script>
pannellum.viewer('panorama', {
    "default": { "firstScene": "scene1" },
    "scenes": {
        "scene1": {
            "title": "Living Room",
            "panorama": "/media/tour-360-1.jpg",
            "hotSpots": [...]
        }
    }
});
</script>
```

#### 2.2 Video Ekleme

**Yükleme Sürecü:**
1. Frontend: Dosya seçimi → Thumbnail oluştur
2. Backend: S3'e upload → Video processing (FFmpeg)
3. Storage: Processed video + thumbnail
4. Serving: CDN üzerinden streaming

**API:**
```
POST /api/listings/{id}/videos     - Video upload
GET  /api/videos/{id}              - Video metadata + stream URL
DELETE /api/videos/{id}            - Video sil
```

#### 2.3 Önce/Sonra Görselleri (Hizmetler)

**Component:**
```html
<div class="before-after-slider">
    <img src="before.jpg" alt="Before" class="before-image">
    <img src="after.jpg" alt="After" class="after-image">
    <input type="range" min="0" max="100" value="50" class="slider">
</div>
```

---

### Milestone 3: Kurumsal Paketler [21-28 gün]

**Paket Tablosu:**

| Paket | Aylık İlan | Sponsorlu Spot | API Access | Fiyat |
|-------|-----------|---------------|-----------|-------|
| Starter | 3 | - | - | TRY 99 |
| Professional | 20 | 1 | - | TRY 299 |
| Enterprise | Unlimited | 5 | ✓ | TRY 999 |
| Trial | 5 | 1 | - | Ücretsiz (7 gün) |

**Veritabanı:**
```sql
CREATE TABLE CorporatePackages (
    Id INT PRIMARY KEY IDENTITY,
    PackageName NVARCHAR(50),
    MonthlyListings INT,
    SponsoredSpots INT,
    HasAPIAccess BIT,
    MonthlyPrice DECIMAL(10,2),
    TrialDays INT DEFAULT 0
);

CREATE TABLE UserSubscriptions (
    Id INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(450) NOT NULL,
    PackageId INT NOT NULL,
    StartDate DATETIME DEFAULT GETDATE(),
    EndDate DATETIME,
    IsTrialPeriod BIT,
    PaymentStatus VARCHAR(20), -- Active, Expired, Cancelled
    FOREIGN KEY (PackageId) REFERENCES CorporatePackages(Id)
);
```

---

### Milestone 4: B2B Entegrasyonlar [42-56 gün]

#### 4.1 Ödeme Gateway (Stripe)

```csharp
// Stripe Setup
var service = new ChargeService();
var options = new ChargeCreateOptions
{
    Amount = (long)(price * 100), // Cents
    Currency = "try",
    Source = stripeToken,
    Description = "SEN-T PAZAR Package Subscription"
};
var charge = service.Create(options);
```

#### 4.2 Lojistik Integration

```csharp
// Logistics API Call
var shippingRate = await LogisticsService.CalculateRate(
    origin: listing.Location,
    destination: userLocation,
    weight: item.Weight,
    carrier: "AramEx" // or DHL, FedEx, etc.
);
```

#### 4.3 Insurance Options

**Veritabanı:**
```sql
CREATE TABLE InsuranceOptions (
    Id INT PRIMARY KEY IDENTITY,
    ListingId INT NOT NULL,
    InsuranceType VARCHAR(50), -- Vehicle, Property, General
    Provider NVARCHAR(100), -- Allianz, Vakıf, etc.
    CoverageAmount DECIMAL(15,2),
    PremiumPerMonth DECIMAL(10,2),
    Status VARCHAR(20),
    FOREIGN KEY (ListingId) REFERENCES Listings(Id)
);
```

---

## Teknoloji Yığını

### Backend
```
ASP.NET Core 8.0
Entity Framework Core
SQL Server 2019+
MediatR (CQRS pattern)
Serilog (Logging)
```

### Frontend
```
Bootstrap 5
jQuery 3.6+
AJAX (Form submissions)
Chart.js (Analytics)
Swiper.js (Carousels)
```

### DevOps
```
Docker
GitHub Actions
Azure App Service
Azure SQL Database
Azure CDN
```

---

## Git Workflow

```
main (production)
  ↑
  ├─ develop (staging)
      ↑
      ├─ feature/M1-01-favoriler
      ├─ feature/M1-02-comparison
      └─ bugfix/search-filter-issue
```

**Release Süreci:**
```bash
# 1. Develop'den release branch oluştur
git checkout -b release/v1.2.0 develop

# 2. Version güncellemeleri yap
# - .csproj version
# - package.json version
# - CHANGELOG.md

# 3. Merge to main
git checkout main
git merge --no-ff release/v1.2.0
git tag -a v1.2.0 -m "Release version 1.2.0"

# 4. Back to develop
git checkout develop
git merge --no-ff release/v1.2.0
```

---

## Testing Stratejisi

### Unit Tests (Xunit)
```csharp
[Fact]
public void AddFavorite_ValidListing_ReturnSuccess()
{
    // Arrange
    var userId = "user123";
    var listingId = 1;
    
    // Act
    var result = _favoriteService.AddFavorite(userId, listingId);
    
    // Assert
    Assert.True(result.IsSuccess);
}
```

### Integration Tests
- Database migration tests
- API endpoint tests
- Authentication tests

### Performance Tests
- Load testing (JMeter)
- Lighthouse audits
- CDN performance

---

## Kaynaklar

- 📖 [API Dokümantasyonu](../docs/API.md)
- 📋 [Proje Şablonları](../docs/TEMPLATES.md)
- 🔒 [Security Guidelines](../docs/SECURITY.md)
- 🎨 [Design System](../docs/DESIGN.md)

---

**Sürüm:** 1.0  
**Son Güncelleme:** 29 Mart 2026  
**Sonraki Gözden Geçirme:** 12 Nisan 2026

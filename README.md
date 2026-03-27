# Sen-T Pazar Platformu

C# ve ASP.NET Core MVC ile gelistirilmiş, tarayıcı ve platform bağımsız çok kategorili ilan platformu örneği.

## Özellikler
- Türkçe, İngilizce, Rusça ve Arapça çoklu dil desteği
- Arapça için RTL (sağdan sola) yerleşim desteği
- Emlak, vasıta, elektronik, iş makineleri ve diğer kategoriler
- Satılık ve kiralık ilan akışı
- Menü ve arama paneli üzerinden filtreleme (tip, şehir, kategori, bütçe, anahtar kelime)
- İlan detay sayfası
- Doğrulamalı "İlan Ver" formu (server-side validation)

## Teknoloji
- .NET 10
- ASP.NET Core MVC
- Razor View Engine
- Bootstrap (altyapı) + özel CSS tema

## Proje Yapisi
- `Controllers/HomeController.cs`: Ana sayfa verisi ve aksiyonlar
- `Models/HomePageViewModel.cs`: Ana sayfa view modeli
- `Models/PropertyCard.cs`: Ilan kart modeli
- `Views/Home/Index.cshtml`: Ana sayfa tasarimi
- `Views/Shared/_Layout.cshtml`: Ortak sayfa iskeleti
- `wwwroot/css/site.css`: Tema ve responsive stiller

## Calistirma
```bash
dotnet restore
dotnet build
dotnet run
```

Uygulama varsayilan olarak su adreste acilir:
- `http://localhost:5080`

## Geliştirme Notları
- Veriler şu an örnek (mock) olarak denetleyicide oluşturuluyor.
- Sonraki adımda SQL Server/PostgreSQL + Entity Framework Core eklenebilir.
- Form verisi kalıcı olarak saklanmıyor; onay akışına geçmek için veritabanı eklenmeli.

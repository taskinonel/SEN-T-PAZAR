# Odeme Operasyon Rehberi

Bu dokuman kredi karti, havale ve EFT odeme yontemlerini canli ortamda guvenli sekilde acmak ve gercek odeme operasyonunu yonetmek icin hazirlanmistir.

## 1) Canli Yapi Landirma

### 1.1 Kredi Karti (Stripe)
Asagidaki ayarlar ortam degiskeni veya gizli konfigurasyon kaynagi ile verilmelidir:

- `Payments__Stripe__SecretKey`
- `Payments__Stripe__WebhookSecret`
- `Payments__Stripe__SuccessUrl`
- `Payments__Stripe__CancelUrl`

Not: Bu degerleri appsettings icine duz yazi olarak yazmayin.

Canli sunucuda tek adimda ayar yapmak icin:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\set-production-payment-env.ps1 \
	-StripeSecretKey "sk_live_xxx" \
	-StripeWebhookSecret "whsec_xxx" \
	-StripeSuccessUrl "https://www.sen-t.com/Pricing/PaymentReturn?status=success" \
	-StripeCancelUrl "https://www.sen-t.com/Pricing/PaymentReturn?status=cancel" \
	-BankAccountName "SEN-T SOFTWARE" \
	-BankName "BANKA ADI" \
	-Iban "TR00 0000 0000 0000 0000 0000 00" \
	-SwiftCode "AAAA TR IS"
```

Onemli: Uygulama production modda acilirken canli odeme ayarlari eksikse baslamaz. Bu, yarim ayarla canliya cikisi engellemek icin bilerek eklenmistir.

### 1.2 Havale / EFT Banka Bilgileri
Asagidaki ayarlar havale ve EFT ekranlarinda kullanilir:

- `Payments__BankTransfer__AccountName`
- `Payments__BankTransfer__BankName`
- `Payments__BankTransfer__Iban`
- `Payments__BankTransfer__SwiftCode`

Ornek (PowerShell):

```powershell
$env:Payments__BankTransfer__AccountName="SEN-T SOFTWARE"
$env:Payments__BankTransfer__BankName="BANKA ADI"
$env:Payments__BankTransfer__Iban="TR00 0000 0000 0000 0000 0000 00"
$env:Payments__BankTransfer__SwiftCode="AAAA TR IS"
```

### 1.3 Stripe Webhook
Stripe panelinde webhook endpoint olarak su adresi tanimlanmalidir:

- `/Pricing/StripeWebhook`

Olay tipi:

- `checkout.session.completed`

## 2) Uctan Uca Canli Odeme Senaryolari

## Senaryo A - Kredi Karti (Basarili)
1. Kullanici giris yapar.
2. Paket satin alma ekraninda `Kredi Karti` secer.
3. Stripe Checkout ekranina yonlenir.
4. Gercek kart ile odemeyi tamamlar.
5. Uygulama `PaymentReturn` ile doner ve odeme `completed` olur.
6. Kullanici paket hakki aktif gorunur.

Beklenen sonuc:
- `Payments` tablosunda `payment_method=credit_card` ve `payment_status=completed`.
- `UserPackages` kaydi olusmus veya mevcut kayit guncellenmis olmali.

## Senaryo B - Havale (Onay Bekliyor)
1. Kullanici paket satin alma ekraninda `Havale` secer.
2. Sistem bekleyen odeme kaydi olusturur.
3. Kullaniciya referans kodu ve banka bilgileri gosterilir.
4. Admin panelinde odeme `pending` listesinde gorulur.
5. Admin onaylar.

Beklenen sonuc:
- `Payments` tablosunda `payment_method=bank_transfer`.
- Onay oncesi `pending`, onay sonrasi `completed`.
- Onay sonrasi kullanicinin paket hakki aktif olur.

## Senaryo C - EFT (Onay Bekliyor)
1. Kullanici paket satin alma ekraninda `EFT` secer.
2. Sistem bekleyen odeme kaydi olusturur.
3. Kullaniciya referans kodu ve banka bilgileri gosterilir.
4. Admin panelinde odeme `pending` listesinde gorulur.
5. Admin onaylar.

Beklenen sonuc:
- `Payments` tablosunda `payment_method=eft`.
- Onay oncesi `pending`, onay sonrasi `completed`.
- Onay sonrasi kullanicinin paket hakki aktif olur.

## 3) Operasyonel Kontrol Listesi

- Stripe canli anahtarlari tanimli mi?
- Webhook imzasi (`WebhookSecret`) dogru mu?
- Havale/EFT banka bilgileri dogru mu?
- Admin panelinde bekleyen odeme onay akisina erisim var mi?
- Satin alma ekraninda 3 yontem gorunuyor mu?

## 4) Sorun Giderme

- Kredi karti secenegi gorunuyor ama odeme baslamiyorsa: Stripe `SecretKey` dolu mu kontrol edin.
- Kredi karti secenegi kapaliysa: `SecretKey` degeri `sk_live_` ile basliyor mu kontrol edin.
- Webhook calismiyorsa: endpoint URL, event tipi ve webhook imzasini kontrol edin.
- Havale/EFT de paket aktif olmuyorsa: Admin `Payments` ekranindan odeme onayi verildi mi kontrol edin.

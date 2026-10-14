# Neon Pinball – App Store yükleme rehberi

## 1. Önce yap (bir kez)
1. GitHub'da `neon-pinball` adlı **public** repo aç; `docs/` içindeki 3 dosyayı (index/privacy/support.html) repo köküne koy.
2. Settings → Pages → Branch: main / root. Adres: https://ttahaklc.github.io/neon-pinball/privacy.html (oyunun içindeki bağlantı bu adrestir).
3. Üç dosyada `YOUR_SUPPORT_EMAIL` yerine destek e-postanı yaz (kişisel adresin görünmesin istiyorsan yeni bir adres aç).

## 2. App Store Connect → Yeni App
- Platform iOS · Ad: Neon Pinball (alınmışsa "Neon Pinball: Arcade" dene) · Birincil dil: English (U.S.) · Bundle ID: com.tahakilic.neonpinball · SKU: neonpinball-001
- Kategori: Games → birincil Arcade, ikincil Casual (alt kategori seçmen gerekmez)
- Fiyat: Free · Telif hakkı: © 2026 Taha Kılıç
- Metinler: `Store/AppStore/Metin/<dil>.txt` (7 dil; her dili sol menüden ekle). Alanlar karakter sınırı içinde.
- URL'ler: Support URL = …/support.html · Privacy Policy URL = …/privacy.html · Marketing URL (isteğe bağlı) = …/

## 3. Görseller
- Store/AppStore/Screenshots/iPhone_6.9/<dil>/ → "6.9-inch" alanına 1320×2868, sırayla 1–6
- Store/AppStore/Screenshots/iPad_13/<dil>/ → "13-inch iPad" alanına 2064×2752, sırayla 1–6
- Store/AppStore/Icon/AppStore_1024.png → Xcode'daki AppIcon zaten içinde; App Store için ayrıca yükleme gerekmez (build'den gelir).

## 4. App Privacy (Gizlilik)
"Data Not Collected" (Veri toplanmıyor) seç → Publish.

## 5. Yaş derecelendirmesi
Tüm sorulara **None / Hayır** (şiddet yok, kumar yok, UGC yok, web erişimi yok…). Sonuç 4+ çıkar.

## 6. Export Compliance
Info.plist'te `ITSAppUsesNonExemptEncryption = NO` zaten var; soru sorulmaz.

## 7. App Review Information
- Giriş gerekmez (hesap yok). 
- Notes: "Neon Pinball is an offline pinball game. No account, no login, no in-app purchases, no network use. Tap Play to start; tap or drag the plunger on the right to launch the ball. A How to Play guide is in Settings."
- İletişim: adın, telefonun, destek e-postan.

## 8. Build yükleme
1. Unity: Neon Pinball → Build iOS (menü) veya Build Profiles → iOS → Xcode'da `Unity-iPhone` aç, Signing: Team 5HSPFP8H55.
2. Cihaz yerine **Any iOS Device (arm64)** seç → Product → Archive → Distribute App → App Store Connect → Upload.
3. Birkaç dk sonra build App Store Connect'te (TestFlight) görünür; sürüm sayfasında "Build" ekle → Submit for Review.
4. Önerilir: önce TestFlight ile iç test.

## 9. Sonrası
Genelde 24–48 saat içinde inceleme olur. Ret gelirse nedenini yaz, düzeltelim.

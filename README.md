# Neon Pinball

Unity 6 (6000.5.3f1) ile yazılmış, URP 2D, tamamen çevrimdışı bir pinball oyunu. iOS ve Android için hazır.
Paket adı / Bundle ID: `com.tahakilic.neonpinball` · Sürüm 1.0 (build 1)

## 1. Projeyi açma
1. Unity Hub → **Installs** → `6000.5.3f1` kur. Modüller: **iOS Build Support** (yalnızca Mac), **Android Build Support** (+ OpenJDK + Android SDK & NDK Tools).
2. Bu repoyu klonla: `git clone <repo-adresi>`
3. Unity Hub → **Add → Add project from disk** → klasörü seç. İlk açılış birkaç dakika sürer (Library oluşur). Bu normal.
4. Sahne: `Assets/Scenes/Main.unity` (zaten Build Settings'te).

## 2. Android (Google Play) – .aab üretme
1. **File → Build Profiles → Android → Switch Platform.**
2. **Player Settings → Publishing Settings**: kendi upload keystore'unu seç (ya da Play Console "Play App Signing" ile yeni oluştur). Keystore dosyası repoda YOK, ayrıca verilir.
3. **Build App Bundle (Google Play)** kutusunu işaretle → **Build**. Çıktı `.aab` dosyasını Play Console'a yükle.
4. Mağaza yazıları, görseller ve yayın adımları: `Store/PlayStore/` ve `Store/PLAY_STORE_REHBERI.md`.

## 3. iOS (App Store) – yalnızca Mac
1. **File → Build Profiles → iOS → Switch Platform → Build** (boş bir klasöre, örn. `Builds/iOS`).
2. Çıkan klasörde `Unity-iPhone.xcodeproj`'u Xcode ile aç. **Signing & Capabilities**'ten kendi Team'ini seç.
3. Cihaz: *Any iOS Device (arm64)* → **Product → Archive → Distribute App → App Store Connect**.
4. Mağaza yazıları, görseller ve adımlar: `Store/AppStore/` ve `Store/APP_STORE_REHBERI.md`.
Not: Unity build sonrası otomatik olarak ikon, açılış ekranı ve şifreleme beyanı eklenir (`Assets/NeonPinball/Editor/IosPostBuild.cs`). Elle bir şey yapman gerekmez.

## 4. Gizlilik / destek sayfaları (zorunlu)
Her iki mağaza da herkese açık bir gizlilik politikası adresi ister. `docs/` içindeki 3 HTML dosyasını **public** bir GitHub reposunun köküne koyup Pages'i aç (örn. `https://ttahaklc.github.io/neon-pinball/privacy.html`). Oyunun içindeki "Gizlilik Politikası" bağlantısı bu adrese gider. Dosyalardaki `YOUR_SUPPORT_EMAIL` yerine destek e-postasını yaz.

## 5. Klasörler
- `Assets/NeonPinball/` oyunun tüm kodu (Runtime = oyun, Editor = build araçları, Tests = motor testleri)
- `Branding/` ikon ve açılış logosu kaynakları (build sırasında kullanılır)
- `Store/` mağaza metinleri (7 dil), ekran görüntüleri, ikonlar
- `docs/` gizlilik ve destek sayfaları

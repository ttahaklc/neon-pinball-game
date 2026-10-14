# Google Play yükleme rehberi – Neon Pinball

## Hazır malzeme (`Store/PlayStore/`)
- `Metin/<dil>.txt`: başlık (≤30), kısa açıklama (≤80), tam açıklama, sürüm notu — 7 dil (tr-TR, en-US, es-419, fr-FR, zh-CN, hi-IN, ar)
- `Screenshots/<dil>/`: 6 telefon ekran görüntüsü (1080×1920, 9:16)
- `icon_512.png` (uygulama simgesi 512×512), `feature_graphic_1024x500.png` (öne çıkan görsel)

## Adımlar
1. Play Console → **Uygulama oluştur**: ad *Neon Pinball*, varsayılan dil English (US), **Oyun**, **Ücretsiz**. Beyanları işaretle.
2. **Mağaza varlığı → Ana mağaza girişi**: en-US metinlerini yaz; simge, öne çıkan görsel ve telefon ekran görüntülerini yükle. Diğer 6 dili "Çeviri ekle" ile ekle.
3. **Kategori**: Oyun → Arcade. Etiket: Pinball, Arcade. İletişim e-postası: destek e-postası. Gizlilik politikası URL'si: README madde 4'teki adres.
4. **Uygulama içeriği** (hepsi zorunlu):
   - Gizlilik politikası: URL
   - Reklamlar: **Hayır, reklam içermiyor**
   - Uygulama erişimi: **Tüm işlevler özel erişim gerektirmeden kullanılabilir**
   - İçerik derecelendirme anketi: kategori *Oyun*; şiddet, cinsellik, kumar, kullanıcı içeriği, konum, satın alma → hepsi **Hayır** (sonuç: herkes için uygun)
   - Hedef kitle: **13 yaş ve üzeri** (çocuklara yönelik DEĞİL)
   - **Veri güvenliği**: "Veri toplanmıyor / paylaşılmıyor" seçeneklerini işaretle
   - Haber uygulaması: Hayır · COVID-19: Hayır · Devlet uygulaması: Hayır · Finansal özellikler: Hayır · Sağlık: Hayır
5. **Test ve yayın → Üretim → Yeni sürüm**: `.aab`'ı yükle, sürüm notunu yapıştır, incelemeye gönder.
   Yeni *kişisel* geliştirici hesaplarında üretime çıkmadan önce **kapalı testte en az 12 test kullanıcısıyla 14 gün** şartı vardır (Play Console "Test ve yayın" ekranında görünür). Hesap eskiyse gerekmez.
6. Play App Signing otomatik açık gelir; yüklediğin anahtar "upload key" olur.

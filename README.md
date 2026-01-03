# Liman Korku Oyunu (Storia) – İlk Gün Oynanış Prototipi

Bu proje, **Liman Korku Oyunu (Storia)** için hazırlanan **ilk gün odaklı oynanış prototipidir**.  
Amaç, oyunun ana karar verme mekaniğini erken aşamada test etmek ve ileride yazılacak Game Design Document (GDD) için teknik bir referans oluşturmaktır.

---

## Amaç

Bu prototipin amacı, oyunun tüm sistemlerini simüle etmek değil;  
oyuncunun **vinç operatörü rolünde konteynerleri değerlendirirken karar verme baskısı hissedip hissetmediğini** test etmektir.

Prototip şu temel soruya odaklanır:

> Oyuncu, sınırlı süre içinde eksik veya çelişkili bilgilerle karar vermekten gerilim duyuyor mu?

---

## Kapsam

Prototip yalnızca **oyunun 1. gününü** kapsar.

Aşağıdaki sistemler bilinçli olarak kapsam dışı bırakılmıştır:

- Tam oynanış süresi olan 7 günlük yapı
- Oyuncunun psikolojik sağlığını korumaya yönelik ilaç sistemi
- Psikolojik bozulma sonucu değişen oynanış dinamikleri
- Hayali yaratıklar ve aksiyon mekanikleri
- Ekonomi yönetimi ve illegal işler

Bu sistemlerin, ana karar verme mekaniği doğrulandıktan sonra eklenmesi planlanmaktadır.

---

## Oynanış Özeti

1. Oyuncu gece vardiyasında vinç kabinindedir.  
2. Limana gelen gemilerden konteynerler sırayla değerlendirilir.  
3. Her konteyner için oyuncu:
   - Sunulan bilgileri inceler,
   - Bilgilerin doğruluğunu değerlendirmeye çalışır,
   - Kabul veya Red kararı verir.
4. Kabul edilen konteynerler için oyuncu:
   - Konteynerin hangi gemiden alındığını,
   - Limandaki konteyner yerleştirme alanında hangi bölgeye bırakıldığını
   seçer.
5. Gün süresi dolduğunda oynanış sona erer.
6. Gün sonu değerlendirme ekranı gösterilir.

---

## Vinç Sistemi (Temsilî)

Bu prototipte vinç sistemi **gerçek fizik veya manuel kontrol içermez**.

- Vinç, oyuncuya operatör hissi vermek amacıyla kullanılır.
- Sistem seçim ve yerleştirme tabanlıdır.
- Arka planda fizik, ağırlık veya çarpışma hesaplaması yapılmaz.

Bu yaklaşım, karar verme mekaniğini öne çıkarmak ve teknik karmaşıklığı azaltmak için tercih edilmiştir.

---

## Konteyner Değerlendirme Mekaniği

Her konteyner için oyuncuya:

- Konteyner bilgileri (manifest / evrak),
- Konteynerin kendisine ait görsel ipuçları

sunulur.

Bu bilgiler:
- Her zaman tamamen doğru olmayabilir,
- Bazen eksik veya çelişkili olabilir.

Oyuncu, sahip olduğu mevcut bilgilerle **en doğru kararı vermeye çalışır**.

Bu mekanik ile amaçlanan, **bilgi belirsizliği altında karar verme deneyimini** test etmektir.

---

## Liman Konumlandırma Sistemi

Kabul edilen konteynerler için oyuncu:

- Hangi gemiden konteyner aldığını,
- Limandaki konteyner yerleştirme alanında hangi bölgeye bıraktığını

kendisi belirler.

Bu sistem:
- Şimdilik placeholder objelerle,
- Grid / bölge mantığıyla,
- Görsel geri bildirim sağlayacak şekilde

uygulanmıştır.

---

## Zaman Sistemi

- Prototip tek bir günü temsil eder.
- Gün süresi sınırlıdır.
- Süre dolduğunda gün otomatik olarak sona erer.

Zaman yapısı şu şekildedir:
- Oyun içi gün süresi: **6 saat** (00.00 – 06.00)
- Oyun içi 1 saat = Gerçek hayatta **2 dakika**
- Toplam bir günlük oynanış süresi: **12 dakika**

Zaman sistemi, aynı zamanda oynanış temposunu ve karar baskısını test etmek için kullanılır.

---

## Gün Sonu Ekranı

Gün sonunda oyuncuya:

- Toplam değerlendirilen konteyner sayısı,
- Doğru karar sayısı,
- Yanlış karar sayısı

gösterilir.

Bu ekran, oynanış geri bildirimi sağlamak ve ileride eklenecek sistemler için referans oluşturmak amacıyla tasarlanmıştır.

---

## Bilinçli Olarak Dahil Edilmeyen Sistemler

Bu prototipte aşağıdaki sistemler **bilerek yer almamaktadır**:

- İlaç ve psikoloji sistemi
- Görsel halüsinasyonlar
- Hayali yaratıklar / dövüş
- Hava durumu
- Çoklu gün döngüsü
- Ekonomi ve illegal işler

Bu sistemlerin, ana mekanik doğrulandıktan sonra modüler şekilde eklenmesi planlanmaktadır.

---

## Notlar

- Bu prototip nihai oyun değildir.
- Görsel ve teknik olarak sade tutulmuştur.
- Amaç, erken aşamada doğru oynanış kararlarını verebilmektir.

Bu prototip, GDD yazımı ve ileriki tasarım kararları için teknik bir temel oluşturmayı hedefler.

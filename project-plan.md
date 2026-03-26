FAZ 1 – FOOTBALL SCOUTING PLATFORM PRODUCT DEFINITION

Amaç:
Bu ürünün amacı, kullanıcıların futbol oyuncuları keşfedip analiz eklediği, ancak içeriklerin yayınlanmadan önce kontrol edildiği bir scouting platformu oluşturmaktır. Sistem, erken aşamada yüksek kaliteli veri üretimine odaklanır.

Ana Prensip:
Herkes içerik üretebilir, ancak herkes içerik yayınlayamaz.

Ürün Tanımı:
Platform, kullanıcıların oyuncu önerebildiği, bu önerilere analiz eklediği ve admin onayından sonra bu içeriklerin diğer kullanıcılar tarafından görüntülenip oylanabildiği bir yapıdır. Amaç sosyal medya değil, kaliteli scouting datası üretmektir.

Kullanıcı Rolleri:

* User: Oyuncu önerir, görüntüler, oy verir
* Admin: Önerileri inceler, onaylar veya reddeder

Temel Akış:

1. Kullanıcı platforma girer

* Mevcut oyuncuları keşfeder
* Oyuncu listesi ve detay sayfalarını inceler

2. Kullanıcı oyuncu önerir

* Oyuncu bilgileri girilir (isim, yaş, pozisyon, takım, lig)
* Video linki zorunludur
* Analiz yazısı zorunludur

3. Sistem öneriyi beklemeye alır

* Status: Pending
* Kullanıcıya “incelemede” bilgisi gösterilir

4. AI ön değerlendirme yapar (yardımcı rol)

* Analizin boş veya anlamsız olup olmadığını kontrol eder
* Kısa bir özet çıkarır
* Kalite skoru üretir (AI Score)
  NOT: AI karar vermez, sadece yardımcı olur

5. Admin değerlendirme yapar

* Oyuncunun gerçekliği
* Videonun uygunluğu
* Analizin kalitesi
* AI skorunu referans alabilir

Karar:

* Approve → içerik yayına alınır
* Reject → içerik reddedilir

6. Yayınlanan içerik (Approved)

* Player sayfasında görünür
* Diğer kullanıcılar:

  * Oy verebilir (upvote/downvote)
  * Yorum yapabilir

7. Basit skor sistemi

* Score = Upvote - Downvote
* En çok oy alan oyuncular öne çıkar

İçerik Kalite Stratejisi:

* Amaç çok içerik değil, kaliteli içerik üretmektir
* Düşük kaliteli içerikler reddedilmelidir
* Platformun ilk dönemi “elit içerik” üretmeye odaklanır

Kabul Kriterleri:

* Gerçek oyuncu
* Video mevcut ve ilgili
* Analiz anlamlı ve boş değil

Red Kriterleri:

* Spam içerik
* Boş analiz (örnek: “çok iyi oyuncu”)
* Alakasız veya yanıltıcı video

UI/UX Prensipleri:

* Keşif odaklı yapı (discovery first)
* Kart bazlı oyuncu gösterimi
* Basit ve hızlı oy verme
* Karanlık tema + spor estetiği
* Admin panel gizli ve sade olmalı

Faz 1 Scope (özellikle sınır):

* Basit voting (weighted yok)
* Scout score yok
* Subscription yok
* Gelişmiş filtreleme yok
* AI sadece yardımcı (karar vermez)

Başarı Kriterleri:

1. Kullanıcılar oyuncu ekliyor mu?
2. Kullanıcılar oy veriyor mu?
3. Admin olarak sen içerikleri yönetmekte zorlanıyor musun?

Eğer admin yükü artıyorsa, bu ürünün çalıştığını gösterir.

Faz 1 Hedefi:
Platformu büyütmek değil, ilk 100 kaliteli oyuncu datasını oluşturmaktır.

Zihniyet:
Bu bir sosyal medya ürünü değil, bir veri üretim sistemidir.


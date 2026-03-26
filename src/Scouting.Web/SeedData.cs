using Scouting.Web.Domains.AnalysisEntity;
using Scouting.Web.Domains.AnalysisLikeEntity;
using Scouting.Web.Domains.PlayerEntity;
using Scouting.Web.Domains.ScouterFollowEntity;
using Scouting.Web.Domains.UserEntity;
using Scouting.Web.Domains.VoteEntity;
using Scouting.Web.Infrastructure;

namespace Scouting.Web;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Zaten seed edilmişse geç
        if (db.Users.Any()) return;

        // ── Kullanıcılar ──────────────────────────────────────────────────────
        var hash = BCrypt.Net.BCrypt.HashPassword("scout123");

        var admin   = MakeUser("admin@scouting.com",  "sç",       hash, admin: true);
        var emre    = MakeUser("emre@scout.com",      "utku atakul",     hash);
        var mehmet  = MakeUser("mehmet@scout.com",    "mutlu taşova",    hash);
        var ahmet   = MakeUser("ahmet@scout.com",     "bilal çınar", hash);
        var zeynep  = MakeUser("zeynep@scout.com",    "onur özmay",  hash);

        db.Users.AddRange(admin, emre, mehmet, ahmet, zeynep);

        // ── Oyuncular (approved) ──────────────────────────────────────────────
        var arda = MakePlayer("Arda Güler",           19, PlayerPosition.CAM,
            "Real Madrid", "La Liga", "Türkiye", emre.Id,
            imageUrl: "https://img.a.transfermarkt.technology/portrait/header/861410-1699472585.jpg?lm=1",
            tmId: "887080", tmUrl: "https://www.transfermarkt.com.tr/arda-guler/profil/spieler/887080",
            marketValue: 80m);

        var kenan = MakePlayer("Kenan Yıldız",        19, PlayerPosition.LW,
            "Juventus", "Serie A", "Türkiye", emre.Id,
            imageUrl: "https://img.a.transfermarkt.technology/portrait/header/845654-1759822280.jpg?lm=1",
            tmId: "920455", tmUrl: "https://www.transfermarkt.com.tr/kenan-yildiz/profil/spieler/920455",
            marketValue: 40m);

        var ferdi = MakePlayer("Ferdi Kadıoğlu",      24, PlayerPosition.LB,
            "Brighton", "Premier League", "Türkiye", mehmet.Id,
            imageUrl: "https://img.a.transfermarkt.technology/portrait/header/369316-1724792538.jpg?lm=1",
            marketValue: 35m);

        var semih = MakePlayer("Semih Kılıçsoy",      19, PlayerPosition.ST,
            "Beşiktaş", "Süper Lig", "Türkiye", mehmet.Id,
            imageUrl: "https://img.a.transfermarkt.technology/portrait/header/875334-1757662611.jpg?lm=1",
            marketValue: 12m);

        var karetsas = MakePlayer("Konstantinos Karetsas", 19, PlayerPosition.CAM,
            "Racing Genk", "Belgium Pro League", "Yunanistan", ahmet.Id,
            imageUrl:"https://img.a.transfermarkt.technology/portrait/header/990148-1742566542.jpg?lm=1",
            tmId: "990148", tmUrl: "https://www.transfermarkt.com.tr/konstantinos-karetsas/profil/spieler/990148",
            marketValue: 8m);


        db.Players.AddRange(arda, kenan, ferdi, semih, karetsas);

        // Pending oyuncular
        var efecan = MakePlayer("Efecan Karaca", 21, PlayerPosition.CB,
            "Trabzonspor", "Süper Lig", "Türkiye", ahmet.Id, approved: false);
        var lucas = MakePlayer("Lucas Bellingham", 18, PlayerPosition.CM,
            "Birmingham City", "Championship", "İngiltere", zeynep.Id, approved: false);

        db.Players.AddRange(efecan, lucas);

        // ── Analizler ─────────────────────────────────────────────────────────
        // Arda Güler için analizler
        var a1 = MakeAnalysis(arda.Id, emre.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Arda Güler bu sezon Real Madrid'de beklentilerin çok üzerinde bir performans sergilemektedir. 19 yaşında Avrupa'nın en büyük kulübünde oynamak başlı başına büyük bir başarı olmakla birlikte, Arda sahaya çıktığı her maçta fark yaratan oyunudur. Top kontrolü, dar alanda etkinliği ve yaratıcı paslarıyla dikkat çekmektedir.",
            technical: "Top kontrolü ve ilk temas kalitesi üst düzey. Sıkı markajda top kaybetme oranı oldukça düşük. Her iki ayağını da etkin şekilde kullanabiliyor. Şut isabeti özellikle sert vuruşlarda yüksek.",
            tactical: "Pozisyon alışı çok zekice. Serbest kalma hareketleri organize savunmaları bile zorlayacak düzeyde. Pressing'e katılımı sezon başına göre belirgin şekilde artmış.",
            strengths: "Dar alanda sürpriz geçişler, frikik ve serbest vuruş uzmanlığı, genç yaşına rağmen maç okuma kapasitesi, karizmatik liderlik özellikleri.",
            weaknesses: "Fiziksel güç ve sürat açısından Premier Lig ve La Liga standartlarına ulaşmak için gelişim devam etmeli. Savunmaya dönüş mesafesini kısaltması gerekiyor.",
            score: 8.5m, summary: "Olağanüstü teknik kalite, taktiksel zekâ. Real Madrid'de gelecek vadeden oyuncu.");
        a1.Approve();
        for (var i = 0; i < 24; i++) a1.IncrementLikeCount();

        var a2 = MakeAnalysis(arda.Id, mehmet.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Arda Güler'in son dönemde gösterdiği performans takdire şayandır. Özellikle Şampiyonlar Ligi maçlarında sahneye çıkışı ve katkı oranı dikkat çekmektedir. Savunma organizasyonlarını anlama kapasitesi ve boşluk kullanımı çok gelişmiş.",
            strengths: "Serbest vuruş kalitesi Avrupa genelinde en iyiler arasında. Dar alanlarda çalım ve çabuk pozisyon değiştirme.",
            score: 7.8m, summary: "Dengeli ve geniş çaplı değerlendirme.");
        a2.Approve();
        for (var i = 0; i < 11; i++) a2.IncrementLikeCount();

        // Kenan Yıldız için analizler
        var a3 = MakeAnalysis(kenan.Id, emre.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Kenan Yıldız Juventus formasıyla bu sezon kilit oyuncu haline gelmeye başlamıştır. Sol kanatta oynarken sağ ayağını kullanarak içeriden tehdit yaratması en büyük silahı. Özellikle savunma arkaları için yaptığı koşular rakip defansları sürekli meşgul ediyor.",
            technical: "Her iki ayağıyla şut kullanabilmesi büyük avantaj. Kafadan vuruş performansı da boy uzunluğunu düşündüğünüzde oldukça iyi. Çalım hareketleri çeşitli ve rakip için öngörülemez.",
            tactical: "Juventus'un pressing organizasyonuna tam uyum sağlamış durumda. Kanatları genişletme ve içe kesme hareketlerini çok iyi zamanlıyor.",
            physical: "Sürat değerleri elit düzeyde. Maç boyunca yüksek yoğunlukta koşabilmesi kondisyonunun üst seviyede olduğunu gösteriyor.",
            strengths: "Hız, çift ayak kullanımı, dribling kalitesi, genç yaşına rağmen büyük maç deneyimi.",
            weaknesses: "Savunmaya katkı oranı tutarsız. Fiziksel güç gelişimi sürmekte.",
            score: 8.2m, summary: "Çift ayak kullanımı ve sürat kombinasyonu istisnai. Juventus'ta güvenilir sol kanat.");
        a3.Approve();
        for (var i = 0; i < 18; i++) a3.IncrementLikeCount();

        // Ferdi Kadıoğlu analizi
        var a4 = MakeAnalysis(ferdi.Id, mehmet.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Ferdi Kadıoğlu Brighton'da düzenli başlangıç kadrosunda yerini almıştır. Modern sol bek profiline tam olarak uymaktadır: hem savunmada sağlam hem de hücuma katılımda etkili. Premier Lig'in yüksek tempoya adapte olması son derece başarılıdır.",
            technical: "Savunma pozisyonları çok güçlü. Cross kalitesi yüksek. Sol ayakla çalıştırma hâkimiyeti üst düzey.",
            tactical: "Brighton'ın yüksek pressing sistemine mükemmel uyum. Hücuma katılım zamanlaması defans arkası boşlukları minimize edecek şekilde ayarlanmış.",
            strengths: "Savunmada sağlamlık, hücuma katkı, çalıştırma hâkimiyeti, Premier Lig adaptasyonu.",
            score: 7.5m, summary: "Premier Lig'in zorlu temposunda kendini ispat etmiş dengeli sol bek.");
        a4.Approve();
        for (var i = 0; i < 9; i++) a4.IncrementLikeCount();

        // Semih Kılıçsoy analizi
        var a5 = MakeAnalysis(semih.Id, ahmet.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Semih Kılıçsoy Beşiktaş'ta sahneye çıkışından itibaren Süper Lig'in en dikkat çeken genç oyuncularından biri olmuştur. Ceza sahası içindeki zekâsı, isabetli bitirişleri ve yaşına oranla sahip olduğu olgunluk seviyesi onu özel kılmaktadır. Avrupa kulüpleri tarafından yakından takip edildiği bilinmektedir.",
            technical: "Ceza sahası içinde son derece soğukkanlı. Şut geometrisi iyi, açıyı güzel görüyor. Kafa vuruşu da boya rağmen etkili.",
            strengths: "Ceza sahası zekâsı, bitiriş kalitesi, genç yaşta rekabetçi ruh.",
            weaknesses: "Fiziksel olarak gelişmekte. Savunmaya katkı düşük.",
            score: 7.2m, summary: "Süper Lig'in en umut verici genç forveti.");
        a5.Approve();
        for (var i = 0; i < 7; i++) a5.IncrementLikeCount();

        // Karetsas analizi
        var a6 = MakeAnalysis(karetsas.Id, ahmet.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Konstantinos Karetsas, Racing Genk altyapısından çıkarak Belçika Pro Lig'inde başlangıç kadrosuna girmeyi başarmış 19 yaşında bir orta saha organizatörüdür. Teknik donanımı ve oyun zekâsı yaşının çok üzerinde. Dört büyük lig ekiplerinin radarında olduğu bildirilmektedir.",
            technical: "İki ayak hakimiyeti etkili. Dar alanda hızlı kombinasyon yapabilme kapasitesi yüksek. Uzun pas isabet oranı dikkat çekici.",
            tactical: "Genk'in geçiş oyununda kilit rol üstleniyor. Orta sahadaki mesafe kapama çalışması ve top geri kazanım oranı takımı ortalamasının oldukça üstünde.",
            strengths: "Yaşına göre taktiksel olgunluk, oyun okuma kapasitesi, pas çeşitliliği.",
            weaknesses: "Fiziksel güç gelişimi sürmekte. Büyük maçlarda tutarlılık test edilmeli.",
            score: 7.0m, summary: "Belçika'nın en değerli genç yeteneklerinden biri. Büyük kulüplere adım atacak potansiyel var.");
        a6.Approve();
        for (var i = 0; i < 5; i++) a6.IncrementLikeCount();
        
        // Pending analizler (pending oyuncular için)
        var a9 = MakeAnalysis(efecan.Id, ahmet.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Efecan Karaca Trabzonspor'da bu sezon ön plana çıkan genç stoperlerden biridir. Hava toplarındaki hakimiyeti ve top taşıma becerisi modern stoper profiline yakışmaktadır. Yurt içi ve dışından kulüplerin ilgisini çekmeye başladığı söylenmektedir.",
            strengths: "Hava topu hakimiyeti, güçlü gövde savunması.",
            weaknesses: "Hız gerektiren birebir durumlarında zaman zaman geri kalabiliyor.");
        // pending — approve edilmedi

        var a10 = MakeAnalysis(lucas.Id, zeynep.Id,
            videoUrl: "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            general: "Lucas Bellingham Championship'ta Birmingham City formasıyla oynadığı maçlarda dikkat çekici performanslar sergilemektedir. Pas oyunu ve top dağıtımındaki kalitesi yaşı için oldukça yüksek. Üst liglerin radarına girmiş durumdadır.",
            technical: "İlk temas kalitesi iyi. Kısa paslaşma ve kombinasyon oyununa hakimiyeti güçlü.",
            score: 5.5m, summary: "Championship'te umut vaat eden genç orta saha.");
        // pending — approve edilmedi

        db.Analyses.AddRange(a1, a2, a3, a4, a5, a6, a9, a10);

        // ── Kullanıcı seviyeleri (analiz onayları) ────────────────────────────
        // emre: Expert (50 approved analyses = 500 puan)
        for (var i = 0; i < 50; i++) emre.IncrementApprovedAnalysisCount();
        // + 200 like bonus
        for (var i = 0; i < 200; i++) emre.IncrementLikesReceived();

        // mehmet: Senior (15 approved = 150 + 60 like = 210 puan)
        for (var i = 0; i < 15; i++) mehmet.IncrementApprovedAnalysisCount();
        for (var i = 0; i < 60; i++) mehmet.IncrementLikesReceived();

        // ahmet: Mid (5 approved = 50 puan)
        for (var i = 0; i < 5; i++) ahmet.IncrementApprovedAnalysisCount();

        // zeynep: Starter (2 approved = 20 puan)
        for (var i = 0; i < 2; i++) zeynep.IncrementApprovedAnalysisCount();

        // ── Oyuncu oyları ─────────────────────────────────────────────────────
        db.Votes.AddRange(
            Vote.Create(arda.Id, emre.Id, VoteType.Up),
            Vote.Create(arda.Id, mehmet.Id, VoteType.Up),
            Vote.Create(arda.Id, ahmet.Id, VoteType.Up),
            Vote.Create(arda.Id, zeynep.Id, VoteType.Up),
            Vote.Create(kenan.Id, emre.Id, VoteType.Up),
            Vote.Create(kenan.Id, mehmet.Id, VoteType.Up),
            Vote.Create(kenan.Id, ahmet.Id, VoteType.Up),
            Vote.Create(ferdi.Id, emre.Id, VoteType.Up),
            Vote.Create(ferdi.Id, mehmet.Id, VoteType.Up),
            Vote.Create(semih.Id, emre.Id, VoteType.Up),
            Vote.Create(semih.Id, zeynep.Id, VoteType.Up),
            Vote.Create(karetsas.Id, ahmet.Id, VoteType.Up),
            Vote.Create(karetsas.Id, zeynep.Id, VoteType.Up)

        );

        // Player score güncelleme
        arda.UpdateScore(4);
        kenan.UpdateScore(3);
        ferdi.UpdateScore(2);
        semih.UpdateScore(2);
        karetsas.UpdateScore(2);

        // ── Analiz beğenileri ─────────────────────────────────────────────────
        db.AnalysisLikes.AddRange(
            AnalysisLike.Create(a1.Id, mehmet.Id).Data!,
            AnalysisLike.Create(a1.Id, ahmet.Id).Data!,
            AnalysisLike.Create(a1.Id, zeynep.Id).Data!,
            AnalysisLike.Create(a3.Id, emre.Id).Data!,
            AnalysisLike.Create(a3.Id, mehmet.Id).Data!,
            AnalysisLike.Create(a4.Id, emre.Id).Data!,
            AnalysisLike.Create(a5.Id, emre.Id).Data!,
            AnalysisLike.Create(a6.Id, emre.Id).Data!
        );

        // ── Takipler ─────────────────────────────────────────────────────────
        db.ScouterFollows.AddRange(
            ScouterFollow.Create(mehmet.Id, emre.Id).Data!,
            ScouterFollow.Create(ahmet.Id, emre.Id).Data!,
            ScouterFollow.Create(zeynep.Id, emre.Id).Data!,
            ScouterFollow.Create(ahmet.Id, mehmet.Id).Data!,
            ScouterFollow.Create(zeynep.Id, mehmet.Id).Data!
        );

        emre.IncrementFollowerCount();
        emre.IncrementFollowerCount();
        emre.IncrementFollowerCount();
        mehmet.IncrementFollowerCount();
        mehmet.IncrementFollowerCount();

        await db.SaveChangesAsync();
    }

    // ── Yardımcı factory'ler ──────────────────────────────────────────────────

    private static User MakeUser(string email, string username, string hash, bool admin = false)
    {
        var u = User.Create(email, username, hash).Data!;
        if (admin) u.MakeAdmin();
        return u;
    }

    private static Player MakePlayer(
        string name, int age, PlayerPosition position,
        string team, string league, string country,
        Guid createdBy,
        string? imageUrl = null,
        string? tmId = null,
        string? tmUrl = null,
        decimal? marketValue = null,
        bool approved = true)
    {
        var p = Player.Create(name, age, position, team, league, country, createdBy).Data!;
        if (approved) p.Approve();
        if (imageUrl is not null) p.SetImageUrl(imageUrl);
        if (tmId is not null && tmUrl is not null) p.SetTransfermarkt(tmId, tmUrl);
        if (marketValue is not null)
        {
            // Piyasa değerini doğrudan UpdateFromTransfermarkt ile set et
            var fakeData = new Scouting.Web.Services.TransfermarktPlayerData
            {
                TmId = tmId ?? "",
                MarketValueMillions = marketValue
            };
            p.UpdateFromTransfermarkt(fakeData);
        }
        return p;
    }

    private static Analysis MakeAnalysis(
        Guid playerId, Guid scoutId,
        string videoUrl, string general,
        string? technical = null, string? tactical = null,
        string? physical = null, string? strengths = null,
        string? weaknesses = null,
        decimal? score = null, string? summary = null)
    {
        var a = Analysis.Create(playerId, videoUrl, general, scoutId,
            technical, tactical, physical, strengths, weaknesses).Data!;

        if (score.HasValue || summary is not null)
            a.SetAIReview(summary ?? "", score ?? 0m);

        return a;
    }
}

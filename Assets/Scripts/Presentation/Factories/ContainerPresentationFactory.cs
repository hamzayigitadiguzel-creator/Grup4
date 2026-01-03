using System.Collections.Generic;
using Storia.Constants;
using Storia.Data.Generated;

/// <summary>
/// Konteyner presentation oluşturan factory sınıfı.
/// 
/// Sorumluluk: Manifest + Label bilgisini oluşturmak.
/// 
/// İş akışı:
/// 1. Konteyner'in gerçek verisi ile başla (manifestShown = truth, labelShown = truth)
/// 2. conflictChance'e göre çelişki olup olmayacağını karar ver
/// 3. Eğer çelişki olacaksa:
///    - Çelişki tipi seç (ID, Origin, Cargo)
///    - Manifest vs Label'dan hangisini tahrif et (manifestTamperBias'e göre)
///    - Seçilen alanı değiştir (pool'dan farklı değer seç veya ID mutate)
/// 4. Sonuç: manifestShown ve labelShown farklı (çelişki!) veya aynı (temiz)
/// 
/// Determinizm: RNG seed tarafından kontrol edilir (reproducible)
/// </summary>
public static class ContainerPresentationFactory
{
    /// <summary>
    /// Konteyner için presentation (manifest + label) oluştur.
    /// Seed'e göre deterministic, reproducible sonuç verir.
    /// </summary>
    /// <param name="containerData">Konteyner'in gerçek verisi (truth)</param>
    /// <param name="tuning">Çelişki ayarları (chance, bias)</param>
    /// <param name="rng">Seed-based RNG (determinizm için)</param>
    /// <param name="originPool">Mevcut origin port'ları (çelişkide kullanılır)</param>
    /// <param name="cargoPool">Mevcut cargo type'ları (çelişkide kullanılır)</param>
    /// <returns>Manifest ve label bilgisi (gerçek veya çelişkili)</returns>
    public static ContainerPresentation Build(
    ContainerData containerData,
    PresentationTuning tuning,
    DeterministicRng rng,
    IReadOnlyList<string> originPool,
    IReadOnlyList<string> cargoPool)
    {
        // Başlangıç: manifest ve label = truth (her ikisi de aynı/doğru)
        ContainerPresentation result = new ContainerPresentation
        {
            manifestShown = containerData.truth,
            labelShown = containerData.truth,
            conflict = PresentationConflict.None  // Henüz çelişki yok
        };

        // RNG'ye göre bu konteyner çelişkili olacak mı?
        // Örn: conflictChance = 0.3 ise %30 ihtimalle çelişkili
        bool willConflict = rng.Chance(tuning.conflictChance);
        if (!willConflict)
            return result;  // Çelişki yok, orijinal return

        // Çelişki tipi seç: 0=ID, 1=Origin, 2=Cargo
        int conflictPick = rng.RangeInt(0, 3);
        
        // Manifest mi yoksa Label'ı tahrif etmeliyiz?
        // manifestTamperBias = 0.7 ise: %70 manifest hatalı, %30 label hatalı
        bool tamperManifest = rng.Next01() < tuning.manifestTamperBias;

        // Seçilen çelişki tipi ve tahrifat hedefine göre işlem yap
        if (conflictPick == 0)
        {
            // ID Mismatch
            result.conflict = PresentationConflict.IdMismatch;
            if (tamperManifest) 
                // Manifest'in ID'sini mutate et (dijital belge hatalı)
                result.manifestShown.containerId = MutateId(containerData.truth.containerId, rng);
            else 
                // Label'ın ID'sini mutate et (fiziksel etiket hatalı)
                result.labelShown.containerId = MutateId(containerData.truth.containerId, rng);
        }
        else if (conflictPick == 1)
        {
            // Origin Mismatch
            result.conflict = PresentationConflict.OriginMismatch;
            if (tamperManifest)
                // Manifest'in origin'ini farklı bir port'a değiştir
                result.manifestShown.originPort = PickDifferent(containerData.truth.originPort, originPool, rng);
            else
                // Label'ın origin'ini farklı bir port'a değiştir
                result.labelShown.originPort = PickDifferent(containerData.truth.originPort, originPool, rng);
        }
        else
        {
            // Cargo Mismatch
            result.conflict = PresentationConflict.CargoMismatch;
            if (tamperManifest)
                // Manifest'in cargo'sunu farklı type'a değiştir
                result.manifestShown.cargoLabel = PickDifferent(containerData.truth.cargoLabel, cargoPool, rng);
            else
                // Label'ın cargo'sunu farklı type'a değiştir
                result.labelShown.cargoLabel = PickDifferent(containerData.truth.cargoLabel, cargoPool, rng);
        }

        return result;
    }

    /// <summary>
    /// Verilen değer'den farklı bir değer pool'dan seç.
    /// Algoritma: Pool'dan rastgele seçim, current'ten farklı olana kadar deneme
    /// </summary>
    /// <param name="current">Şu anki/gerçek değer</param>
    /// <param name="pool">Seçenekler havuzu (pool)</param>
    /// <param name="rng">Seed-based RNG</param>
    /// <returns>Pool'dan seçilmiş, current'ten farklı değer (veya current eğer impossible)</returns>
    private static string PickDifferent(string current, IReadOnlyList<string> pool, DeterministicRng rng)
    {
        // Pool boş veya null ise, current'i döndür (yapılacak bir şey yok)
        if (pool == null || pool.Count == 0) 
            return current;

        // MaxMutationAttempts kadar deneme yap (sonsuz loop'u önlemek için)
        for (int i = 0; i < GameConstants.MaxMutationAttempts; i++)
        {
            // Pool'dan rastgele bir seçenek al
            string candidate = pool[rng.RangeInt(0, pool.Count)];
            
            // Eğer current'ten farklıysa, bunu döndür
            if (candidate != current) 
                return candidate;
        }

        // Fail-safe: En kötü durumda, current'i döndür
        // (Pool'da sadece current value varsa bu olabilir)
        return current;
    }

    /// <summary>
    /// Konteyner ID'sini mutate et (1-4 rakamı değiştir).
    /// Format: "PREFIX-####" → "PREFIX-##XX" (örn: "TRBU-1234" → "TRBU-5X89")
    /// </summary>
    /// <param name="id">Orijinal ID (örn: "TRBU-1234")</param>
    /// <param name="rng">Seed-based RNG</param>
    /// <returns>Mutate edilmiş ID</returns>
    private static string MutateId(string id, DeterministicRng rng)
    {
        // Geçersiz ID kontrolü
        if (string.IsNullOrEmpty(id)) 
            return "XX-0000";  // Fallback

        // ID'yi prefix ve digit'lere parse et (örn: "TRBU-1234" → prefix="TRBU", digits=['1','2','3','4'])
        var (prefix, digits) = ParseContainerId(id);
        if (digits == null) 
            return id + "X";  // Fallback: parse başarısız

        // Kaç rakamı değiştirelim? (1-4 arası rastgele)
        int digitCountToMutate = rng.RangeInt(1, 5);

        // Pozisyon array'ini shuffle et [0,1,2,3] → [2,0,3,1] (random sıra)
        int[] positions = ShufflePositions(rng);

        // İlk digitCountToMutate pozisyondaki digit'leri değiştir
        // Örn: digitCountToMutate=2 ise 0. ve 1. pos'taki rakamları değiştir
        for (int i = 0; i < digitCountToMutate; i++)
        {
            int pos = positions[i];
            char newDigit = ReplaceRandomDigit(digits[pos], rng);
            digits[pos] = newDigit;
        }

        // Yeni ID'yi oluştur ve döndür (örn: "TRBU-5289")
        return prefix + "-" + new string(digits);
    }

    /// <summary>
    /// Container ID'yi prefix ve digit array'e parse eder.
    /// Format: "TRBU-1234" → prefix="TRBU", digits=['1','2','3','4']
    /// </summary>
    /// <param name="id">ID string (örn: "TRBU-1234")</param>
    /// <returns>(prefix: "TRBU", digits: ['1','2','3','4']) veya (null, null) eğer invalid</returns>
    private static (string prefix, char[] digits) ParseContainerId(string id)
    {
        // ID formatı: "XXXX-####" (4 harf, dash, 4 rakam)
        string[] parts = id.Split('-');
        if (parts.Length < 2) 
            return (null, null);  // Invalid: dash yok veya yanlış format

        string prefix = parts[0];
        string numericPart = parts[1];

        // Numeric part tam 4 rakam olmalı
        if (numericPart.Length != 4) 
            return (null, null);  // Invalid: 4 rakam değil

        // Char array'e çevir ([1][2][3][4])
        return (prefix, numericPart.ToCharArray());
    }

    /// <summary>
    /// Pozisyon array'ini shuffle eder (Fisher-Yates algoritması).
    /// Amaç: Hangi rakamları mutate edeceğimizi rastgele sırayla seçmek.
    /// Örn: [0,1,2,3] → [3,1,0,2] (shuffle sonrası)
    /// </summary>
    /// <param name="rng">Seed-based RNG</param>
    /// <returns>Shuffle edilmiş pozisyon array'i</returns>
    private static int[] ShufflePositions(DeterministicRng rng)
    {
        // Başlangıç pozisyon array'i: [0,1,2,3] (4 digit indeksi)
        int[] positions = { 0, 1, 2, 3 };
        
        // Fisher-Yates shuffle (seed'e göre deterministic)
        for (int i = positions.Length - 1; i > 0; i--)
        {
            // Random swap: position[i] ↔ position[j]
            int j = rng.RangeInt(0, i + 1);
            int temp = positions[i];
            positions[i] = positions[j];
            positions[j] = temp;
        }

        return positions;
    }

    /// <summary>
    /// Verilen digit'ten farklı rastgele bir digit döner (0-9).
    /// Algoritma: 0-9 arası random seçim, current'ten farklı olana kadar deneme.
    /// </summary>
    /// <param name="originalDigit">Şu anki rakam (değiştirilecek)</param>
    /// <param name="rng">Seed-based RNG</param>
    /// <returns>Orijinal'den farklı bir rakam (0-9)</returns>
    private static char ReplaceRandomDigit(char originalDigit, DeterministicRng rng)
    {
        // Başlangıç: orijinal ile aynı
        char newDigit = originalDigit;
        
        // MaxDigitMutationAttempts kadar deneme yap
        for (int attempt = 0; attempt < Storia.Constants.GameConstants.MaxDigitMutationAttempts; attempt++)
        {
            // 0-9 arası random rakam seç
            newDigit = (char)('0' + rng.RangeInt(0, 10));
            
            // Eğer orijinal'den farklıysa, bunu kullan
            if (newDigit != originalDigit)
                break;
        }

        return newDigit;
    }
}

using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Generation;

namespace Storia.Generators
{
    /// <summary>
    /// Seed ve config'e göre gemi listesi üretir ve konteynerleri dağıtır.
    /// Gemi Üretimi = Kaç tane gemi gelecek? Hangi konteynerler hangi gemide?
    /// </summary>
    public static class ShipGenerator
    {
        /// <summary>
        /// Gemi listesi üret ve konteynerleri gemilere dağıt.
        /// İşlem:
        /// 1. Gemi sayısını config'den hesapla
        /// 2. Benzersiz gemi adları üret
        /// 3. Konteynerleri gemilere round-robin şeklinde dağıt
        /// Requires previous generation step result (compile-time enforced).
        /// </summary>
        /// <param name="prevStep">Previous step result (placement rules)</param>
        /// <param name="config">Üretim konfigürasyonu</param>
        /// <param name="pools">Havuz verileri</param>
        /// <returns>Typed result containing ships and RNG checkpoint</returns>
        public static GenerationOrchestrator.ShipsGeneratedResult Generate(
            GenerationOrchestrator.PlacementGeneratedResult prevStep,
            GenerationConfig config,
            PoolsConfig pools)
        {
            if (config == null || pools == null)
                throw new System.ArgumentNullException("Config, Pools null olamaz");

            var rng = prevStep.PreviousStep.PreviousStep.Rng;
            var containers = prevStep.PreviousStep.PreviousStep.Containers;

            // Config'e göre gemi sayısını hesapla (seed tarafından kontrol edilir)
            int shipCount = config.CalculateShipCount(rng);

            // Validasyon: Gemi sayısı konteyner sayısından fazla olamaz
            // Çünkü her gemi en az 1 konteyner taşımalıdır (boş gemi = mantıksız)
            if (shipCount > containers.Count)
                shipCount = containers.Count;

            // Validasyon: Gemi sayısı 0'dan az olamaz (en az 1 gemi gerekli)
            if (shipCount <= 0)
                shipCount = 1;

            // Üretilecek gemiler'in listesi
            List<ShipData> ships = new List<ShipData>(shipCount);

            // Benzersiz gemi adı kontrolü (aynı adda 2 gemi olmasın diye)
            HashSet<string> usedNames = new HashSet<string>();

            // Belirtilen sayı kadar gemi üret
            for (int i = 0; i < shipCount; i++)
            {
                // Gemi ID'sini pool'dan al (örn: "SHIP-001")
                string shipId = pools.GenerateShipId(rng, i + 1);
                
                // Gemi adını rastgele seç (tekrar etmeyen)
                string shipName = GenerateUniqueShipName(rng, pools, usedNames);
                usedNames.Add(shipName);  // İçinde kullanılan adlar'a ekle

                // Gemi'nin çıkış port'unu rastgele seç
                string originPort = pools.GetRandomOriginPort(rng);
                
                // Gemi'nin yolculuk süresini rastgele seç (saatlerde)
                float voyageDuration = config.GenerateVoyageDuration(rng);

                // Gemi data's'ını oluştur (ID, ad, port, süre)
                ShipData ship = new ShipData(shipId, shipName, originPort, voyageDuration);
                ships.Add(ship);
            }

            // Tüm konteynerleri oluşturulan gemilere round-robin şeklinde dağıt
            // Round-robin = sırayla her gemiye konteyner atama (adil dağıtım)
            DistributeContainersToShips(containers, ships);

            // Sonuç: Gemiler listesi + RNG checkpoint (determinism tracking için)
            return new GenerationOrchestrator.ShipsGeneratedResult(prevStep, ships);
        }

        /// <summary>
        /// Konteynerler'i round-robin algoritması ile gemilere dağıt.
        /// Algoritma: Konteyner 0 → Gemi 0, Konteyner 1 → Gemi 1, ... → Gemi N, Konteyner N+1 → Gemi 0
        /// Sonuç: Her gemi yaklaşık eşit sayıda konteyner alır.
        /// </summary>
        /// <param name="containers">Dağıtılacak konteynerler</param>
        /// <param name="ships">Hedef gemiler</param>
        private static void DistributeContainersToShips(
            List<ContainerData> containers,
            List<ShipData> ships)
        {
            // Konteyner veya gemi yoksa işlem yapma
            if (containers.Count == 0 || ships.Count == 0)
                return;

            // Şu anki gemi indexi (sırasıyla hangi gemi konteyner alacak?)
            int shipIndex = 0;

            // Her konteyner'i sırayla bir gemiye ata
            foreach (var container in containers)
            {
                // Şu anki gemi'ye konteyner'i ekle
                ships[shipIndex].AddContainer(container);
                
                // Sonraki gemiye geç (mod işlemi ile gezinti: 0→1→2→...→N→0→1→...)
                shipIndex = (shipIndex + 1) % ships.Count;
            }
        }

        /// <summary>
        /// Benzersiz gemi ismi üret (daha önce kullanılmamış).
        /// Algoritma: Pool'dan isim al, kullanıldı mı kontrol et, yoksa fallback kullan.
        /// </summary>
        /// <param name="rng">Randomizer (isim seçimi için)</param>
        /// <param name="pools">Gemi ismi pool'u</param>
        /// <param name="usedNames">Daha önce kullanılan isimler (tekrar etme kontrolü için)</param>
        /// <returns>Benzersiz gemi ismi</returns>
        private static string GenerateUniqueShipName(
            DeterministicRng rng,
            PoolsConfig pools,
            HashSet<string> usedNames)
        {
            // Maksimum 50 kez deneme (sonsuz loop'u önlemek için)
            const int maxAttempts = 50;
            int attempts = 0;

            // Pool'dan rastgele isim al ve benzersizliğini kontrol et
            while (attempts < maxAttempts)
            {
                // Pool'daki isimler'den rastgele seç
                string name = pools.GenerateShipName(rng);
                
                // Eğer bu isim daha önce kullanılmamışsa, döndür
                if (!usedNames.Contains(name))
                    return name;

                attempts++;  // Başarısız, tekrar dene
            }

            // Fallback: Eğer 50 denemede de benzersiz isim bulunmadıysa, numara ile isim üret
            // Format: "M/V Ship #1", "M/V Ship #2", vb.
            return $"M/V Ship #{usedNames.Count + 1}";
        }
    }
}

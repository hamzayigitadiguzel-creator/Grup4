using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Generation;
using Storia.Rules.Runtime;

namespace Storia.Generators
{
    /// <summary>
    /// Seed ve config'e göre placement rule üretir.
    /// Placement Rule = Konteynerler hangi zone'a yerleştirilmeli?
    /// Rule tipi: Cargo-based, ID-based veya Origin-based olabilir.
    /// </summary>
    public static class PlacementRuleGenerator
    {
        /// <summary>
        /// Placement rule üret ve konteynerler'e expectedZoneId ata.
        /// İşlem:
        /// 1. Rule tipi belirle (Cargo/ID/Origin - hangisi kullanılacak?)
        /// 2. Konteynerler'in kaçına rule uygulanacağını hesapla (coverage ratio)
        /// 3. Rastgele konteynerler seç
        /// 4. Seçilen konteynerler'e expectedZoneId ata
        /// Requires previous generation step result (compile-time enforced).
        /// </summary>
        /// <param name="prevStep">Previous step result (task rules)</param>
        /// <param name="config">Üretim konfigürasyonu</param>
        /// <param name="pools">Havuz verileri (zone isimleri için)</param>
        /// <returns>Typed result containing placement rule and RNG checkpoint</returns>
        public static GenerationOrchestrator.PlacementGeneratedResult Generate(
            GenerationOrchestrator.TaskRulesGeneratedResult prevStep,
            GenerationConfig config,
            PoolsConfig pools)
        {
            if (config == null || pools == null)
                throw new System.ArgumentNullException("Config, Pools null olamaz");

            var rng = prevStep.PreviousStep.Rng;
            var containers = prevStep.PreviousStep.Containers;

            // RNG'ye göre rule tipi belirle (Cargo, ID veya Origin?)
            // config.UseCargoBasedPlacement ve config.UseIdBasedPlacement'e göre seçilir
            RuleConditionType ruleType = DecideRuleType(rng, config);

            // Boş placement rule'u oluştur (tipi belirtildi, mapping'ler sonra eklenecek)
            PlacementRuleData rule = new PlacementRuleData(ruleType);

            // Kaç tane konteyner için placement rule tanımlı olacak?
            // Örn: %80 coverage = konteynerler'in %80'ine zone atanacak
            int coverageCount = UnityEngine.Mathf.RoundToInt(containers.Count * config.PlacementCoverageRatio);

            // Sıfır veya negatif sonuç kontrolü - eğer öyle ise tüm konteynerler'i kapsı
            if (coverageCount <= 0)
                coverageCount = containers.Count;

            // Hesaplanan sayı kadar rastgele konteyner seç (seed'e göre deterministik)
            List<ContainerData> selectedContainers = SelectRandomContainers(rng, containers, coverageCount);

            // Rule tipi'ne göre mapping kuralını inşa et
            // Her rule tipi farklı mantıkla konteynerler'e zone atar
            if (ruleType == RuleConditionType.CargoType)
            {
                // Cargo-based: Aynı cargo türündeki konteynerler = aynı zone
                BuildCargoBasedRule(rng, pools, selectedContainers, rule);
            }
            else if (ruleType == RuleConditionType.ContainerId)
            {
                // ID-based: Her konteyner = farklı/aynı zone (spesifik mapping)
                BuildIdBasedRule(rng, pools, selectedContainers, rule);
            }
            else if (ruleType == RuleConditionType.OriginPort)
            {
                // Origin-based: Aynı port'tan gelen konteynerler = aynı zone
                BuildOriginBasedRule(rng, pools, selectedContainers, rule);
            }

            return new GenerationOrchestrator.PlacementGeneratedResult(prevStep, rule);
        }

        /// <summary>
        /// Hangi placement rule tipi kullanılacağını belirle.
        /// Config'deki boolean flag'lere göre aday rule tipi'leri oluştur ve rastgele seç.
        /// </summary>
        /// <param name="rng">Seed-based randomizer (seçim için)</param>
        /// <param name="config">Üretim konfigürasyonu (hangi tipler aktif?)</param>
        /// <returns>Seçilen rule tipi (Cargo, ID veya Origin)</returns>
        private static RuleConditionType DecideRuleType(DeterministicRng rng, GenerationConfig config)
        {
            // Config'de aktif olan rule tipleri'ni bir listeye ekle
            List<RuleConditionType> candidates = new List<RuleConditionType>();

            // Eğer Cargo-based placement aktifse listeye ekle
            if (config.UseCargoBasedPlacement)
                candidates.Add(RuleConditionType.CargoType);

            // Eğer ID-based placement aktifse listeye ekle
            if (config.UseIdBasedPlacement)
                candidates.Add(RuleConditionType.ContainerId);

            // Eğer hiç aday yoksa (config yanlış ayarlandıysa), cargo-based'i default olarak ekle
            if (candidates.Count == 0)
                candidates.Add(RuleConditionType.CargoType);

            // Aday'lar arasından rastgele seç (seed tarafından kontrol edilir)
            int index = rng.RangeInt(0, candidates.Count);
            return candidates[index];
        }

        /// <summary>
        /// Konteyner listesinden rastgele N tane seç (unique - tekrar etmez).
        /// Yöntemi: Shuffle + GetRange (Fisher-Yates algoritması)
        /// </summary>
        /// <param name="rng">Seed-based randomizer</param>
        /// <param name="containers">Seçilecek konteynerler havuzu</param>
        /// <param name="count">Kaç tane seçilecek</param>
        /// <returns>Rastgele seçilmiş konteynerler</returns>
        private static List<ContainerData> SelectRandomContainers(
            DeterministicRng rng,
            List<ContainerData> containers,
            int count)
        {
            // Eğer istenen sayı tüm konteynerler kadar ise hepsini döndür
            if (count >= containers.Count)
                return new List<ContainerData>(containers);

            // Orijinal listeyi etkilememeye dikkat ediyorum, bir kopyasını kullan
            List<ContainerData> shuffled = new List<ContainerData>(containers);
            
            // RNG seed'ine göre shuffle et (aynı seed = aynı sıralama)
            rng.Shuffle(shuffled);

            // Shuffle'dan sonra ilk N tanesini seç ve döndür
            return shuffled.GetRange(0, count);
        }

        /// <summary>
        /// Cargo-based placement rule oluştur.
        /// Mantık: Aynı cargo türündeki tüm konteynerler = aynı zone'a yerleştirilmeli.
        /// Örnek: Elektronik (cargo) → Zone A, Gıda (cargo) → Zone B
        /// </summary>
        /// <param name="rng">Randomizer (her cargo için zone seçmek için)</param>
        /// <param name="pools">Zone pool'u (mevcut zone'ları almak için)</param>
        /// <param name="containers">Seçilmiş konteynerler (bunlara rule uygulanacak)</param>
        /// <param name="rule">Doldurulacak placement rule (mapping'ler eklenecek)</param>
        private static void BuildCargoBasedRule(
            DeterministicRng rng,
            PoolsConfig pools,
            List<ContainerData> containers,
            PlacementRuleData rule)
        {
            // Cargo tipi → Zone ID mapping
            Dictionary<string, int> cargoToZoneId = new Dictionary<string, int>();
            // Cargo tipi → Zone Name mapping (UI gösterim için)
            Dictionary<string, string> cargoToZoneName = new Dictionary<string, string>();

            // Seçilmiş tüm konteynerler'i ata
            foreach (var container in containers)
            {
                // Konteyner'in gerçek cargo tipi (truth'tan)
                string cargo = container.truth.cargoLabel;

                // Bu cargo tipi için daha önce zone tanımlanmış mı?
                if (!cargoToZoneId.ContainsKey(cargo))
                {
                    // Tanımlanmamışsa, rastgele bir zone seç
                    var (zoneId, zoneName) = pools.GetRandomZoneIdAndName(rng);
                    cargoToZoneId[cargo] = zoneId;      // Mapping'i kayıt et
                    cargoToZoneName[cargo] = zoneName;
                    // Rule'a bu mapping'i ekle
                    rule.AddMapping(cargo, zoneId, zoneName);
                }

                // Konteyner'e expected zone ID'sini ata (karar validator'ü tarafından kullanılacak)
                container.expectedZoneId = cargoToZoneId[cargo];
            }
        }

        /// <summary>
        /// ID-based placement rule oluştur.
        /// Mantık: Her spesifik konteyner ID'sine bir zone'a ata (ayrı ayrı).
        /// Örnek: TRBU-1001 → Zone A, TRBU-1002 → Zone B, vb.
        /// En katı placement kuralı (en zor oyun)
        /// </summary>
        /// <param name="rng">Randomizer (her ID için zone seçmek için)</param>
        /// <param name="pools">Zone pool'u (mevcut zone'ları almak için)</param>
        /// <param name="containers">Seçilmiş konteynerler (her birine unique zone ata)</param>
        /// <param name="rule">Doldurulacak placement rule (tüm ID → Zone mapping'leri eklenecek)</param>
        private static void BuildIdBasedRule(
            DeterministicRng rng,
            PoolsConfig pools,
            List<ContainerData> containers,
            PlacementRuleData rule)
        {
            // Her seçilmiş konteyner'e ayrı ayrı zone ata
            foreach (var container in containers)
            {
                // Konteyner'in gerçek ID'si
                string id = container.truth.containerId;
                
                // Bu ID için rastgele bir zone seç
                var (zoneId, zoneName) = pools.GetRandomZoneIdAndName(rng);

                // Rule'a bu mapping'i ekle (ID → Zone)
                rule.AddMapping(id, zoneId, zoneName);
                
                // Konteyner'e expected zone ID'sini ata
                container.expectedZoneId = zoneId;
            }
        }

        /// <summary>
        /// Origin-based placement rule oluştur.
        /// Mantık: Aynı port'tan gelen tüm konteynerler = aynı zone'a yerleştirilmeli.
        /// Örnek: Pire (port) → Zone A, İstanbul (port) → Zone B
        /// Cargo-based gibi, ama origin'e göre
        /// </summary>
        /// <param name="rng">Randomizer (her origin port için zone seçmek için)</param>
        /// <param name="pools">Zone pool'u (mevcut zone'ları almak için)</param>
        /// <param name="containers">Seçilmiş konteynerler (bunlara rule uygulanacak)</param>
        /// <param name="rule">Doldurulacak placement rule (mapping'ler eklenecek)</param>
        private static void BuildOriginBasedRule(
            DeterministicRng rng,
            PoolsConfig pools,
            List<ContainerData> containers,
            PlacementRuleData rule)
        {
            // Origin port → Zone ID mapping
            Dictionary<string, int> originToZoneId = new Dictionary<string, int>();
            // Origin port → Zone Name mapping (UI gösterim için)
            Dictionary<string, string> originToZoneName = new Dictionary<string, string>();

            // Seçilmiş tüm konteynerler'i ata
            foreach (var container in containers)
            {
                // Konteyner'in gerçek origin port'u
                string origin = container.truth.originPort;

                // Bu port için daha önce zone tanımlanmış mı?
                if (!originToZoneId.ContainsKey(origin))
                {
                    // Tanımlanmamışsa, rastgele bir zone seç
                    var (zoneId, zoneName) = pools.GetRandomZoneIdAndName(rng);
                    originToZoneId[origin] = zoneId;       // Mapping'i kayıt et
                    originToZoneName[origin] = zoneName;
                    // Rule'a bu mapping'i ekle
                    rule.AddMapping(origin, zoneId, zoneName);
                }

                // Konteyner'e expected zone ID'sini ata
                container.expectedZoneId = originToZoneId[origin];
            }
        }
    }
}

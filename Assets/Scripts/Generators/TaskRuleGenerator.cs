using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Generation;
using Storia.Rules.Runtime;

namespace Storia.Generators
{
    /// <summary>
    /// Seed ve config'e göre task rule üretir (AND/OR composite desteği ile).
    /// Task Rule = Hangi konteynerler "kabul edilmesi gereken" konteynerler?
    /// Basit OR'dan composite AND/OR kombinasyonlarına kadar kompleks kurallar oluşturabilir.
    /// </summary>
    public static class TaskRuleGenerator
    {
        /// <summary>
        /// Task rule üret ve target container'ları işaretle.
        /// İşlem:
        /// 1. Hedef konteyner sayısını config'den hesapla
        /// 2. Rastgele konteyner'ları hedef olarak seç ve işaretle
        /// 3. Basit OR veya composite AND/OR rule'u inşa et
        /// Requires previous generation step result (compile-time enforced).
        /// </summary>
        /// <param name="prevStep">Previous step result (containers)</param>
        /// <param name="config">Üretim konfigürasyonu</param>
        /// <returns>Typed result containing task rule and RNG checkpoint</returns>
        public static GenerationOrchestrator.TaskRulesGeneratedResult Generate(
            GenerationOrchestrator.ContainersGeneratedResult prevStep,
            GenerationConfig config)
        {
            if (config == null)
                throw new System.ArgumentNullException("Config null olamaz");

            var rng = prevStep.Rng;
            var containers = prevStep.Containers;

            // Hedef konteyner sayısını config'deki oranına göre hesapla (örn: %33)
            int targetCount = config.CalculateTargetContainerCount(containers.Count);
            
            // Geçersiz değer kontrolü (0'dan az veya toplam sayıyı aşan)
            if (targetCount <= 0 || targetCount > containers.Count)
                targetCount = containers.Count / 3; // Fallback: tüm konteynerler = %33'ü hedef

            // RNG'nin seed'ine göre belirtilen sayıda rastgele konteyner seç
            List<ContainerData> targetContainers = SelectRandomContainers(rng, containers, targetCount);

            // Seçilen konteynerler hedef olarak işaretle (isTarget = true)
            // Bu işaret, decision manager'ın "doğru karar" kontrolü için kullanılacak
            foreach (var container in targetContainers)
            {
                container.isTarget = true;
            }

            // Seçilen hedef konteynerler'e göre task rule'unu inşa et
            // Rule tipi: basit OR mi yoksa composite AND/OR mi? Config'e göre karar ver
            CompositeRule rule = BuildRule(rng, config, targetContainers);

            return new GenerationOrchestrator.TaskRulesGeneratedResult(prevStep, rule);
        }

        /// <summary>
        /// Listeden rastgele N tane konteyner seç (unique - tekrar etmez).
        /// Algoritma: Fisher-Yates shuffle kullanarak rastgele seçim (seed-based)
        /// </summary>
        /// <param name="rng">Deterministic RNG (seed tarafından kontrol ediliyor)</param>
        /// <param name="containers">Tüm konteynerler</param>
        /// <param name="count">Kaç tane seçilecek</param>
        /// <returns>Rastgele seçilmiş, unique konteynerler (sırası shuffle'dan bağımsız)</returns>
        private static List<ContainerData> SelectRandomContainers(
            DeterministicRng rng,
            List<ContainerData> containers,
            int count)
        {
            // Eğer istenen sayı tüm konteynerler kadar veya fazlaysa, hepsini döndür
            if (count >= containers.Count)
                return new List<ContainerData>(containers);

            // Konteynerlerin bir kopyasını yap (orijinal listeyi etkilemesin)
            List<ContainerData> shuffled = new List<ContainerData>(containers);
            
            // RNG seed'ine göre listeyi karıştır (Fisher-Yates algoritması)
            // Aynı seed = aynı shuffle sırası
            rng.Shuffle(shuffled);

            // Karıştırılmış listeden ilk N tanesini al
            return shuffled.GetRange(0, count);
        }

        /// <summary>
        /// Target container'lara göre task rule'unu inşa et (AND/OR composite desteği ile).
        /// Karar: Basit OR rule mi yoksa kompleks composite rule mi kullanılacak?
        /// Config.CompositeRuleChance'a göre RNG ile karar verilir.
        /// </summary>
        /// <param name="rng">Seed-based randomizer</param>
        /// <param name="config">Üretim konfigürasyonu (composite şansı, max komplexity)</param>
        /// <param name="targetContainers">Hedef konteynerler (rule bu konteynerler için yazılacak)</param>
        /// <returns>Oluşturulan task rule (basit veya composite)</returns>
        private static CompositeRule BuildRule(
            DeterministicRng rng,
            GenerationConfig config,
            List<ContainerData> targetContainers)
        {
            // RNG'ye göre %X şans ile composite rule kullanılsın mı? (config'den alınan değer)
            bool useComposite = rng.Chance(config.CompositeRuleChance);

            // Eğer composite rule'u istemiyorsa VEYA max komplexity < 2 ise, basit rule kullan
            if (!useComposite || config.MaxRuleComplexity < 2)
            {
                // Basit OR rule: "ID=X OR ID=Y OR ID=Z" şeklinde
                // Tüm hedef konteynerler ID'sine göre kontrol edilir
                return BuildSimpleOrRule(targetContainers);
            }
            else
            {
                // Kompleks composite rule: Cargo/Origin kombinasyonları
                // Örnek: (Cargo=Elektronik AND Origin=Pire) OR (Cargo=Gıda) OR ID=TRBU-1042
                return BuildCompositeRule(rng, config, targetContainers);
            }
        }

        /// <summary>
        /// Basit OR rule oluştur: "ID=X OR ID=Y OR ID=Z" şeklinde.
        /// Tüm hedef konteynerler ID'sine göre kontrolü yapılır.
        /// Oyuncu bu ID'lerden herhangi birini kabul ederse DOĞRU karar yapmış olur.
        /// </summary>
        /// <param name="targetContainers">Kontrol edilecek hedef konteynerler</param>
        /// <returns>OR rule (herhangi bir ID match'i = doğru)</returns>
        private static CompositeRule BuildSimpleOrRule(List<ContainerData> targetContainers)
        {
            // LogicalOperator.Or = herhangi bir koşul match ederse tüm rule match eder
            CompositeRule rule = new CompositeRule(LogicalOperator.Or);

            // Her hedef konteyner için bir koşul ekle
            foreach (var container in targetContainers)
            {
                // Koşul: Konteyner ID'si = şu ID
                rule.AddCondition(new RuleCondition(
                    RuleConditionType.ContainerId,  // Koşul tipi: ID kontrolü
                    container.truth.containerId      // Gerçek ID (truth'tan alınıyor)
                ));
            }

            return rule;
        }

        /// <summary>
        /// Kompleks composite rule oluştur (AND/OR kombinasyonları ile).
        /// Örnek: (Cargo=Elektronik AND Origin=Pire) OR (Cargo=Gıda) OR ID=TRBU-1042
        /// Algoritma:
        /// 1. Hedef konteynerler'i gruplara böl (2-MaxComplexity tane grup)
        /// 2. Her grup için: AND grubu mu yoksa tek koşul mu?
        /// 3. Tüm grupları OR ile birleştir
        /// </summary>
        /// <param name="rng">Seed-based randomizer (grup sayısı ve tip seçimi için)</param>
        /// <param name="config">Max komplexity (kaç gruba bölüceğimizi belirler)</param>
        /// <param name="targetContainers">Hedef konteynerler (gruplara bölünecek)</param>
        /// <returns>Kompleks composite rule</returns>
        private static CompositeRule BuildCompositeRule(
            DeterministicRng rng,
            GenerationConfig config,
            List<ContainerData> targetContainers)
        {
            // Kök rule: OR operatörü ile (her grup birbirinden OR ile bağlanır)
            CompositeRule rootRule = new CompositeRule(LogicalOperator.Or);

            // Hedef konteynerler'i rastgele sayıda gruplara böl
            // Grup sayısı: 2 ile MaxRuleComplexity arasında (seed'e göre karar verilir)
            int groupCount = rng.RangeInt(2, config.MaxRuleComplexity + 1);
            List<List<ContainerData>> groups = SplitIntoGroups(rng, targetContainers, groupCount);

            // Her grup için kuralını oluştur
            foreach (var group in groups)
            {
                // Boş grup'u atla
                if (group.Count == 0)
                    continue;

                // %50 şans ile: bu grubu AND kombinasyonu ile yapla
                // %50 şans ile: bu grubu tek bir koşul ile yapla
                // Ama grup'ta 1 tane konteyner varsa, AND yapma (en az 2 koşul gerekli)
                bool useAndGroup = rng.Chance(0.5f) && group.Count > 1;

                if (useAndGroup)
                {
                    // AND grubu oluştur: (Cargo=X AND Origin=Y) şeklinde
                    // Her iki koşul da match edilirse bu AND grubu doğru olur
                    CompositeRule andRule = new CompositeRule(LogicalOperator.And);

                    // İlk konteyner'dan cargo type'ını al
                    string cargo = group[0].truth.cargoLabel;
                    andRule.AddCondition(new RuleCondition(
                        RuleConditionType.CargoType,  // Koşul: Cargo type'ı
                        cargo                         // Değer: İlk grup üyesi'nin cargo'su
                    ));

                    // İkinci konteyner'dan origin port'u al (varsa), yoksa ilk konteyner'dan
                    string origin = group.Count > 1 ? group[1].truth.originPort : group[0].truth.originPort;
                    andRule.AddCondition(new RuleCondition(
                        RuleConditionType.OriginPort,  // Koşul: Origin port'u
                        origin                          // Değer: İkinci grup üyesi'nin origin'i
                    ));

                    // AND grubu'nu kök rule'a (OR) alt rule olarak ekle
                    rootRule.AddSubRule(andRule);
                }
                else
                {
                    // Tek koşul: Cargo tipine göre veya ID'ye göre kontrol et
                    bool useCargo = rng.Chance(0.5f);  // %50 şans Cargo, %50 şans ID

                    if (useCargo)
                    {
                        // Cargo type koşulu: bu cargo'yu taşıyan tüm konteynerler hedef
                        string cargo = group[0].truth.cargoLabel;
                        rootRule.AddCondition(new RuleCondition(
                            RuleConditionType.CargoType,
                            cargo
                        ));
                    }
                    else
                    {
                        // ID koşulu: bu spesifik konteyner hedef
                        string id = group[0].truth.containerId;
                        rootRule.AddCondition(new RuleCondition(
                            RuleConditionType.ContainerId,
                            id
                        ));
                    }
                }
            }

            return rootRule;
        }

        /// <summary>
        /// Konteyner listesini rastgele gruplara böl (round-robin).
        /// Algoritma: Listeyi N gruba böl ve round-robin şeklinde dağıt.
        /// Örnek: 10 konteyner, 3 grup → Grup0: [0,3,6,9], Grup1: [1,4,7], Grup2: [2,5,8]
        /// </summary>
        /// <param name="rng">Randomizer (şu anda kullanılmıyor, ileride shuffle için)</param>
        /// <param name="containers">Gruplara bölünecek konteynerler</param>
        /// <param name="groupCount">Kaç gruba bölünecek</param>
        /// <returns>Gruplar listesi (her grup konteyner listesi)</returns>
        private static List<List<ContainerData>> SplitIntoGroups(
            DeterministicRng rng,
            List<ContainerData> containers,
            int groupCount)
        {
            // N tane boş grup oluştur
            List<List<ContainerData>> groups = new List<List<ContainerData>>();

            for (int i = 0; i < groupCount; i++)
            {
                groups.Add(new List<ContainerData>());
            }

            // Konteynerler'i round-robin algoritması ile gruplara dağıt
            // Round-robin: 0→Grup0, 1→Grup1, 2→Grup2, 3→Grup0, 4→Grup1, ... şeklinde
            for (int i = 0; i < containers.Count; i++)
            {
                // Konteyner'ın gideceği grup: (index % grup sayısı)
                int groupIndex = i % groupCount;
                groups[groupIndex].Add(containers[i]);
            }

            return groups;
        }
    }
}

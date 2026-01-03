using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Generation;
using Storia.Rules.Runtime;

namespace Storia.Generators
{
    /// <summary>
    /// Prosedürel üretim sürecini koordine eden orchestrator sınıfı.
    /// Container, task, placement ve ship üretimini doğru sırayla yönetir.
    /// 
    /// Ardışık düzen sıralaması gerekli tür sistemi tarafından ZORUNLUDUR - adımlar derleme hatası olmadan yeniden sıralanamaz.
    /// </summary>
    public sealed class GenerationOrchestrator
    {
        /// <summary>
        /// Yazılan ara sonuç - Konteyner oluşturma adımı.
        /// RNG durumu kontrol noktası konteyner oluşturulduktan sonra.
        /// </summary>
        public readonly struct ContainersGeneratedResult
        {
            public readonly List<ContainerData> Containers;
            public readonly DeterministicRng Rng;
            public readonly int RngCallCount;

            public ContainersGeneratedResult(List<ContainerData> containers, DeterministicRng rng)
            {
                Containers = containers;
                Rng = rng;
                RngCallCount = rng.GetCallCount();
            }
        }

        /// <summary>
        /// Yazılan ara sonuç - Görev kuralı oluşturma adımı.
        /// Önceki adım sonucunu gerektirir (derleme zamanında zorunlu bağımlılık).
        /// </summary>
        public readonly struct TaskRulesGeneratedResult
        {
            public readonly ContainersGeneratedResult PreviousStep;
            public readonly CompositeRule TaskRule;
            public readonly int RngCallCount;

            public TaskRulesGeneratedResult(ContainersGeneratedResult prev, CompositeRule taskRule)
            {
                PreviousStep = prev;
                TaskRule = taskRule;
                RngCallCount = prev.Rng.GetCallCount();
            }
        }

        /// <summary>
        /// Yazılan ara sonuç - Yerleştirme kuralı oluşturma adımı.
        /// Önceki adım sonucunu gerektirir (derleme zamanında zorunlu bağımlılık).
        /// </summary>
        public readonly struct PlacementGeneratedResult
        {
            public readonly TaskRulesGeneratedResult PreviousStep;
            public readonly PlacementRuleData PlacementRule;
            public readonly int RngCallCount;

            public PlacementGeneratedResult(TaskRulesGeneratedResult prev, PlacementRuleData placementRule)
            {
                PreviousStep = prev;
                PlacementRule = placementRule;
                RngCallCount = prev.PreviousStep.Rng.GetCallCount();
            }
        }

        /// <summary>
        /// Yazılan ara sonuç - Gemi oluşturma adımı (son adım).
        /// Önceki adım sonucunu gerektirir (derleme zamanında zorunlu bağımlılık).
        /// </summary>
        public readonly struct ShipsGeneratedResult
        {
            public readonly PlacementGeneratedResult PreviousStep;
            public readonly List<ShipData> Ships;
            public readonly int RngCallCount;

            public ShipsGeneratedResult(PlacementGeneratedResult prev, List<ShipData> ships)
            {
                PreviousStep = prev;
                Ships = ships;
                RngCallCount = prev.PreviousStep.PreviousStep.Rng.GetCallCount();
            }
        }

        /// <summary>
        /// Tüm prosedürel içeriği üretir ve sonuç yapısı döndürür.
        /// Ardışık düzen sıralaması gerekli tür sistemi tarafından ZORUNLUDUR - adımlar derleme hatası olmadan yeniden sıralanamaz.
        /// </summary>
        /// <param name="rng">Deterministik RNG instance</param>
        /// <param name="generationConfig">Üretim parametreleri</param>
        /// <param name="poolsConfig">Pool konfigürasyonu</param>
        /// <returns>Üretilen tüm içeriği içeren sonuç yapısı</returns>
        public static GenerationResult GenerateAll(
            DeterministicRng rng,
            GenerationConfig generationConfig,
            PoolsConfig poolsConfig)
        {
            var result = new GenerationResult();

            // === PROSEDÜREL ÜRETİM ===
            // Tip zorlamalı ardışık düzen - her adım bir önceki adımın sonucunu gerektirir

            // Adım 1: Konteynerleri oluştur (bağımlılık yok)
            var step1 = ContainerGenerator.Generate(rng, generationConfig, poolsConfig);

            // Adım 2: Görev kurallarını oluştur (ADIM 1 GEREKLİ - eksikse derleme hatası)
            var step2 = TaskRuleGenerator.Generate(step1, generationConfig);

            // Adım 3: Yerleştirme kurallarını oluştur (ADIM 2 GEREKLİ - eksikse derleme hatası)
            var step3 = PlacementRuleGenerator.Generate(step2, generationConfig, poolsConfig);

            // Adım 4: Gemileri oluştur (ADIM 3 GEREKLİ - eksikse derleme hatası)
            var step4 = ShipGenerator.Generate(step3, generationConfig, poolsConfig);

            // İç içe geçmiş zincirden nihai verileri çıkarın
            result.Containers = step1.Containers;
            result.TaskRule = step2.TaskRule;
            result.PlacementRule = step3.PlacementRule;
            result.Ships = step4.Ships;

            // RNG çağrı sayısı kontrol noktalarını sakla - deterministik regresyon tespiti için
            result.RngCallCountCheckpoints = new int[]
            {
                step1.RngCallCount,
                step2.RngCallCount,
                step3.RngCallCount,
                step4.RngCallCount
            };

            return result;
        }
    }

    /// <summary>
    /// Prosedürel üretim sonuçlarını tutan yapı.
    /// </summary>
    public sealed class GenerationResult
    {
        /// <summary>
        /// Üretilen tüm konteynerler.
        /// </summary>
        public List<ContainerData> Containers { get; set; }

        /// <summary>
        /// Üretilen görev kuralı (composite).
        /// </summary>
        public CompositeRule TaskRule { get; set; }

        /// <summary>
        /// Üretilen yerleştirme kuralı.
        /// </summary>
        public PlacementRuleData PlacementRule { get; set; }

        /// <summary>
        /// Üretilen gemiler.
        /// </summary>
        public List<ShipData> Ships { get; set; }

        /// <summary>
        /// Her nesil adımından sonra RNG çağrı sayısı kontrol noktaları.
        /// Belirlenimlilik regresyon tespiti için kullanılır.
        /// Sıra: [konteynerlerden sonra, görevlerden sonra, yerleştirmeden sonra, gemilerden sonra]
        /// </summary>
        public int[] RngCallCountCheckpoints { get; set; }
    }
}

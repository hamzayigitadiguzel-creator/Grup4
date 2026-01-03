using System.Collections.Generic;
using Storia.Data.Generated;

/// <summary>
/// Konteyner sunumunu (görünümünü) yöneten sınıf.
/// 
/// Sorumlulukları:
/// 1. Manifest + Label bilgisi üretmek (gerçek vs çelişkili)
/// 2. Çelişki oranını (conflict chance) kontrol etmek
/// 3. Manifest vs Label'dan hangisinin hatalı olacağını belirlemek
/// 
/// Singleton veya coordinator tarafından kullanılır:
/// - oyun başında: tüm gemilerin konteyner'ları için presentation oluştur
/// - aynı seed = aynı çelişkiler
/// </summary>
public sealed class ContainerPresentationManager
    {
        /// <summary>Konteyner'in çelişkili olma şansı (0-1 arası, örn: 0.3 = %30)</summary>
        private readonly float _conflictChance;
        
        /// <summary>
        /// Çelişki varsa, manifest'in hatalı olma şansı (0-1 arası).
        /// 0.7 = %70 manifest hatalı, %30 label hatalı
        /// </summary>
        private readonly float _manifestTamperBias;
        
        /// <summary>Origin pool (çelişkide farklı port seçmek için)</summary>
        private readonly IReadOnlyList<string> _originPool;
        
        /// <summary>Cargo pool (çelişkide farklı cargo type seçmek için)</summary>
        private readonly IReadOnlyList<string> _cargoPool;

        /// <summary>
        /// Manager'ı başlat.
        /// </summary>
        /// <param name="conflictChance">Çelişki olma olasılığı (0-1)</param>
        /// <param name="manifestTamperBias">Manifest'in hatalı olma şansı (0-1)</param>
        /// <param name="originPool">Mevcut origin port'ları (havuz)</param>
        /// <param name="cargoPool">Mevcut cargo type'ları (havuz)</param>
        public ContainerPresentationManager(
            float conflictChance,
            float manifestTamperBias,
            IReadOnlyList<string> originPool,
            IReadOnlyList<string> cargoPool)
        {
            _conflictChance = conflictChance;
            _manifestTamperBias = manifestTamperBias;
            _originPool = originPool;
            _cargoPool = cargoPool;
        }

        /// <summary>
        /// Verilen konteyner için presentation oluştur (manifest + label).
        /// </summary>
        /// <param name="containerData">Konteyner'in gerçek verisi (truth)</param>
        /// <param name="rng">Seed-based RNG (determinism için)</param>
        /// <returns>Manifest ve label bilgisi (çelişkili veya değil)</returns>
        public ContainerPresentation CreatePresentation(ContainerData containerData, DeterministicRng rng)
        {
            // Manager'ın ayarlarını factory'ye geç
            PresentationTuning tuning = new PresentationTuning
            {
                conflictChance = _conflictChance,
                manifestTamperBias = _manifestTamperBias
            };

            // Factory'ye tüm parametreleri geç, oluşturulmuş presentation al
            return ContainerPresentationFactory.Build(
                containerData,
                tuning,
                rng,
                _originPool,
                _cargoPool);
        }
}

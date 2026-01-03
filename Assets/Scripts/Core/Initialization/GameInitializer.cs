using System.Collections.Generic;
using Storia.Constants;
using Storia.Data.Generated;
using Storia.Rules.Runtime;
using Storia.Generation;
using Storia.Generators;
using Storia.Diagnostics;

namespace Storia.Core.Initialization
{
    /// <summary>
    /// Oyun başlatma sürecini yöneten ana sınıf.
    /// 
    /// Sorumluluklar:
    /// 1. RNG sistemini başlatmak (seed çözümü)
    /// 2. Prosedürel içerik üretimini tetiklemek
    /// 3. Manager'ları oluşturmak (ManagerFactory kullanarak)
    /// 4. Scene temizliği (eski günün konteynerleri, zone'lar)
    /// 
    /// Bu sınıf stateless'dır - her initialization için yeni bir result döner.
    /// </summary>
    public sealed class GameInitializer
    {
        /// <summary>
        /// Oyunu başlatır ve InitializationResult döner.
        /// </summary>
        public InitializationResult InitializeGame(
            DaySeedConfig daySeedConfig,
            GenerationConfig generationConfig,
            PoolsConfig poolsConfig,
            SceneReferencesProvider sceneRefs)
        {
            // Step 1: RNG sistemini başlat
            int runSeed = ResolveRunSeed(daySeedConfig);
            var rng = new DeterministicRng(runSeed);

            // Step 2: Prosedürel içerik üret (containers, ships, rules)
            var generatedContent = GenerateGameContent(rng, generationConfig, poolsConfig);

            // Step 3: Manager'ları oluştur
            var (decisionManager, deckManager, presentationManager) = ManagerFactory.CreateManagers(
                generatedContent.Containers,
                generatedContent.TaskRule,
                generatedContent.PlacementRule,
                generationConfig,
                poolsConfig,
                rng,
                sceneRefs.PlacementZones
            );

            // Step 4: Scene cleanup (eski günün konteynerleri)
            CleanupScene(sceneRefs);

            // Step 5: InitializationResult oluştur ve döndür
            return new InitializationResult(
                generatedContainers: generatedContent.Containers,
                generatedShips: generatedContent.Ships,
                taskRule: generatedContent.TaskRule,
                placementRule: generatedContent.PlacementRule,
                decisionManager: decisionManager,
                deckManager: deckManager,
                presentationManager: presentationManager,
                runSeed: runSeed,
                rng: rng
            );
        }

        /// <summary>
        /// Seed resolution - DaySeedConfig veya DevRunSeedOverride'dan seed'i çözümle.
        /// </summary>
        private int ResolveRunSeed(DaySeedConfig daySeedConfig)
        {
#if UNITY_EDITOR
            if (DevRunSeedOverride.HasOverride)
                return DevRunSeedOverride.OverrideSeed;
#endif

            if (daySeedConfig != null)
                return daySeedConfig.GetRunSeed();

            return GameConstants.FallbackSeed;
        }

        /// <summary>
        /// Prosedürel içerik üretimi - GenerationOrchestrator'a delegate eder.
        /// </summary>
        private (List<ContainerData> Containers, List<ShipData> Ships, CompositeRule TaskRule, PlacementRuleData PlacementRule) 
            GenerateGameContent(DeterministicRng rng, GenerationConfig config, PoolsConfig pools)
        {
            var result = GenerationOrchestrator.GenerateAll(rng, config, pools);

            return (
                result.Containers,
                result.Ships,
                result.TaskRule,
                result.PlacementRule
            );
        }

        /// <summary>
        /// Scene cleanup - eski günün konteynerleri ve zone'ları temizle.
        /// </summary>
        private void CleanupScene(SceneReferencesProvider sceneRefs)
        {
            // 3D konteynerleri temizle
            sceneRefs.ContainerSpawner?.ClearPlacedContainers();

            // Placement zone'ları sıfırla
            if (sceneRefs.PlacementZones != null)
            {
                foreach (var zone in sceneRefs.PlacementZones)
                {
                    zone?.ResetZone();
                }
            }
        }
    }
}

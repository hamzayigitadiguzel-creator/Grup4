using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Rules.Runtime;
using Storia.Managers.Decision;
using Storia.Managers.Deck;
using Storia.Generation;
using Storia.Core.Placement;

namespace Storia.Core.Initialization
{
    /// <summary>
    /// Runtime manager'ların oluşturulmasından sorumlu factory class.
    /// Single Responsibility: Manager instantiation ve konfigürasyon.
    /// </summary>
    public static class ManagerFactory
    {
        /// <summary>
        /// Oluşturulan içeriğe dayalı olarak tüm manager'ları oluşturur.
        /// </summary>
        public static (DecisionManager, DeckManager, ContainerPresentationManager) CreateManagers(
            List<ContainerData> generatedContainers,
            CompositeRule taskRule,
            PlacementRuleData placementRule,
            GenerationConfig generationConfig,
            PoolsConfig poolsConfig,
            DeterministicRng rng,
            PlacementZone[] placementZones)
        {
            // DecisionManager - karar takibi
            var decisionManager = new DecisionManager(taskRule, placementRule);
            decisionManager.Reset();

            // DeckManager - konteyner destesi yönetimi
            var deckManager = new DeckManager(generatedContainers);
            deckManager.ShuffleDeck(rng);

            // ContainerPresentationManager - bilgi bozulması sistemi
            var presentationManager = new ContainerPresentationManager(
                generationConfig.ConflictChance,
                generationConfig.ManifestTamperBias,
                poolsConfig.OriginPorts,
                poolsConfig.CargoTypes
            );

            return (decisionManager, deckManager, presentationManager);
        }

        /// <summary>
        /// Placement zone'ları dictionary'e dönüştürür.
        /// </summary>
        public static Dictionary<int, PlacementZone> CreateZoneMap(PlacementZone[] placementZones)
        {
            var zoneMap = new Dictionary<int, PlacementZone>();
            
            if (placementZones != null)
            {
                foreach (var zone in placementZones)
                {
                    if (zone != null)
                    {
                        zoneMap[zone.ZoneId] = zone;
                    }
                }
            }

            return zoneMap;
        }
    }
}

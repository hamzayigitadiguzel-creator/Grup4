using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Rules.Runtime;
using Storia.Managers.Decision;
using Storia.Managers.Deck;

namespace Storia.Core.Initialization
{
    /// <summary>
    /// Initialization sonuçlarını tutan veri yapısı.
    /// GameInitializer tarafından üretilir, Controller tarafından kullanılır.
    /// </summary>
    public sealed class InitializationResult
    {
        // Oluşturulan İçerik
        public List<ContainerData> GeneratedContainers { get; }
        public List<ShipData> GeneratedShips { get; }
        public CompositeRule TaskRule { get; }
        public PlacementRuleData PlacementRule { get; }

        // Runtime Yöneticiler
        public DecisionManager DecisionManager { get; }
        public DeckManager DeckManager { get; }
        public ContainerPresentationManager PresentationManager { get; }

        // Metadata
        public int RunSeed { get; }
        public DeterministicRng Rng { get; }

        public InitializationResult(
            List<ContainerData> generatedContainers,
            List<ShipData> generatedShips,
            CompositeRule taskRule,
            PlacementRuleData placementRule,
            DecisionManager decisionManager,
            DeckManager deckManager,
            ContainerPresentationManager presentationManager,
            int runSeed,
            DeterministicRng rng)
        {
            GeneratedContainers = generatedContainers;
            GeneratedShips = generatedShips;
            TaskRule = taskRule;
            PlacementRule = placementRule;
            DecisionManager = decisionManager;
            DeckManager = deckManager;
            PresentationManager = presentationManager;
            RunSeed = runSeed;
            Rng = rng;
        }
    }
}

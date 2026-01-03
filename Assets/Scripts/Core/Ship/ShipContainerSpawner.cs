using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Core.Spawning;
using Storia.Managers.Deck;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi cargo hold'undaki konteyner spawn işlemlerinden sorumlu.
    /// 
    /// Sorumluluklar:
    /// - Gemiye ait konteynerleri deck'ten almak
    /// - Her konteyner için presentation oluşturmak (deterministik)
    /// - Cargo hold'da spawn etmek
    /// - Kalan konteynerleri otomatik red etmek
    /// 
    /// Tek Sorumluluk Prensibi: Gemi-spesifik konteyner spawning.
    /// </summary>
    public sealed class ShipContainerSpawner
    {
        private readonly ContainerPresentationManager _presentationManager;
        private readonly ContainerSpawner _containerSpawner;

        public ShipContainerSpawner(
            DeckManager deckManager,
            ContainerPresentationManager presentationManager,
            ContainerSpawner containerSpawner)
        {
            _presentationManager = presentationManager;
            _containerSpawner = containerSpawner;
        }

        /// <summary>
        /// Geminin konteynerlerini cargo hold'da spawn eder.
        /// Her konteyner için deterministik presentation oluşturur.
        /// </summary>
        public void SpawnContainersInCargo(
            ShipInstance ship,
            ShipMovement shipMovement,
            DeterministicRng rng,
            System.Action<int, ContainerData, ContainerPresentation> onContainerSpawned = null)
        {
            if (shipMovement == null || _containerSpawner == null)
                return;

            // Geminin konteynerlerini deck'ten al
            var shipContainers = GetContainersForShip(ship);

            // Her konteyner için presentation oluştur (deterministik)
            var presentations = new ContainerPresentation[shipContainers.Count];
            for (int i = 0; i < shipContainers.Count; i++)
            {
                presentations[i] = _presentationManager.CreatePresentation(shipContainers[i], rng);

                // Callback - dev mode logging için
                onContainerSpawned?.Invoke(i, shipContainers[i], presentations[i]);
            }

            // Cargo hold'da spawn et (presentation'larla birlikte)
            _containerSpawner.SpawnAllContainersInShip(shipContainers, presentations, shipMovement);
        }

        /// <summary>
        /// Cargo hold'da kalan konteynerleri otomatik olarak red eder.
        /// </summary>
        public List<ContainerData> AutoRejectRemainingContainers(ShipMovement shipMovement)
        {
            var rejectedContainers = new List<ContainerData>();

            if (shipMovement == null)
                return rejectedContainers;

            var remainingContainers = shipMovement.GetAllCargoContainers();
            if (remainingContainers.Count > 0)
            {
                UnityEngine.Debug.Log($"[ShipContainerSpawner] Cargo hold'da {remainingContainers.Count} konteyner kaldı. Otomatik olarak red ediliyor.");

                // Kalan tüm konteynerlerin data'sını topla
                foreach (var container in remainingContainers)
                {
                    if (container != null && container.ContainerData != null)
                    {
                        rejectedContainers.Add(container.ContainerData);
                    }
                }
            }

            return rejectedContainers;
        }

        /// <summary>
        /// Gemi için konteyner listesini döndürür.
        /// </summary>
        private List<ContainerData> GetContainersForShip(ShipInstance ship)
        {
            return ship.Containers;
        }
    }
}

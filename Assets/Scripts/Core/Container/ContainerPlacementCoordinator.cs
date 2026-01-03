using System.Collections.Generic;
using UnityEngine;
using Storia.Data.Generated;
using Storia.Presentation;
using Storia.Managers.Decision;
using Storia.Core.Placement;
using Storia.Core.Spawning;
using Storia.UI;

namespace Storia.Core.Container
{
    /// <summary>
    /// Konteyner yerleştirme koordinasyonundan sorumlu.
    /// 
    /// Sorumluluklar:
    /// - Zone seçimi handling
    /// - Placement validation (DecisionManager)
    /// - Fiziksel yerleştirme (ContainerSpawner + PlacementZone)
    /// - UI state yönetimi
    /// 
    /// Single Responsibility: Container placement coordination.
    /// </summary>
    public sealed class ContainerPlacementCoordinator
    {
        private readonly DecisionManager _decisionManager;
        private readonly ContainerSpawner _containerSpawner;
        private readonly Dictionary<int, PlacementZone> _zoneMap;
        private readonly IWorkstationView _workstation;

        // Callbacks
        public event System.Action OnPlacementCompleted;

        public ContainerPlacementCoordinator(
            DecisionManager decisionManager,
            ContainerSpawner containerSpawner,
            Dictionary<int, PlacementZone> zoneMap,
            IWorkstationView workstation)
        {
            _decisionManager = decisionManager;
            _containerSpawner = containerSpawner;
            _zoneMap = zoneMap;
            _workstation = workstation;
        }

        /// <summary>
        /// Zone seçildiğinde çağrılır - konteyneri yerleştirir.
        /// </summary>
        public void PlaceContainer(
            ContainerData containerData,
            ContainerPresentation presentation,
            int zoneId)
        {
            // Decision manager'a placement kararını kaydet
            _decisionManager.RegisterPlacement(containerData, zoneId, presentation);

            // Fiziksel yerleştirme
            PlaceContainerPhysically(zoneId);

            // UI cleanup
            _workstation?.ShowPlacement(false);

            // Controller'a completion bildirimi
            OnPlacementCompleted?.Invoke();
        }

        /// <summary>
        /// Konteyneri fiziksel olarak zone'a yerleştirir.
        /// </summary>
        private void PlaceContainerPhysically(int zoneId)
        {
            if (_containerSpawner == null || _zoneMap == null)
                return;

            if (_zoneMap.ContainsKey(zoneId))
            {
                PlacementZone targetZone = _zoneMap[zoneId];
                Vector3 targetPos = targetZone.GetNextSlotPosition();
                _containerSpawner.MoveCurrentContainerToZone(targetPos);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[ContainerPlacementCoordinator] ZoneId not found: {zoneId}");
            }
        }
    }
}

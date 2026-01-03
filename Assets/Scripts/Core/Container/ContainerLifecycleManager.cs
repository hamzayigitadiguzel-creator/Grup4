using UnityEngine;
using Storia.Data.Generated;
using Storia.Presentation;
using Storia.Core.Spawning;
using Storia.Core.Ship;
using Storia.UI;
using Storia.Core.GameFlow;

namespace Storia.Core.Container
{
    /// <summary>
    /// Konteyner lifecycle akışını yöneten manager.
    /// 
    /// Sorumluluklar:
    /// - NextContainer logic (cargo hold'dan vinç'e taşıma)
    /// - Container UI gösterimi
    /// - Current container tracking
    /// - Vinç pozisyon hesaplama
    /// 
    /// Single Responsibility: Container flow management.
    /// </summary>
    public sealed class ContainerLifecycleManager
    {
        private readonly IShipService _shipManager;
        private readonly ContainerSpawner _containerSpawner;
        private readonly IWorkstationView _workstation;
        private readonly GameFlowStateMachine _stateMachine;
        private readonly StateTransitionHandler _stateTransitionHandler;

        // Current container tracking
        private ContainerViewController _currentContainer;
        private ContainerData _currentContainerData;
        private ContainerPresentation _currentPresentation;

        // Callbacks
        public event System.Action OnContainerReady;
        public event System.Action OnNoMoreContainers;

        public ContainerViewController CurrentContainer => _currentContainer;
        public ContainerData CurrentContainerData => _currentContainerData;
        public ContainerPresentation CurrentPresentation => _currentPresentation;

        public ContainerLifecycleManager(
            IShipService shipManager,
            ContainerSpawner containerSpawner,
            IWorkstationView workstation,
            GameFlowStateMachine stateMachine,
            StateTransitionHandler stateTransitionHandler)
        {
            _shipManager = shipManager;
            _containerSpawner = containerSpawner;
            _workstation = workstation;
            _stateMachine = stateMachine;
            _stateTransitionHandler = stateTransitionHandler;
        }

        /// <summary>
        /// Cargo hold'dan sonraki konteyneri vinç'e taşır ve gösterir.
        /// </summary>
        public void NextContainer()
        {
            // UI temizle
            _workstation?.ShowPlacement(false);

            var shipMovement = _shipManager?.GetCurrentShipMovement();
            if (shipMovement == null)
            {
                OnNoMoreContainers?.Invoke();
                return;
            }

            // Cargo hold'dan sonraki konteyneri al
            var cargoContainers = shipMovement.GetAllCargoContainers();
            if (cargoContainers.Count == 0)
            {
                // Cargo hold boş, tüm konteynerler işlendi
                OnNoMoreContainers?.Invoke();
                return;
            }

            // İlk cargo konteyneri al
            var containerController = cargoContainers[0];
            if (containerController == null)
            {
                OnNoMoreContainers?.Invoke();
                return;
            }

            // Vinç pozisyonuna taşı
            MoveContainerToCrane(shipMovement, containerController);

            // ContainerData ve Presentation'ı al
            if (containerController.ContainerData == null)
            {
                NextContainer(); // Recursive call - geçersiz konteyner, sonrakini dene
                return;
            }

            // Current container tracking
            _currentContainer = containerController;
            _currentContainerData = containerController.ContainerData;
            _currentPresentation = containerController.Presentation;

            // ContainerSpawner'a bildir
            _containerSpawner?.SetCurrentContainer(_currentContainer);

            // UI'ı göster
            DisplayContainer();
            // State machine transition - ShipDecision'dan sonra ilk konteyner için ContainerEvaluation'a geç
            if (_stateMachine != null && _stateTransitionHandler != null)
            {
                if (_stateMachine.CurrentState == GameState.ShipDecision)
                {
                    _stateTransitionHandler.HandleShipAccepted();
                }
                else if (_stateMachine.CurrentState == GameState.PlacementSelection)
                {
                    // Bir önceki konteyner yerleştirildi, yeni konteyner için tekrar ContainerEvaluation'a geç
                    _stateTransitionHandler.HandleContainerProcessed();
                }
            }
            // Controller'a hazır olduğunu bildir
            OnContainerReady?.Invoke();
        }

        /// <summary>
        /// Konteyneri cargo hold'dan vinç pozisyonuna taşır.
        /// </summary>
        private void MoveContainerToCrane(ShipMovement shipMovement, ContainerViewController containerController)
        {
            Vector3 cranePosition = CalculateCranePosition();
            shipMovement.MoveContainerToCrane(containerController, cranePosition);
        }

        /// <summary>
        /// Vinç pozisyonunu hesaplar.
        /// </summary>
        private Vector3 CalculateCranePosition()
        {
            if (_containerSpawner != null && _containerSpawner.GetInitialSpawnPoint() != null)
            {
                return _containerSpawner.GetInitialSpawnPoint().position + _containerSpawner.GetSpawnOffset();
            }

            // Fallback
            return Vector3.zero + Vector3.up * 2f;
        }

        /// <summary>
        /// Container UI'ını gösterir.
        /// </summary>
        private void DisplayContainer()
        {
            if (_currentPresentation == null) return;

            _workstation?.ShowContainer(
                _currentPresentation.manifestShown,
                _currentPresentation.labelShown
            );
        }

        /// <summary>
        /// Red edilen konteyneri cargo hold'a geri taşır.
        /// </summary>
        public void ReturnContainerToCargo()
        {
            var shipMovement = _shipManager?.GetCurrentShipMovement();
            if (shipMovement != null && _currentContainer != null)
            {
                shipMovement.ReturnContainerToCargo(_currentContainer);
            }
        }

        /// <summary>
        /// Kabul edilen konteyneri cargo hold'dan kaldırır.
        /// </summary>
        public void RemoveContainerFromCargo()
        {
            var shipMovement = _shipManager?.GetCurrentShipMovement();
            if (shipMovement != null && _currentContainer != null)
            {
                shipMovement.RemoveContainerFromCargo(_currentContainer);
            }
        }
    }
}

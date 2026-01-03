using System;
using Storia.UI;
using Storia.Managers.Decision;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi lifecycle'ını koordine eden üst seviye orchestrator.
    /// 
    /// Sorumluluklar:
    /// - Gemi arrival flow'unu yönetmek
    /// - Gemi decision'larını koordine etmek
    /// - Container işlemlerini başlatmak
    /// - Gemi departure'ını tetiklemek
    /// 
    /// Bu sınıf high-level coordination yapar, detayları delegate eder.
    /// Single Responsibility: Gemi yaşam döngüsü düzenlemesini koordine etmek.
    /// </summary>
    public sealed class ShipLifecycleCoordinator
    {
        private readonly IShipService _shipManager;
        private readonly ShipContainerSpawner _containerSpawner;
        private readonly ShipDecisionHandler _decisionHandler;
        private readonly IWorkstationView _workstation;
        private readonly DecisionManager _decisionManager;
        private readonly Storia.Diagnostics.DevModeManager _devModeManager;

        // Callbacks - Controller'a event'ler döndürmek için
        public event Action OnShipCompleted;
        public event Action OnReadyForNextContainer;

        public ShipLifecycleCoordinator(
            IShipService shipManager,
            ShipContainerSpawner containerSpawner,
            ShipDecisionHandler decisionHandler,
            IWorkstationView workstation,
            DecisionManager decisionManager,
            Storia.Diagnostics.DevModeManager devModeManager)
        {
            _shipManager = shipManager;
            _containerSpawner = containerSpawner;
            _decisionHandler = decisionHandler;
            _workstation = workstation;
            _decisionManager = decisionManager;
            _devModeManager = devModeManager;
        }

        /// <summary>
        /// Yeni gemi geldiğinde çağrılır. Tüm arrival flow'unu koordine eder.
        /// </summary>
        public void ProcessShipArrival(
            ShipInstance ship,
            DeterministicRng rng,
            ref int containerIndex)
        {
            if (ship == null) return;

            // Dev mode logging
            LogShipArrival(ship);

            // Cargo hold'da konteyner spawn
            var shipMovement = _shipManager?.GetCurrentShipMovement();
            if (shipMovement != null)
            {
                // Lambda'dan önce containerIndex değerini yakala
                int currentIndex = containerIndex;

                _containerSpawner.SpawnContainersInCargo(
                    ship,
                    shipMovement,
                    rng,
                    (idx, data, presentation) =>
                    {
                        // Dev mode'da konteyneri logla
                        _devModeManager?.LogContainer(currentIndex + idx, data, presentation);
                    });

                // Yumurtladıktan sonra containerIndex'i güncelle
                containerIndex = currentIndex + ship.Definition.containerCount;
            }

            // Gemi bilgilerini göster
            ShowShipDecisionUI(ship);
        }

        /// <summary>
        /// Oyuncu gemiyi kabul ettiğinde.
        /// </summary>
        public void HandleShipAccepted(ShipInstance ship)
        {
            if (ship == null) return;

            _shipManager?.MakeShipDecision(ship, accepted: true);
            _decisionHandler.HandleShipAccepted();

            // Controller'a konteyner işlemeye başlayabileceğini bildir
            OnReadyForNextContainer?.Invoke();
        }

        /// <summary>
        /// Oyuncu gemiyi red ettiğinde.
        /// </summary>
        public void HandleShipRejected(ShipInstance ship)
        {
            if (ship == null) return;

            _shipManager?.MakeShipDecision(ship, accepted: false);
            _decisionHandler.HandleShipRejected();

            // Gemi ayrılmasını tetikle
            CompleteCurrentShip(ship);
        }

        /// <summary>
        /// Mevcut gemi tamamlandığında (konteynerler işlendi veya red edildi).
        /// </summary>
        public void CompleteCurrentShip(ShipInstance ship)
        {
            if (ship == null) return;

            // Cargo hold'da kalan konteynerleri otomatik red et
            var shipMovement = _shipManager?.GetCurrentShipMovement();
            if (shipMovement != null)
            {
                var remainingContainers = _containerSpawner.AutoRejectRemainingContainers(shipMovement);

                // Kalan konteynerleri decision manager'a kaydet
                foreach (var containerData in remainingContainers)
                {
                    // Oyuncu tarafından işlenmeyen konteynerleri otomatik red et
                    _decisionManager.RegisterDecision(containerData, false, null);
                }
            }

            // UI temizle
            _decisionHandler.CleanupShipUI();

            // Gemi fiziksel ayrılışını başlat
            PrepareShipDeparture(ship);

            // Controller'a completion event gönder
            OnShipCompleted?.Invoke();
        }

        /// <summary>
        /// Dev mode'da gemi bilgilerini loglar.
        /// </summary>
        private void LogShipArrival(ShipInstance ship)
        {
            _devModeManager?.LogShip(
                ship.Definition.shipName,
                ship.Definition.shipId,
                ship.Definition.originPort,
                ship.Definition.containerCount,
                ship.Definition.voyageDurationHours
            );
        }

        /// <summary>
        /// Gemi karar panelini gösterir.
        /// </summary>
        private void ShowShipDecisionUI(ShipInstance ship)
        {
            _workstation?.ShowShipInfo(
                ship.Definition.shipName,
                ship.Definition.shipId,
                ship.Definition.originPort,
                ship.Definition.containerCount,
                ship.Definition.voyageDurationHours
            );
        }

        /// <summary>
        /// Gemi fiziksel ayrılışını başlatır.
        /// </summary>
        private void PrepareShipDeparture(ShipInstance ship)
        {
            if (_shipManager != null && _shipManager.CurrentShip == ship)
            {
                var shipMovement = _shipManager.GetCurrentShipMovement();
                shipMovement?.InitiateDeparture();
            }
        }
    }
}

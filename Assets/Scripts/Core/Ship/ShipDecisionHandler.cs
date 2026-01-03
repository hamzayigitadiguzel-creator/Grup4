using Storia.UI;
using Storia.Interaction;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi kabul/red kararlarını handle eden sınıf.
    /// 
    /// Sorumluluklar:
    /// - Gemi kabul kararını işlemek
    /// - Gemi red kararını işlemek
    /// - UI güncellemelerini koordine etmek
    /// 
    /// Single Responsibility: Gemi karar işlemesini koordine etmek.
    /// </summary>
    public sealed class ShipDecisionHandler
    {
        private readonly IWorkstationView _workstation;
        private readonly InteractionPoint _craneInteractionPoint;

        public ShipDecisionHandler(
            IWorkstationView workstation,
            InteractionPoint craneInteractionPoint)
        {
            _workstation = workstation;
            _craneInteractionPoint = craneInteractionPoint;
        }

        /// <summary>
        /// Gemi kabul edildiğinde çağrılır.
        /// UI'ı günceller ve konteyner işlemeye hazır hale getirir.
        /// </summary>
        public void HandleShipAccepted()
        {
            _workstation?.HideShipInfo();
        }

        /// <summary>
        /// Gemi red edildiğinde çağrılır.
        /// UI'ı temizler ve etkileşimi kapatır.
        /// </summary>
        public void HandleShipRejected()
        {
            // UI temizle
            _workstation?.HideShipInfo();
            _workstation?.HideContainer();
            _workstation?.ShowPlacement(false);

            // Etkileşimi kapat
            _craneInteractionPoint?.ForceCloseInteraction();
        }

        /// <summary>
        /// Gemi tamamlandığında (konteyner işlemleri bitti) UI'ı temizler.
        /// </summary>
        public void CleanupShipUI()
        {
            _workstation?.HideShipInfo();
            _workstation?.HideContainer();
            _workstation?.ShowPlacement(false);
            _craneInteractionPoint?.ForceCloseInteraction();
        }
    }
}

namespace Storia.UI.Coordination
{
    /// <summary>
    /// Workstation (World-space) UI kontrolünden sorumlu controller.
    /// 
    /// Sorumluluklar:
    /// - Container bilgi paneli gösterimi
    /// - Ship bilgi paneli gösterimi
    /// - Placement zone seçim paneli
    /// - Panel state yönetimi
    /// 
    /// Single Responsibility: Workstation kullanıcı arayüzü durum yönetimi.
    /// </summary>
    public sealed class WorkstationUIController
    {
        private readonly IWorkstationView _workstation;

        public WorkstationUIController(IWorkstationView workstation)
        {
            _workstation = workstation;
        }

        /// <summary>
        /// Container bilgilerini gösterir (manifest + label).
        /// </summary>
        public void ShowContainer(ContainerPresentation presentation)
        {
            if (presentation == null) return;

            _workstation?.ShowContainer(
                presentation.manifestShown,
                presentation.labelShown
            );
        }

        /// <summary>
        /// Container panelini gizler.
        /// </summary>
        public void HideContainer()
        {
            _workstation?.HideContainer();
        }

        /// <summary>
        /// Ship bilgi panelini gösterir.
        /// </summary>
        public void ShowShipInfo(string shipName, string shipId, string originPort, int containerCount, float voyageHours)
        {
            _workstation?.ShowShipInfo(shipName, shipId, originPort, containerCount, voyageHours);
        }

        /// <summary>
        /// Ship panelini gizler.
        /// </summary>
        public void HideShipInfo()
        {
            _workstation?.HideShipInfo();
        }

        /// <summary>
        /// Placement zone seçim panelini gösterir/gizler.
        /// </summary>
        public void ShowPlacementPanel(bool show)
        {
            _workstation?.ShowPlacement(show);
        }

        /// <summary>
        /// Tüm workstation panellerini gizler (cleanup).
        /// </summary>
        public void HideAllPanels()
        {
            _workstation?.HideShipInfo();
            _workstation?.HideContainer();
            _workstation?.ShowPlacement(false);
        }

        /// <summary>
        /// Ship panel'in aktif olup olmadığını kontrol eder.
        /// </summary>
        public bool IsShipPanelActive => _workstation?.IsShipPanelActive ?? false;
    }
}

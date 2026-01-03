using System;

namespace Storia.UI
{
    /// <summary>
    /// Workstation (İş İstasyonu) interface - 3D dünyada etkileşim noktalarında görünen UI.
    /// World-Space Canvas'ta render edilir.
    /// Hem gemi kararı hem de konteyner değerlendirmesi için kullanılır.
    /// </summary>
    public interface IWorkstationView
    {
        // ============ Gemi Paneli ============
        
        /// <summary>
        /// Gemi bilgileri panelini göster.
        /// </summary>
        void ShowShipInfo(string shipName, string shipId, string originPort, 
                         int containerCount, float voyageHours);

        /// <summary>
        /// Gemi kararı butonlarını bağla.
        /// </summary>
        void BindShipDecisionButtons(Action onAccept, Action onReject);

        /// <summary>
        /// Gemi panelini gizle.
        /// </summary>
        void HideShipInfo();

        /// <summary>
        /// Gemi paneli aktif mi?
        /// </summary>
        bool IsShipPanelActive { get; }

        /// <summary>
        /// Konteyner panelini gizle.
        /// </summary>
        void HideContainer();

        // ============ Konteyner Paneli ============
        
        /// <summary>
        /// Konteyner değerlendirme UI'sini göster.
        /// </summary>
        void ShowContainer(ContainerFields manifestShown, ContainerFields labelShown);

        /// <summary>
        /// Kabul/Red karar butonlarını bağla.
        /// </summary>
        void BindDecisionButtons(Action onAccept, Action onReject);

        /// <summary>
        /// Placement (Zone seçimi) panelini göster/gizle.
        /// </summary>
        void ShowPlacement(bool show);

        /// <summary>
        /// Placement butonlarını bağla.
        /// </summary>
        void BindPlacementButtons(Action<int> onZonePicked);
    }
}

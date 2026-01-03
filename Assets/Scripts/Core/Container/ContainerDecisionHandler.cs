using Storia.Data.Generated;
using Storia.Presentation;
using Storia.Managers.Decision;
using Storia.UI;

namespace Storia.Core.Container
{
    /// <summary>
    /// Konteyner kabul/red kararlarını handle eden sınıf.
    /// 
    /// Sorumluluklar:
    /// - Accept button logic
    /// - Reject button logic
    /// - DecisionManager ile entegrasyon
    /// - Placement UI tetikleme
    /// 
    /// Single Responsibility: Container decision handling.
    /// </summary>
    public sealed class ContainerDecisionHandler
    {
        private readonly DecisionManager _decisionManager;
        private readonly IWorkstationView _workstation;

        // Callbacks - Controller'a event'ler için
        public event System.Action OnPlacementRequested;
        public event System.Action OnContainerProcessed;

        public ContainerDecisionHandler(
            DecisionManager decisionManager,
            IWorkstationView workstation)
        {
            _decisionManager = decisionManager;
            _workstation = workstation;
        }

        /// <summary>
        /// Accept button pressed - konteyner kabul edildi.
        /// </summary>
        public void HandleAccept(ContainerData containerData, ContainerPresentation presentation)
        {
            bool needsPlacement = _decisionManager.RegisterDecision(containerData, true, presentation);

            if (needsPlacement)
            {
                // Placement UI'ı göster
                _workstation?.ShowPlacement(true);
                
                // Controller'a placement beklendiğini bildir
                OnPlacementRequested?.Invoke();
            }
        }

        /// <summary>
        /// Reject button pressed - konteyner red edildi.
        /// </summary>
        public void HandleReject(ContainerData containerData, ContainerPresentation presentation)
        {
            _decisionManager.RegisterDecision(containerData, false, presentation);

            // Controller'a container işleminin tamamlandığını bildir
            OnContainerProcessed?.Invoke();
        }
    }
}

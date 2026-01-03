using System;

namespace Storia.Core.GameFlow
{
    /// <summary>
    /// Oyun durumu geçişlerini yöneten koordinatör.
    /// 
    /// Sorumluluklar:
    /// - Yüksek seviyeli durum geçiş mantığı
    /// - Olay tabanlı durum tetikleme
    /// - Durum makinesi koordinasyonu
    /// 
    /// Single Responsibility: Durum geçişlerinin orkestrasyonu.
    /// </summary>
    public sealed class StateTransitionHandler
    {
        private readonly GameFlowStateMachine _stateMachine;

        /// <summary>
        /// Durum değişikliği tamamlandığında tetiklenir (action callbacks için).
        /// </summary>
        public event Action<GameState> OnTransitionCompleted;

        public StateTransitionHandler(GameFlowStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        /// <summary>
        /// Gün başlangıcını işler.
        /// </summary>
        public void StartDay()
        {
            _stateMachine.TransitionTo(GameState.AwaitingShip);
            OnTransitionCompleted?.Invoke(GameState.AwaitingShip);
        }

        /// <summary>
        /// Gemi gelişini işler.
        /// </summary>
        public void HandleShipArrival()
        {
            _stateMachine.TransitionTo(GameState.ShipArrival);
            
            // Ship arrival animasyonu bittikten sonra decision state'e geç
            _stateMachine.TransitionTo(GameState.ShipDecision);
            OnTransitionCompleted?.Invoke(GameState.ShipDecision);
        }

        /// <summary>
        /// Gemi kabul edildiğinde.
        /// </summary>
        public void HandleShipAccepted()
        {
            _stateMachine.TransitionTo(GameState.ContainerEvaluation);
            OnTransitionCompleted?.Invoke(GameState.ContainerEvaluation);
        }

        /// <summary>
        /// Gemi reddedildiğinde.
        /// </summary>
        public void HandleShipRejected()
        {
            // Gemi reddedildi, bir sonraki gemiyi bekle
            _stateMachine.TransitionTo(GameState.AwaitingShip);
            OnTransitionCompleted?.Invoke(GameState.AwaitingShip);
        }

        /// <summary>
        /// Konteyner kabul edilip placement bekleniyor.
        /// </summary>
        public void HandleContainerAccepted()
        {
            _stateMachine.TransitionTo(GameState.PlacementSelection);
            OnTransitionCompleted?.Invoke(GameState.PlacementSelection);
        }

        /// <summary>
        /// Konteyner reddedildi veya yerleştirildi, bir sonraki konteynere geç.
        /// </summary>
        public void HandleContainerProcessed()
        {
            _stateMachine.TransitionTo(GameState.ContainerEvaluation);
            OnTransitionCompleted?.Invoke(GameState.ContainerEvaluation);
        }

        /// <summary>
        /// Gemideki tüm konteynerler işlendi.
        /// </summary>
        public void HandleShipCompleted()
        {
            _stateMachine.TransitionTo(GameState.AwaitingShip);
            OnTransitionCompleted?.Invoke(GameState.AwaitingShip);
        }

        /// <summary>
        /// Gün sona erdi.
        /// </summary>
        public void EndDay()
        {
            _stateMachine.TransitionTo(GameState.DayEnd);
            OnTransitionCompleted?.Invoke(GameState.DayEnd);
        }

        /// <summary>
        /// Player input kabul edilebilir mi?
        /// </summary>
        public bool CanAcceptInput()
        {
            return _stateMachine.CanAcceptInput();
        }

        /// <summary>
        /// Mevcut state'i döndürür.
        /// </summary>
        public GameState GetCurrentState()
        {
            return _stateMachine.CurrentState;
        }
    }
}

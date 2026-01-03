using System;

namespace Storia.Core.GameFlow
{
    /// <summary>
    /// Oyun akışının state machine yönetimi.
    /// 
    /// Sorumluluklar:
    /// 1. Mevcut oyun durumunu takip etme
    /// 2. Durum geçişlerinin geçerliliğini doğrulama (transition validation)
    /// 3. Durum değişikliklerini olay aracılığıyla bildirme
    /// 4. Player input kabul edilebilirliğini kontrol etme
    /// 
    /// Temel Durumlar (GameState enum):
    /// - DayInitialization: Gün başlangıcı (initialization)
    /// - AwaitingShip: Sonraki gemi bekleniyor
    /// - ShipArrival: Gemi limana ulaştı (visual presentation)
    /// - ShipDecision: Oyuncu gemi kabul/red karar'ı veriyor
    /// - ContainerEvaluation: Oyuncu konteyner kabul/red kararı veriyor
    /// - PlacementSelection: Oyuncu konteyner için zone seçiyor
    /// - DayEnd: Gün sona erdi (terminal state)
    /// 
    /// Single Responsibility: Oyun durumu yönetimi ve geçiş doğrulaması.
    /// </summary>
    public sealed class GameFlowStateMachine
    {
        // ========== Private State ==========
        /// <summary>
        /// Şu anda oyunun içinde bulunduğu durum (state).
        /// DayInitialization ile başlar, DayEnd ile biter.
        /// TransitionTo() methodu aracılığıyla değiştirilir.
        /// </summary>
        private GameState _currentState;

        // ========== Events ==========
        /// <summary>
        /// State geçişi yapıldığında tetiklenir.
        /// Event handler'lar durum değişikliğini takip edebilir (logging, UI update, vs).
        /// Parametreler: (oldState, newState)
        /// </summary>
        public event Action<GameState, GameState> OnStateChanged;

        // ========== Public Properties ==========
        /// <summary>
        /// Mevcut oyun durumunu döner (read-only).
        /// Day01PrototypeController ve coordinator'lar tarafından sorgulanır.
        /// </summary>
        public GameState CurrentState => _currentState;

        // ========== Lifecycle ==========
        /// <summary>
        /// State machine'i başlat (yeni gün için).
        /// İlk durum: DayInitialization
        /// Day01PrototypeController.StartDay()'de çağrılır.
        /// </summary>
        public GameFlowStateMachine()
        {
            _currentState = GameState.DayInitialization;
        }

        /// <summary>
        /// Yeni state'e geçiş yap (validation ile).
        /// 
        /// Akış:
        /// 1. Geçiş geçerliliğini kontrol et (IsValidTransition)
        /// 2. Geçersizse uyarı ver, dön
        /// 3. Eski state'i kaydet, yeni state'e geç
        /// 4. Debug log yaz (durum değişikliğini takip için)
        /// 5. OnStateChanged event'ini tetikle (listener'lar bilgilendirilir)
        /// </summary>
        /// <param name="newState">Gidilecek yeni state</param>
        public void TransitionTo(GameState newState)
        {
            if (!IsValidTransition(_currentState, newState))
            {
                UnityEngine.Debug.LogWarning($"[GameFlowStateMachine] Geçersiz durum geçişi: {_currentState} → {newState}");
                return;
            }

            GameState oldState = _currentState;
            _currentState = newState;

            UnityEngine.Debug.Log($"[GameFlowStateMachine] Durum geçişi: {oldState} → {newState}");
            OnStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// State geçişinin geçerli olup olmadığını kontrol eder.
        /// 
        /// Validation kuralları:
        /// 1. Aynı state'ten aynı state'e geçiş yasak (idempotency)
        /// 2. DayEnd'den (terminal state) başka state'e geçiş yasak
        /// 3. Transition matrix'inde tanımlanmış geçişlere izin ver
        /// 4. Diğer tüm geçişler yasak
        /// 
        /// Transition matrix (from → to):
        /// - DayInitialization: AwaitingShip, DayEnd
        /// - AwaitingShip: ShipArrival, DayEnd
        /// - ShipArrival: ShipDecision, DayEnd
        /// - ShipDecision: ContainerEvaluation, AwaitingShip, DayEnd
        /// - ContainerEvaluation: PlacementSelection, ContainerEvaluation, AwaitingShip, DayEnd
        /// - PlacementSelection: ContainerEvaluation, AwaitingShip, DayEnd
        /// </summary>
        /// <param name="from">Mevcut state</param>
        /// <param name="to">Hedef state</param>
        /// <returns>Geçiş geçerli mi? (true = izin, false = yasak)</returns>
        private bool IsValidTransition(GameState from, GameState to)
        {
            // Aynı state'e geçiş izin verilmiyor (idempotency kontrolü)
            if (from == to) return false;

            // DayEnd'den başka bir state'e geçiş yok
            if (from == GameState.DayEnd) return false;

            // Transition matrix
            switch (from)
            {
                case GameState.DayInitialization:
                    return to == GameState.AwaitingShip || to == GameState.DayEnd;

                case GameState.AwaitingShip:
                    return to == GameState.ShipArrival || to == GameState.DayEnd;

                case GameState.ShipArrival:
                    return to == GameState.ShipDecision || to == GameState.DayEnd;

                case GameState.ShipDecision:
                    return to == GameState.ContainerEvaluation || to == GameState.AwaitingShip || to == GameState.DayEnd;

                case GameState.ContainerEvaluation:
                    return to == GameState.PlacementSelection || to == GameState.ContainerEvaluation || 
                           to == GameState.AwaitingShip || to == GameState.DayEnd;

                case GameState.PlacementSelection:
                    return to == GameState.ContainerEvaluation || to == GameState.AwaitingShip || to == GameState.DayEnd;

                default:
                    return false;
            }
        }

        // ========== Query Methods ==========
        /// <summary>
        /// Belirli bir state'te olup olmadığını kontrol eder.
        /// 
        /// Kullanım: Controller'lar state-specific davranışlar için sorgulanır.
        /// Örnek: if (_stateMachine.IsInState(GameState.ContainerEvaluation)) { ...kabul/ret işleme... }
        /// </summary>
        /// <param name="state">Kontrol edilecek state</param>
        /// <returns>Şu anda bu state'te mi? (true = evet, false = hayır)</returns>
        public bool IsInState(GameState state)
        {
            return _currentState == state;
        }

        /// <summary>
        /// Player input'unun kabul edilip edilemeyeceğini kontrol eder.
        /// 
        /// Input kabul edilen state'ler:
        /// - ShipDecision: Gemi kabul/ret kararı
        /// - ContainerEvaluation: Konteyner kabul/ret kararı
        /// - PlacementSelection: Zone seçimi
        /// 
        /// Diğer state'lerdeki input'lar görmezden gelinir (shutdown gating).
        /// Day01PrototypeController button handler'larında kontrol edilir.
        /// </summary>
        /// <returns>Input bu state'te kabul edilir mi?</returns>
        public bool CanAcceptInput()
        {
            return _currentState == GameState.ShipDecision ||
                   _currentState == GameState.ContainerEvaluation ||
                   _currentState == GameState.PlacementSelection;
        }

        /// <summary>
        /// Oyun günü bitmiş mi?
        /// 
        /// True ise:
        /// - Tüm gemi ve konteyner işleme tamamlandı
        /// - UI sonuç ekranını gösterebilir
        /// - Yeni gün başlamaya hazır
        /// </summary>
        /// <returns>Gün bitti mi? (state == DayEnd)</returns>
        public bool IsDayEnded()
        {
            return _currentState == GameState.DayEnd;
        }

        // ========== Lifecycle Methods ==========
        /// <summary>
        /// State machine'i sıfırla (yeni gün başlangıcı için).
        /// 
        /// İlk state'e döner: DayInitialization
        /// Day01PrototypeController.StartDay() sırasında çağrılır.
        /// </summary>
        public void Reset()
        {
            _currentState = GameState.DayInitialization;
        }
    }
}

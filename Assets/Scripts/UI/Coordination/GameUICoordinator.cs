using Storia.Core.Initialization;

namespace Storia.UI.Coordination
{
    /// <summary>
    /// Oyunun tüm UI koordinasyonundan sorumlu ana orchestrator.
    /// 
    /// Sorumluluklar:
    /// - HUD ve Workstation controller'larını yönetmek
    /// - UI initialization
    /// - High-level UI state coordination
    /// 
    /// Single Responsibility: UI orchestration.
    /// </summary>
    public sealed class GameUICoordinator
    {
        private HUDUIController _hudController;
        private WorkstationUIController _workstationController;
        private DayTimer _timer;

        /// <summary>
        /// UI sistemini başlatır ve controller'ları oluşturur.
        /// </summary>
        public void Initialize(SceneReferencesProvider sceneRefs)
        {
            if (sceneRefs == null)
            {
                UnityEngine.Debug.LogError("[GameUICoordinator] SceneReferencesProvider referansı eksik!");
                return;
            }

            // Timer referansını sakla
            _timer = sceneRefs.Timer;

            // HUD controller
            _hudController = new HUDUIController(sceneRefs.HUD);

            // Workstation controller
            _workstationController = new WorkstationUIController(sceneRefs.Workstation);

            // Dev overlay timer referansını ata
            sceneRefs.DevOverlayUI?.SetTimer(_timer);
        }

        /// <summary>
        /// Timer güncelleme - her frame çağrılır.
        /// </summary>
        public void UpdateTimer(float deltaTime)
        {
            if (_timer == null) return;

            _timer.Tick(deltaTime);
            _hudController?.UpdateTimer(_timer.GetGameClockText());
        }

        /// <summary>
        /// Task ve placement talimatlarını HUD'da gösterir.
        /// </summary>
        public void DisplayTaskInstructions(string taskDescription, string placementInstruction)
        {
            string combinedText = $"{taskDescription}\n\n{placementInstruction}";
            _hudController?.UpdateTaskHint(combinedText);
        }

        /// <summary>
        /// End-of-day sonuçlarını gösterir.
        /// </summary>
        public void ShowEndOfDayResults(PrototypeStats stats, string logText)
        {
            _hudController?.ShowEndOfDayResults(stats, logText);
        }

        // Workstation UI access
        public WorkstationUIController Workstation => _workstationController;

        // HUD UI access
        public HUDUIController HUD => _hudController;
    }
}

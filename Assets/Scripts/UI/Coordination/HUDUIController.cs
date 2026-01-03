namespace Storia.UI.Coordination
{
    /// <summary>
    /// HUD (Screen-overlay) UI güncellemelerinden sorumlu controller.
    /// 
    /// Sorumluluklar:
    /// - Timer text güncelleme
    /// - Task hint gösterimi
    /// - End-of-day summary gösterimi
    /// 
    /// Single Responsibility: HUD UI state management.
    /// </summary>
    public sealed class HUDUIController
    {
        private readonly IHUDView _hudView;

        public HUDUIController(IHUDView hudView)
        {
            _hudView = hudView;
        }

        /// <summary>
        /// Timer text'ini günceller (game clock).
        /// </summary>
        public void UpdateTimer(string timeText)
        {
            _hudView?.SetTimeText(timeText);
        }

        /// <summary>
        /// Task hint text'ini günceller (görev talimatları).
        /// </summary>
        public void UpdateTaskHint(string taskText)
        {
            _hudView?.SetTaskHint(taskText);
        }

        /// <summary>
        /// End-of-day summary ve log'u gösterir.
        /// </summary>
        public void ShowEndOfDayResults(PrototypeStats stats, string logText)
        {
            _hudView?.SetEndSummary(stats);
            _hudView?.SetEndLog(logText);
        }
    }
}

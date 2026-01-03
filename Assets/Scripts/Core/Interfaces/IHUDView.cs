namespace Storia.UI
{
    /// <summary>
    /// HUD (Heads-Up Display) interface - her zaman ekranda görünen UI elementleri.
    /// Screen-Space Overlay Canvas'ta render edilir.
    /// </summary>
    public interface IHUDView
    {
        /// <summary>
        /// Zaman göstergesini güncelle (sadece oyun saati).
        /// </summary>
        void SetTimeText(string gameClockText);

        /// <summary>
        /// Görev talimatlarını göster (sürekli ekranda).
        /// </summary>
        void SetTaskHint(string text);

        /// <summary>
        /// Gün sonu ekranını göster/gizle.
        /// </summary>
        void ShowEndScreen(bool show);

        /// <summary>
        /// Gün sonu özet istatistiklerini göster.
        /// </summary>
        void SetEndSummary(PrototypeStats stats);

        /// <summary>
        /// Gün sonu hata log'unu göster.
        /// </summary>
        void SetEndLog(string text);
    }
}

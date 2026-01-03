using TMPro;
using UnityEngine;
using Storia.Constants;
using Storia.Player;
using Storia.Interaction;

namespace Storia.UI
{
    /// <summary>
    /// HUD (Heads-Up Display) - her zaman ekranda görünen UI elementleri.
    /// Screen-Space Overlay Canvas kullanır.
    /// Gün sonu ekranını otomatik olarak timer event'i ile tetikler.
    /// </summary>
    public sealed class HUDView : MonoBehaviour, IHUDView
    {
        [Header("Zaman Gösterimi")]
        [Tooltip("Oyun içi saat metin alanı")]
        [SerializeField] private TMP_Text _txtTime;

        [Header("Görev Talimatları")]
        [Tooltip("Görev talimatları metin alanı")]
        [SerializeField] private TMP_Text _txtTaskHint;

        [Header("Gün Sonu")]
        [Tooltip("Gün sonu paneli root GameObject'i")]
        [SerializeField] private GameObject _endPanelRoot;
        [Tooltip("Gün sonu özet metin alanı")]
        [SerializeField] private TMP_Text _txtEndSummary;
        [Tooltip("Gün sonu log metin alanı")]
        [SerializeField] private TMP_Text _txtEndLog;

        [Header("Bağımlılıklar")]
        [Tooltip("Gün sonu zamanlayıcı bileşeni")]
        [SerializeField] private DayTimer _dayTimer;
        [Tooltip("Oyuncu kontrol bileşeni")]
        [SerializeField] private PlayerController _playerController;
        [Tooltip("Etkileşim noktaları (gün sonu ekranında kapatılacaklar)")]
        [SerializeField] private InteractionPoint[] _interactionPoints;

        private bool _endScreenShown = false;

        private void OnEnable()
        {
            if (_dayTimer != null)
                _dayTimer.OnDayTimeUp += HandleDayTimeUp;
            else
                Debug.LogWarning("[HUDView] OnEnable çağrıldı ancak _dayTimer referansı bulunamadı, olay aboneliği atlanıyor.");
        }

        private void OnDisable()
        {
            if (_dayTimer != null)
                _dayTimer.OnDayTimeUp -= HandleDayTimeUp;
        }

        private void Awake()
        {
            // Başlangıçta end panel kapalı
            if (_endPanelRoot != null)
                _endPanelRoot.SetActive(false);
        }

        /// <summary>
        /// Timer zaman dolduğunda otomatik olarak gün sonu ekranını göster.
        /// </summary>
        private void HandleDayTimeUp()
        {
            if (_endScreenShown) return;
            _endScreenShown = true;

            // End panel'i göster
            if (_endPanelRoot != null)
                _endPanelRoot.SetActive(true);

            // Player kontrollerini kilitle
            if (_playerController != null)
            {
                _playerController.SetFullLock(true);
            }

            // Cursor'u serbest bırak
            Storia.Camera.CameraLockManager.Instance.UnlockCursor();

            // Tüm aktif etkileşimleri kapat
            if (_interactionPoints != null)
            {
                foreach (var point in _interactionPoints)
                {
                    if (point != null)
                        point.ForceCloseInteraction();
                }
            }
        }

        public void SetTimeText(string gameClockText)
        {
            if (_txtTime == null) return;

            // Sadece oyun içi saati göster
            _txtTime.text = string.Format(UIConstants.TimeDisplayFormat, gameClockText);
        }

        public void SetTaskHint(string text)
        {
            if (_txtTaskHint != null)
                _txtTaskHint.text = text;
        }

        /// <summary>
        /// Gün sonu ekranını göster/gizle.
        /// </summary>
        public void ShowEndScreen(bool show)
        {
            // Alt düzey sistemler end panel'i kapatamasın
            if (!show) return;
            
            // Manuel açma sadece ilk kez izin ver
            if (_endScreenShown) return;
            
            HandleDayTimeUp();
        }

        /// <summary>
        /// Gün sonu özet istatistiklerini göster.
        /// </summary>
        public void SetEndSummary(PrototypeStats stats)
        {
            if (_txtEndSummary == null) return;

            _txtEndSummary.text =
                $"{UIConstants.DayEndedTitle}\n\n" +
                $"{string.Format(UIConstants.TotalDecisionsFormat, stats.total)}\n" +
                $"{string.Format(UIConstants.CorrectDecisionsFormat, stats.correct)}\n" +
                $"{string.Format(UIConstants.WrongDecisionsFormat, stats.wrong)}\n\n" +
                $"{string.Format(UIConstants.CorrectPlacementFormat, stats.placementCorrect)}\n" +
                $"{string.Format(UIConstants.WrongPlacementFormat, stats.placementWrong)}";

#if UNITY_EDITOR
            // Seed bilgisi sadece geliştirici testleri için
            _txtEndSummary.text += string.Format(UIConstants.DevSeedFormat, stats.seed);
#endif
        }

        /// <summary>
        /// Gün sonu hata log'unu göster.
        /// </summary>
        public void SetEndLog(string text)
        {
            if (_txtEndLog != null)
                _txtEndLog.text = text;
        }
    }
}

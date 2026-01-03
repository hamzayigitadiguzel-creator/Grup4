using TMPro;
using UnityEngine;
using Storia.Constants;

namespace Storia.UI
{
    /// <summary>
    /// Geliştirici modu overlay UI.
    /// Sadece dev mode aktifken görünür, oyuncuya asla gösterilmez.
    /// </summary>
    public sealed class DevOverlayUI : MonoBehaviour
    {
        [Header("Geliştirici Bilgisi Ekranı")]
        [Tooltip("Geliştirici bilgisi metin alanı")]
        [SerializeField] private TMP_Text _txtDevInfo;
        [Tooltip("Geliştirici paneli root GameObject'i")]
        [SerializeField] private GameObject _panelRoot;

        private Storia.Diagnostics.DevModeManager _devMode;
        private DayTimer _timer;

        private void Awake()
        {
            _devMode = Storia.Diagnostics.DevModeManager.Instance;
            // Başlangıç durumu Awake'de set edilmiyor, Start'da DevMode toggle'ı bağlanıyor
        }

        private void Start()
        {
            if (_devMode != null)
            {
                _devMode.OnDevModeToggled += UpdateVisibility;
                UpdateVisibility();
            }
            else
            {
                Debug.LogWarning("[DevOverlayUI] Başlatma çağrıldı ancak _devMode referansı bulunamadı, olay aboneliği atlanıyor.");
            }
        }

        private void OnDestroy()
        {
            if (_devMode != null)
                _devMode.OnDevModeToggled -= UpdateVisibility;
        }

        /// <summary>
        /// Timer referansını dışarıdan ata.
        /// </summary>
        public void SetTimer(DayTimer timer)
        {
            _timer = timer;
        }

        private void Update()
        {
            if (_devMode == null || !_devMode.IsEnabled || _timer == null)
                return;

            UpdateDevInfo();
        }

        private void UpdateVisibility()
        {
            if (_panelRoot == null) return;

            bool shouldShow = _devMode != null && _devMode.IsEnabled;
            _panelRoot.SetActive(shouldShow);
        }

        private void UpdateDevInfo()
        {
            if (_txtDevInfo == null) return;

            string info = UIConstants.DevModeHeader + "\n";
            info += string.Format(UIConstants.DevGameTimeFormat, _timer.GetGameClockText()) + "\n";
            info += string.Format(UIConstants.DevRealTimeFormat, _timer.GetRealTimeElapsedText()) + "\n";
            info += string.Format(UIConstants.DevElapsedFormat, _timer.ElapsedSeconds, _timer.RemainingSeconds);

            _txtDevInfo.text = info;
        }
    }
}

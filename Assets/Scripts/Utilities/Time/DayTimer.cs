using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

/// <summary>
/// Gün zamanlama sistemi.
/// 
/// Sorumlulukları:
/// 1. Oyun içi zamanı gerçek zamana kitleme (12 dakika = 6 saat oyun)
/// 2. Gün süresi tamamlandığında OnDayTimeUp olayını tetikleme
/// 3. Geliştirici modu: zaman ölçeği (time scale) ve manuel atla kontrolleri
/// 4. Saati saat:dakika formatında gösterme
/// 
/// Kullanım:
/// - Day01PrototypeController Update()'de Timer.Tick(Time.deltaTime) çağrılır
/// - OnDayTimeUp event'ine subscribe edilerek gün bitişi yönetilir
/// - Geliştirici modu: Inspector'dan zaman manipülasyonu yapılabilir
/// </summary>
public sealed class DayTimer : MonoBehaviour
{
    // ========== Konfigürasyon ==========
#if ODIN_INSPECTOR
    [FoldoutGroup("Gün Ayarları", expanded: true)]
#endif
    [Header("Gün Ayarları")]
    [Tooltip("Gerçek zamanda kaç saniye = 1 oyun günü")]
    [SerializeField] private float _realDayDurationSeconds = 12f * 60f; // 12 dakika

#if ODIN_INSPECTOR
    [FoldoutGroup("Gün Ayarları")]
#endif
    [Tooltip("Oyun saat başlangıcı (24 saat formatı)")]
    [SerializeField] private int _gameStartHour = 0;  // 00:00

#if ODIN_INSPECTOR
    [FoldoutGroup("Gün Ayarları")]
#endif
    [Tooltip("Oyun saat bitişi (24 saat formatı)")]
    [SerializeField] private int _gameEndHour = 6;    // 06:00

    // ========== Çalışma Zamanı Durumu ==========
    /// <summary>
    /// Geliştirici zaman ölçeği (time scale).
    /// 1.0 = normal hız
    /// 0.5 = yarı hız
    /// 2.0 = çift hız
    /// </summary>
    private float _devTimeScale = 1f;

#if ODIN_INSPECTOR
    // ========== Geliştirici Kontrolleri ==========
    /// <summary>
    /// Geliştirici zaman ölçeği property'si (Inspector'da gösterilir).
    /// Oyun mantığını bozmadan zamanı hızlandırabilir/yavaşlatabilir.
    /// </summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri", expanded: true)]
    [InfoBox("Bu kontroller sadece geliştirici modu için. Oyun mantığını bozmadan zaman manipülasyonu sağlar.")]
    [Range(0.1f, 10f)]
    [ShowInInspector]
    [Tooltip("Geliştirici zaman ölçeği (time scale). 1.0 = normal hız, 0.5 = yarı hız, 2.0 = çift hız")]
    private float DeveloperTimeScale
    {
        get => _devTimeScale;
        set => _devTimeScale = value;
    }

    /// <summary>Zamanı 30 saniye ileri al</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [Button("İleri Al (+30 sn)")]
    private void DevSkipForward30()
    {
        if (Application.isPlaying)
            ElapsedSeconds = Mathf.Min(ElapsedSeconds + 30f, _realDayDurationSeconds);
    }

    /// <summary>Zamanı 30 saniye geri al</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [Button("Geri Al (-30 sn)")]
    private void DevSkipBackward30()
    {
        if (Application.isPlaying)
            ElapsedSeconds = Mathf.Max(ElapsedSeconds - 30f, 0f);
    }

    /// <summary>Zamanı günün başına döndür</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [Button("Günün Başına Dön")]
    private void DevResetToStart()
    {
        if (Application.isPlaying)
            ElapsedSeconds = 0f;
    }

    /// <summary>Zamanı günün sonuna atla</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [Button("Günün Sonuna Atla")]
    private void DevSkipToEnd()
    {
        if (Application.isPlaying)
            ElapsedSeconds = _realDayDurationSeconds - 1f;
    }

    /// <summary>Geçen gerçek zamanı dakika:saniye formatında göster</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [ShowInInspector, ReadOnly]
    [Tooltip("Geçen gerçek zamanı dakika:saniye formatında göster")]
    private string ElapsedRealTime => $"{Mathf.FloorToInt(ElapsedSeconds / 60f)}:{Mathf.FloorToInt(ElapsedSeconds % 60f):00}";

    /// <summary>Kalan zamanı dakika:saniye formatında göster</summary>
    [FoldoutGroup("Geliştirici Zaman Kontrolleri")]
    [ShowInInspector, ReadOnly]
    [Tooltip("Kalan zamanı dakika:saniye formatında göster")]
    private string RemainingTimeDisplay => $"{Mathf.FloorToInt(RemainingSeconds / 60f)}:{Mathf.FloorToInt(RemainingSeconds % 60f):00}";
#endif

    // ========== Public Properties ==========
    /// <summary>
    /// Gün başından beri geçen saniye sayısı.
    /// Tick() metoduyla güncellenir.
    /// </summary>
    public float ElapsedSeconds { get; private set; }

    /// <summary>
    /// Gün bitişine kadar kalan saniye sayısı.
    /// En az 0 döner (gün bittiğinde).
    /// </summary>
    public float RemainingSeconds => Mathf.Max(0f, _realDayDurationSeconds - ElapsedSeconds);

    /// <summary>
    /// Gün bitti mi? (ElapsedSeconds >= _realDayDurationSeconds)
    /// </summary>
    public bool IsFinished => ElapsedSeconds >= _realDayDurationSeconds;

    // ========== Events ==========
    /// <summary>
    /// Gün süresi dolduğunda tetiklenir.
    /// Day01PrototypeController.HandleDayTimeUp()'i çağırır.
    /// </summary>
    public event System.Action OnDayTimeUp;

    // ========== Public Methods ==========
    /// <summary>
    /// Gün sayacını sıfırla (yeni gün başlatılıyor).
    /// Day01PrototypeController.StartDay()'den çağrılır.
    /// </summary>
    public void ResetDay()
    {
        ElapsedSeconds = 0f;
    }

    /// <summary>
    /// Zamanı bir frame ileri al.
    /// Geliştirici zaman ölçeği (_devTimeScale) uygulanır.
    /// Day01PrototypeController.Update()'de her frame çağrılır.
    /// </summary>
    /// <param name="dt">Geçen gerçek zamandaki saniye (Time.deltaTime)</param>
    public void Tick(float dt)
    {
        // Gün zaten biterse artık saat ilerleme
        if (IsFinished)
            return;

        // Zaman ilerle (developer time scale uygulanır)
        ElapsedSeconds += dt * _devTimeScale;

        // Gün tamamlandı mı?
        if (ElapsedSeconds >= _realDayDurationSeconds)
        {
            OnDayTimeUp?.Invoke();
        }
    }

    /// <summary>
    /// Gerçek zamanda geçen süreyi dakika:saniye formatında döndürür.
    /// HUD'da gösterim için kullanılır (sadece dev debug için değil, UI'ın da kullanabileceği).
    /// </summary>
    /// <returns>Format: "M:SS" (örn: "5:30", "0:45")</returns>
    public string GetRealTimeElapsedText()
    {
        int minutes = Mathf.FloorToInt(ElapsedSeconds / 60f);
        int seconds = Mathf.FloorToInt(ElapsedSeconds % 60f);
        return $"{minutes}:{seconds:00}";
    }

    /// <summary>
    /// Oyun saatini saat:dakika formatında döndürür.
    /// ElapsedSeconds'ü (0 ila _realDayDurationSeconds aralığı)
    /// oyun saatine (_gameStartHour ila _gameEndHour) dönüştürür (lineer interpolasyon).
    /// </summary>
    /// <returns>Format: "HH:MM" (örn: "00:00", "03:45", "06:00")</returns>
    public string GetGameClockText()
    {
        // Gün boyunca kaç yüzde ilerledi? (0.0 = başlangıç, 1.0 = bitiş)
        float t = Mathf.Clamp01(ElapsedSeconds / _realDayDurationSeconds);

        // Oyun saatini hesapla (başlangıç saatinden bitiş saatine doğru lineer)
        float gameHours = Mathf.Lerp(_gameStartHour, _gameEndHour, t);
        int hour = Mathf.FloorToInt(gameHours);

        // Kesirli kısımdan dakika hesapla
        float fractional = gameHours - hour;
        int minute = Mathf.FloorToInt(fractional * 60f);

        return $"{hour:00}:{minute:00}";
    }
}

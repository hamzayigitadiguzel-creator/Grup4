using UnityEngine;
using System.Collections.Generic;
using Storia.Data.Generated;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Storia.Diagnostics
{
    /// <summary>
    /// Geliştirici modu yöneticisi.
    /// 
    /// Sorumlulukları:
    /// 1. Dev mode toggle etme (Inspector üzerinden)
    /// 2. Seed, konteyner, gemi debug bilgilerini kaydetme
    /// 3. Dev Log Window ile debug verileri gösterme
    /// 
    /// Production build'lerde deaktif (düşük performance impact).
    /// Inspector'da görüntülenebilir, oyuncuya görünmez.
    /// </summary>
    public sealed class DevModeManager : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [BoxGroup("Developer Mode")]
        [InfoBox("Bu mod YALNIZCA geliştiriciler içindir. Production build'lerde görünmez.")]
        [ToggleLeft]
#endif
        [SerializeField] private bool _devModeEnabled = false;

        /// <summary>Dev mode aktif mi? (Editor'de ve toggle aynı anda açık mı?)</summary>
        public bool IsEnabled => _devModeEnabled && Application.isEditor;

        // ========== Singleton ==========
        private static DevModeManager _instance;
        public static DevModeManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<DevModeManager>();
                }
                return _instance;
            }
        }

        // ========== Events ==========
        /// <summary>Dev mode toggle yapıldığında tetiklenir</summary>
        public event System.Action OnDevModeToggled;

        // ========== Private Fields ==========
        /// <summary>OnValidate spam'ini engellemek için son toggle state'i</summary>
        [SerializeField, HideInInspector] private bool _lastDevModeEnabled;

        /// <summary>Çalışma zamanında ayarlanan seed değeri (gün başında)</summary>
        private int _currentSeed;
        
        /// <summary>Tüm günün konteyner debug log'u</summary>
        private readonly List<ContainerDebugInfo> _containerDebugLog = new List<ContainerDebugInfo>();
        
        /// <summary>Tüm günün gemi debug log'u</summary>
        private readonly List<ShipDebugInfo> _shipDebugLog = new List<ShipDebugInfo>();
        
        /// <summary>Şu anda limanda olan gemi'nin index'i (-1 = gemi yok)</summary>
        private int _currentShipIndex = -1;

        // ========== Lifecycle ==========
        /// <summary>
        /// Singleton başlatma.
        /// Duplicate manager'ı sil, state'i kaydet.
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // İlk state'i yakala (OnValidate tetiklenirse doğru karşılaştırma yapsın)
            _lastDevModeEnabled = _devModeEnabled;
        }

        /// <summary>
        /// Inspector'da _devModeEnabled değiştiğinde, OnDevModeToggled event'ini tetikle.
        /// Spam'ı engellemek için state karşılaştırması yap.
        /// </summary>
        private void OnValidate()
        {
            // Inspector'da değer değişmediyse event spam'leme
            if (_lastDevModeEnabled == _devModeEnabled) 
                return;

            _lastDevModeEnabled = _devModeEnabled;
            OnDevModeToggled?.Invoke();
        }

        // ========== Data Logging ==========
        /// <summary>
        /// Yeni gün başladığında seed, konteyner log'u ve gemi log'u sıfırla.
        /// Day01PrototypeController'dan çağrılır.
        /// </summary>
        /// <param name="seed">Gün'ün seed değeri (reproducibility için)</param>
        public void OnDayStarted(int seed)
        {
            _currentSeed = seed;
            _containerDebugLog.Clear();
            _shipDebugLog.Clear();
            _currentShipIndex = -1;
        }

        /// <summary>
        /// Yeni gemi limana geldiğinde gemi bilgilerini debug log'a kaydet.
        /// ShipLifecycleCoordinator'dan çağrılır.
        /// </summary>
        /// <param name="shipName">Gemi adı (örn: "Mavi Balık")</param>
        /// <param name="shipId">Gemi ID'si (örn: "SHIP-001")</param>
        /// <param name="originPort">Çıkış port'u (örn: "Pire")</param>
        /// <param name="containerCount">Taşıdığı konteyner sayısı</param>
        /// <param name="voyageHours">Yolculuk süresi (saatlerde)</param>
        public void LogShip(string shipName, string shipId, string originPort, int containerCount, float voyageHours)
        {
            // Dev mode kapalıysa loglama yapma
            if (!IsEnabled) 
                return;

            // Yeni gemi, index'i artır
            _currentShipIndex++;
            
            // Gemi bilgilerini log'a ekle (containers listesi boş başla)
            _shipDebugLog.Add(new ShipDebugInfo
            {
                shipIndex = _currentShipIndex,
                shipName = shipName,
                shipId = shipId,
                originPort = originPort,
                containerCount = containerCount,
                voyageHours = voyageHours,
                containers = new List<ContainerDebugInfo>()
            });
        }

        /// <summary>
        /// Konteyner spawn edildiğinde (gemiden cargo'ya çıkarıldığında) debug bilgisini kaydet.
        /// ShipContainerSpawner'dan çağrılır.
        /// </summary>
        /// <param name="index">Konteyner'in global index'i (0, 1, 2, ...)</param>
        /// <param name="containerData">Konteyner'in gerçek verisi (truth)</param>
        /// <param name="presentation">Konteyner'in manifest + label gösterim verisi</param>
        public void LogContainer(int index, ContainerData containerData, ContainerPresentation presentation)
        {
            // Dev mode kapalıysa loglama yapma
            if (!IsEnabled) 
                return;

            // Konteyner debug bilgisini oluştur
            var containerInfo = new ContainerDebugInfo
            {
                index = index,                              // Global sıra (0, 1, 2, ...)
                containerId = containerData.truth.containerId,  // Gerçek ID
                trueOrigin = containerData.truth.originPort,    // Gerçek port
                trueCargo = containerData.truth.cargoLabel,     // Gerçek cargo
                manifestShown = presentation.manifestShown,     // Manifest'te gösterilen (hatalı olabilir)
                labelShown = presentation.labelShown,           // Label'da gösterilen (hatalı olabilir)
                conflictType = presentation.conflict,           // Manifest vs Label çelişkisi
                shipIndex = _currentShipIndex                    // Hangi gemi'ye ait?
            };

            // Global log'a ekle
            _containerDebugLog.Add(containerInfo);

            // Eğer aktif bir gemi varsa, konteyneri o geminin listesine de ekle
            if (_currentShipIndex >= 0 && _currentShipIndex < _shipDebugLog.Count)
            {
                _shipDebugLog[_currentShipIndex].containers.Add(containerInfo);
            }
        }

        // ========== Public Accessors (Read-only, Runtime davranışını değiştirmez) ==========
        /// <summary>Mevcut gün'ün seed değeri</summary>
        public int CurrentSeedValue => _currentSeed;
        
        /// <summary>Tüm gemilerin debug log'u (read-only)</summary>
        public System.Collections.Generic.IReadOnlyList<ShipDebugInfo> ShipLogReadonly => _shipDebugLog;
        
        /// <summary>Toplam gemi sayısı</summary>
        public int TotalShipsCount => _shipDebugLog.Count;
        
        /// <summary>Toplam konteyner sayısı</summary>
        public int TotalContainersCount => _containerDebugLog.Count;

        // ========== Dev Log Window ==========
        /// <summary>
        /// Dev Log Window'u aç (Odin veya Context Menu üzerinden).
        /// Inspector'da "Open Dev Log Window" buton'u veya sağ-tık menüsü ile çağrılır.
        /// </summary>
#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Geliştirici Günlüğü Penceresini Aç")]
#endif
#if UNITY_EDITOR
        [UnityEngine.ContextMenu("Geliştirici Günlüğü Penceresini Aç")]
#endif
        public void OpenDevLogWindow()
        {
#if UNITY_EDITOR
            // Window/Dev Log Window menu item'ını çalıştır
            UnityEditor.EditorApplication.ExecuteMenuItem("Window/Storia/Geliştirici Günlüğü");
#endif
        }

        // ========== Debug Info Structs ==========
        /// <summary>
        /// Gemi'nin debug bilgileri.
        /// Inspector'da tablo halinde gösterilir (Odin TableList ile).
        /// </summary>
        [System.Serializable]
        public struct ShipDebugInfo
        {
#if ODIN_INSPECTOR
            [BoxGroup("Gemi Bilgileri")]
            [HorizontalGroup("Gemi Bilgileri/Row1")]
            [LabelWidth(80)]
#endif
            /// <summary>Gemi'nin sırası (0, 1, 2, ...)</summary>
            public int shipIndex;

#if ODIN_INSPECTOR
            [HorizontalGroup("Gemi Bilgileri/Row1")]
            [LabelWidth(80)]
#endif
            /// <summary>Gemi adı (örn: "Mavi Balık")</summary>
            public string shipName;

#if ODIN_INSPECTOR
            [HorizontalGroup("Gemi Bilgileri/Row1")]
            [LabelWidth(80)]
#endif
            /// <summary>Gemi ID'si (örn: "SHIP-001")</summary>
            public string shipId;

#if ODIN_INSPECTOR
            [HorizontalGroup("Gemi Bilgileri/Row2")]
            [LabelWidth(80)]
#endif
            /// <summary>Çıkış port'u (örn: "Pire")</summary>
            public string originPort;

#if ODIN_INSPECTOR
            [HorizontalGroup("Gemi Bilgileri/Row2")]
            [LabelWidth(120)]
#endif
            /// <summary>Taşıdığı konteyner sayısı</summary>
            public int containerCount;

#if ODIN_INSPECTOR
            [HorizontalGroup("Gemi Bilgileri/Row2")]
            [LabelWidth(120)]
#endif
            /// <summary>Yolculuk süresi (saatlerde)</summary>
            public float voyageHours;

#if ODIN_INSPECTOR
            [BoxGroup("Konteynerler")]
            [TableList(ShowIndexLabels = true, AlwaysExpanded = true, IsReadOnly = true, MinScrollViewHeight = 1000, MaxScrollViewHeight = 0)]
#endif
            /// <summary>Bu gemi'ye ait konteynerler'in listesi</summary>
            public List<ContainerDebugInfo> containers;
        }

        /// <summary>
        /// Konteyner'in debug bilgileri.
        /// ShipDebugInfo'daki TableList'te gösterilir.
        /// </summary>
        [System.Serializable]
        public struct ContainerDebugInfo
        {
#if ODIN_INSPECTOR
            [TableColumnWidth(40)]
#endif
            /// <summary>Konteyner'in global sıra numarası</summary>
            public int index;

#if ODIN_INSPECTOR
            [TableColumnWidth(60)]
#endif
            /// <summary>Bu konteyner'in ait olduğu gemi'nin index'i</summary>
            public int shipIndex;

#if ODIN_INSPECTOR
            [TableColumnWidth(80)]
#endif
            /// <summary>Konteyner'in gerçek ID'si</summary>
            public string containerId;

#if ODIN_INSPECTOR
            [TableColumnWidth(100)]
#endif
            /// <summary>Konteyner'in gerçek (truth) origin port'u</summary>
            public string trueOrigin;

#if ODIN_INSPECTOR
            [TableColumnWidth(120)]
#endif
            /// <summary>Konteyner'in gerçek (truth) cargo type'ı</summary>
            public string trueCargo;

#if ODIN_INSPECTOR
            [TableColumnWidth(150)]
#endif
            /// <summary>Manifest'te gösterilen bilgiler (hatalı olabilir)</summary>
            public ContainerFields manifestShown;

#if ODIN_INSPECTOR
            [TableColumnWidth(150)]
#endif
            /// <summary>Label'da gösterilen bilgiler (hatalı olabilir)</summary>
            public ContainerFields labelShown;

#if ODIN_INSPECTOR
            [TableColumnWidth(120)]
#endif
            /// <summary>Manifest vs Label çelişkisi (None/IdMismatch/OriginMismatch/CargoMismatch)</summary>
            public PresentationConflict conflictType;
        }
    }
}

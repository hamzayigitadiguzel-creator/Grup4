using UnityEngine;
using System.Collections.Generic;
using Storia.Core.Data;
using Storia.Data.Generated;
using Storia.Core.Pooling;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi yaşam döngüsünü yönetir.
    /// Artık generated ShipData ile çalışır.
    /// - State geçişleri
    /// - Zaman dolma otomatik işlemleri
    /// - Object pooling ile gemi yönetimi
    /// </summary>
    public sealed class ShipManager : MonoBehaviour, IShipService
    {
        [SerializeField]
        [Tooltip("Gemi prefab'i (ShipMovement component'i içermeli)")]
        private GameObject _shipPrefab;

        [Header("Pooling Settings")]
        [SerializeField]
        [Tooltip("Başlangıç pool boyutu")]
        private int _initialPoolSize = 3;

        [SerializeField]
        [Tooltip("Maksimum pool boyutu (0 = sınırsız)")]
        private int _maxPoolSize = 5;

        private static ShipManager _instance;
        private static readonly object _lock = new object();

        // Object pool
        private ObjectPool<ShipMovement> _shipPool;

        /// <summary>
        /// Singleton instance. Lazy initialization kullanır.
        /// Sahneyi değiştirdiğinde yeni instance alınır.
        /// </summary>
        public static ShipManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = FindFirstObjectByType<ShipManager>();

                            if (_instance == null)
                            {
                                UnityEngine.Debug.LogWarning("[ShipManager] Sahnede ShipManager bulunamadı!");
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        // Gemi listesi (sıralanmış)
        private List<ShipInstance> _allShipsForDay = new List<ShipInstance>();

        // Şu anda işlemde olan gemi
        private ShipInstance _currentShip = null;

        // Fiziksel gemi GameObject'i
        private GameObject _currentShipObject = null;
        private ShipMovement _currentShipMovement = null;

        // ShipMovement olay delege referansları (uygun şekilde abonelikten çıkmak için)
        private System.Action _onArrivedDelegate = null;
        private System.Action _onDepartedDelegate = null;

        // Tüm gemilerle ilgili indeks
        private int _currentShipIndex = 0;

        // Events
        public event System.Action<ShipInstance> OnShipArrived;
        public event System.Action<ShipInstance, bool> OnShipDecisionMade;  // gemi, kabul edildi
        public event System.Action OnAllShipsProcessed;
        public event System.Action OnShipMovementCompleted;  // Gemi hareket tamamlandığında

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Pool'u initialize et
            InitializePool();
        }

        /// <summary>
        /// Ship object pool'unu oluştur.
        /// </summary>
        private void InitializePool()
        {
            if (_shipPrefab == null)
            {
                UnityEngine.Debug.LogError("[ShipManager] Ship prefab atanmamış, pool oluşturulamadı!");
                return;
            }

            _shipPool = new ObjectPool<ShipMovement>(
                _shipPrefab,
                _initialPoolSize,
                _maxPoolSize,
                allowGrowth: true,
                poolContainer: transform
            );

            UnityEngine.Debug.Log($"[ShipManager] Ship pool oluşturuldu: {_initialPoolSize} başlangıç, {_maxPoolSize} maksimum");
        }

        private void OnDestroy()
        {
            // Temizleme: mevcut gemi hareketinden aboneliği iptal et
            UnsubscribeFromCurrentShipMovement();

            // Fiziksel gemi varsa pool'a geri ver
            if (_currentShipMovement != null)
            {
                _shipPool?.Return(_currentShipMovement);
                _currentShipObject = null;
                _currentShipMovement = null;
            }

            // Pool'u temizle
            _shipPool?.Clear();

            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Yeni gün başlıyor, gemiler oluşturuluyor.
        /// Artık generated ShipData listesi alır.
        /// </summary>
        public void InitializeShipsForDay(List<ShipData> generatedShips)
        {
            _allShipsForDay.Clear();
            _currentShipIndex = 0;
            _currentShip = null;

            // Önceki gemi varsa pool'a geri ver
            if (_currentShipMovement != null)
            {
                _shipPool?.Return(_currentShipMovement);
                _currentShipObject = null;
                _currentShipMovement = null;
            }

            if (generatedShips == null || generatedShips.Count == 0)
            {
                OnAllShipsProcessed?.Invoke();
                return;
            }

            // ShipInstance'lar oluştur (ShipData'dan ShipDefinition'a dönüştür)
            foreach (var shipData in generatedShips)
            {
                ShipDefinition shipDef = ConvertToShipDefinition(shipData, out var containers);
                _allShipsForDay.Add(new ShipInstance(shipDef, containers));
            }
        }

        /// <summary>
        /// ShipData'yı ShipDefinition formatına çevir.
        /// Containers listesini ayrı bir out parametresi olarak döndürür (ShipInstance'a aktarılacak).
        /// </summary>
        private ShipDefinition ConvertToShipDefinition(ShipData shipData, out System.Collections.Generic.List<ContainerData> containers)
        {
            ShipDefinition shipDef = new ShipDefinition
            {
                shipId = shipData.shipId,
                shipName = shipData.shipName,
                originPort = shipData.originPort,
                containerCount = shipData.ContainerCount,
                voyageDurationHours = shipData.voyageDurationHours
            };

            // ContainerData listesini ayrı döndür
            containers = new System.Collections.Generic.List<ContainerData>(shipData.containers);

            return shipDef;
        }

        /// <summary>
        /// Sonraki gemiyi limana getir.
        /// İlk çağrıda ilk gemi, sonra sırasıyla diğerleri.
        /// </summary>
        public ShipInstance GetNextShip()
        {
            if (_currentShipIndex >= _allShipsForDay.Count)
            {
                OnAllShipsProcessed?.Invoke();
                return null;
            }

            _currentShip = _allShipsForDay[_currentShipIndex];
            _currentShip.State = ShipState.Approaching;
            _currentShipIndex++;

            // Fiziksel gemiyi spawn et
            SpawnPhysicalShip(_currentShip);

            OnShipArrived?.Invoke(_currentShip);
            return _currentShip;
        }

        /// <summary>
        /// Fiziksel gemi GameObject'ini pool'dan al ve ShipMovement'a link et.
        /// </summary>
        /// <param name="shipInstance">Spawn edilecek gemi instance'ı</param>
        private void SpawnPhysicalShip(ShipInstance shipInstance)
        {
            if (shipInstance == null)
            {
                UnityEngine.Debug.LogError("[ShipManager] ShipInstance null olamaz!");
                return;
            }

            if (_shipPool == null)
            {
                UnityEngine.Debug.LogError("[ShipManager] Ship pool oluşturulmamış!");
                return;
            }

            // Önceki gemi varsa pool'a geri ver
            if (_currentShipMovement != null)
            {
                UnsubscribeFromCurrentShipMovement();
                _shipPool.Return(_currentShipMovement);
            }

            // Pool'dan yeni gemi al
            _currentShipMovement = _shipPool.Get();

            if (_currentShipMovement == null)
            {
                UnityEngine.Debug.LogError("[ShipManager] Pool'dan gemi alınamadı!");
                return;
            }

            _currentShipObject = _currentShipMovement.gameObject;

            // ShipMovement'a ShipInstance'ı bağla
            _currentShipMovement.LinkToShip(shipInstance);

            // Event'lere subscribe ol (delegate referanslarını sakla)
            _onArrivedDelegate = () => HandleShipArrivedAtCrane(shipInstance);
            _onDepartedDelegate = () => HandleShipDeparted(shipInstance);

            _currentShipMovement.OnArrivedAtCrane += _onArrivedDelegate;
            _currentShipMovement.OnDeparted += _onDepartedDelegate;
        }

        /// <summary>
        /// Gemi vinç pozisyonuna ulaştığında.
        /// </summary>
        private void HandleShipArrivedAtCrane(ShipInstance ship)
        {
            // Gemi vinç konumunda, oyuncu artık karar verebilir
            UnityEngine.Debug.Log($"Gemi vinç konumuna ulaştı: {ship.Definition.shipName}");
        }

        /// <summary>
        /// Gemi tamamen ayrıldığında.
        /// </summary>
        private void HandleShipDeparted(ShipInstance ship)
        {
            ship.State = ShipState.Gone;

            // Fiziksel gemiyi pool'a geri vermeden önce unsubscribe
            UnsubscribeFromCurrentShipMovement();

            // Fiziksel gemiyi pool'a geri ver
            if (_currentShipMovement != null)
            {
                _shipPool?.Return(_currentShipMovement);
                _currentShipObject = null;
                _currentShipMovement = null;
            }

            UnityEngine.Debug.Log($"Gemi ayrıldı: {ship.Definition.shipName}");

            // Gemi hareket tamamlandı, sonraki gemiyi getirmeye hazır
            OnShipMovementCompleted?.Invoke();
        }

        /// <summary>
        /// Mevcut ShipMovement'tan event subscription'ları temizle (idempotent).
        /// </summary>
        private void UnsubscribeFromCurrentShipMovement()
        {
            if (_currentShipMovement == null)
                return;

            if (_onArrivedDelegate != null)
            {
                _currentShipMovement.OnArrivedAtCrane -= _onArrivedDelegate;
                _onArrivedDelegate = null;
            }

            if (_onDepartedDelegate != null)
            {
                _currentShipMovement.OnDeparted -= _onDepartedDelegate;
                _onDepartedDelegate = null;
            }
        }

        /// <summary>
        /// Oyuncu gemiyle ilişkili karar verdi (kabul/red).
        /// </summary>
        /// <param name="ship">Karar verilen gemi instance'ı</param>
        /// <param name="accepted">Gemi kabul edildi mi?</param>
        public void MakeShipDecision(ShipInstance ship, bool accepted)
        {
            if (ship == null)
            {
                UnityEngine.Debug.LogWarning("[ShipManager] Null ship'e karar verilemez!");
                return;
            }

            if (ship.ShipDecisionMade)
            {
                UnityEngine.Debug.LogWarning($"[ShipManager] {ship.Definition.shipName} için zaten karar verilmiş!");
                return;
            }

            ship.ShipDecisionMade = true;
            ship.ShipAccepted = accepted;

            if (accepted)
            {
                ship.State = ShipState.ProcessingContainers;
            }
            else
            {
                ship.State = ShipState.Rejected;
                // Tüm konteynerlar reddedilmiş sayılır
                ship.PendingContainerDecisions = 0;
            }

            OnShipDecisionMade?.Invoke(ship, accepted);
        }

        /// <summary>
        /// Oyuncu bir konteynır hakkında karar verdi (sadece kabul edilen gemilerde).
        /// </summary>
        public void MakeContainerDecision(ShipInstance ship)
        {
            if (ship != null)
            {
                ship.DecreaseContainerPendingCount();
            }
        }

        /// <summary>
        /// Şu anki gemi.
        /// </summary>
        public ShipInstance CurrentShip => _currentShip;

        /// <summary>
        /// Şu anki gemi hareketi bileşeni (Controller tarafından InitiateDeparture çağırmak için).
        /// </summary>
        public ShipMovement GetCurrentShipMovement() => _currentShipMovement;

        /// <summary>
        /// Kaç gemi kaldı?
        /// </summary>
        public int RemainingShips => _allShipsForDay.Count - _currentShipIndex;

        /// <summary>
        /// Zaman dolduğunda otomatik kararlar.
        /// Henüz karar verilmeyen veya tamamlanmayan gemileri işle.
        /// </summary>
        public void AutoResolveOnTimeUp()
        {
            foreach (var ship in _allShipsForDay)
            {
                if (!ship.ShipDecisionMade)
                {
                    // Henüz karar verilmemiş gemi → reddedilmiş
                    ship.ShipDecisionMade = true;
                    ship.ShipAccepted = false;
                    ship.State = ShipState.Rejected;
                    ship.PendingContainerDecisions = 0;
                }
                else if (ship.ShipAccepted && ship.PendingContainerDecisions > 0)
                {
                    // Kabul edilen ama konteyner kararları tamamlanmamış → kalan konteynerlar reddedilmiş
                    ship.PendingContainerDecisions = 0;
                }
            }
        }

        /// <summary>
        /// Tüm gemilerin özeti (debug).
        /// </summary>
        public List<ShipInstance> AllShips => _allShipsForDay;
    }
}

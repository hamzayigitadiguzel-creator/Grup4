using UnityEngine;
using Storia.Interaction;
using System.Collections.Generic;
using Storia.Presentation;
using System.Linq;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi fiziksel davranışını ve hareketini yönetir.
    /// - Deniz'den limana yaklaşma
    /// - Vinç önünde bekleme
    /// - Limandan ayrılma
    /// - Konteyner cargo hold yönetimi (gemi üstünde spawn)
    /// </summary>
    public class ShipMovement : MonoBehaviour
    {
        [Header("Pozisyonlar")]
        [SerializeField]
        private Vector3 _seaStartPosition = new Vector3(-90f, 0f, 90f);

        [SerializeField]
        private Vector3 _cranePosition = new Vector3(0f, 0f, 55f);

        [SerializeField]
        private Vector3 _seaEndPosition = new Vector3(90f, 0f, 90f);

        [Header("Hız")]
        [SerializeField]
        [Range(0.1f, 20f)]
        private float _approachSpeed = 10f;

        [SerializeField]
        [Range(0.1f, 20f)]
        private float _departureSpeed = 10f;

        [Header("Cargo Hold (Konteyner Grid Düzeni)")]
        [SerializeField]
        [Tooltip("Gemi üstünde konteyner spawn edilecek noktası")]
        private Transform _cargoHoldSpawnPoint;

        [SerializeField]
        [Tooltip("Cargo hold grid satır sayısı")]
        private int _cargoGridRows = Storia.Constants.GameConstants.CargoGridRows;

        [SerializeField]
        [Tooltip("Cargo hold grid sütun sayısı")]
        private int _cargoGridColumns = Storia.Constants.GameConstants.CargoGridColumns;

        [SerializeField]
        [Tooltip("Cargo hold grid hücre boyutu")]
        private float _cargoGridCellSize = Storia.Constants.GameConstants.CargoGridCellSize;

        [Header("Interaction")]
        [SerializeField]
        [Tooltip("Gemi ayrılırken kapatılacak interaction point'ler")]
        private InteractionPoint[] _craneInteractionPoints;

        // Runtime - Çalışma zamanı durumu
        private ShipInstance _linkedShip;  // Bu hareket'i kontrol eden gemi örneği
        private float _transitionProgress = 0f;  // Başlangıç (0) ve bitiş (1) arasındaki ilerleme
        
        // Konteyner yönetimi
        /// <summary>
        /// Cargo hold'daki bir konteyner için meta veriler.
        /// localPosition: Grid'de konteyerin konumu
        /// spawnIndex: Spawn sırası (FIFO - first in, first out kontrol için)
        /// </summary>
        private struct ContainerInfo
        {
            public Vector3 localPosition;   // Cargo hold grid'inde yerel konumu
            public int spawnIndex;          // Cargo hold'da spawn edilme sırası
        }
        
        // Geminin cargo hold'unda bulunan tüm konteynerler
        // Key: Konteyner controller, Value: Pozisyon ve spawn sırası bilgisi
        private Dictionary<ContainerViewController, ContainerInfo> _cargoHoldContainers = new Dictionary<ContainerViewController, ContainerInfo>();
        private int _nextCargoSlot = 0;                         // Cargo hold'da next boş slot indexi
        private ContainerViewController _containerAtCrane = null;  // Şu anda vinç'te işlenmekte olan konteyner

        // Olaylar (Events) - Diğer sistemlere bilgi vermek için
        /// <summary>
        /// Gemi vinç konumuna vardığında tetiklenir.
        /// Dinleyenler: ShipManager, UI koordinatörleri (gemi bilgisini göstermek için)
        /// </summary>
        public event System.Action OnArrivedAtCrane;
        
        /// <summary>
        /// Gemi ayrılmayı tamamladığında tetiklenir.
        /// Dinleyenler: ShipManager, oyun akışı yöneticileri (bir sonraki gemiyi başlatmak için)
        /// </summary>
        public event System.Action OnDeparted;

        /// <summary>
        /// Bu ShipMovement bileşenini belirtilen gemi örneğine bağla.
        /// Hareket başlatmadan önce bu metod çağrılmalıdır.
        /// Not: Aynı ShipMovement pool'dan reuse ediliyorsa, önceki durumu temizler.
        /// </summary>
        /// <param name="ship">Bağlanacak gemi örneği</param>
        public void LinkToShip(ShipInstance ship)
        {
            _linkedShip = ship;
            
            // Başlangıç pozisyonuna git (deniz'den başla)
            transform.position = _seaStartPosition;
            _transitionProgress = 0f;  // Hareket ilerlemesini sıfırla
            _nextCargoSlot = 0;        // Cargo hold'u boşalt
            _cargoHoldContainers.Clear();  // Cargo hold dictionary'sini temizle
            _containerAtCrane = null;      // Vinç'teki konteyner'ı temizle
            
            // Eski konteyner'ları temizle (pool'dan yeni gemi alındığında önceki gemiden kalabilir)
            // Not: Konteyner'lar zaten pool'a geri verilmiş olmalı, ama güvenlik kontrolü yap
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                // Sadece ContainerViewController olan child'ları deaktif et
                if (child.GetComponent<ContainerViewController>() != null)
                {
                    child.gameObject.SetActive(false);
                    child.SetParent(null);  // Gemi'nin child'ı olmaktan çıkar
                }
            }

            // Cargo hold spawn point'i ayarla (varsayılan: gemi transform'u)
            if (_cargoHoldSpawnPoint == null)
            {
                _cargoHoldSpawnPoint = transform;
            }
            
            // Gemi bağlantı bilgisini console'a yazdır (başlatma kontrolü için)
            UnityEngine.Debug.Log($"[ShipMovement] Gemi bağlandı: {ship.Definition.shipName}, Cargo slot sıfırlandı: {_nextCargoSlot}");
        }

        private void Update()
        {
            if (_linkedShip == null)
                return;

            // Geminin mevcut durumuna göre uygun hareket veya bekleme işlemi yap
            switch (_linkedShip.State)
            {
                case ShipState.Approaching:
                    // Denizden limana doğru yavaş yavaş yaklaş
                    MoveTowardsCrane();
                    break;

                case ShipState.Departing:
                case ShipState.Rejected:
                    // Kabul edilen/reddedilen konteynerlerin işlenmesinden sonra denize geri dön
                    MoveAwayFromCrane();
                    break;

                case ShipState.AtCrane:
                case ShipState.ProcessingContainers:
                    // Vinç önünde bekleme - hareket yok, UI karar bekliyor
                    break;

                case ShipState.Gone:
                    // Gemi tamamen ayrıldı, GameObject'i deaktif et
                    gameObject.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Deniz'den vinç konumuna doğru hareket et.
        /// Lerp kullanarak smooth bir yaklaşım sağlar (0=deniz başı, 1=vinç konumu).
        /// </summary>
        private void MoveTowardsCrane()
        {
            // Kalan mesafiye göre ilerleme oranı hesapla
            // Formül: speed / totalDistance * deltaTime = adım miktarı (0-1 arasında)
            _transitionProgress += (_approachSpeed / Vector3.Distance(_seaStartPosition, _cranePosition)) * Time.deltaTime;

            if (_transitionProgress >= 1f)
            {
                // Yaklaşma tamamlandı
                _transitionProgress = 1f;
                transform.position = _cranePosition;
                _linkedShip.State = ShipState.AtCrane;  // Durum değişikliği: vinç'e vardık
                OnArrivedAtCrane?.Invoke();  // Dinleyenleri billendir
            }
            else
            {
                // Başlangıç ve hedef pozisyon arasında ara konumu hesapla
                transform.position = Vector3.Lerp(_seaStartPosition, _cranePosition, _transitionProgress);
            }
        }

        /// <summary>
        /// Vinç konumundan deniz'e doğru geri hareket et (ayrılış).
        /// Approaching'in tersine işler (1'den 0'a doğru gidişle değil, 0'dan 1'e).
        /// </summary>
        private void MoveAwayFromCrane()
        {
            // Kalan mesafiye göre ilerleme oranı hesapla
            _transitionProgress += (_departureSpeed / Vector3.Distance(_cranePosition, _seaEndPosition)) * Time.deltaTime;

            if (_transitionProgress >= 1f)
            {
                // Ayrılış tamamlandı - gemi tamamen gitti
                _transitionProgress = 1f;
                transform.position = _seaEndPosition;
                _linkedShip.State = ShipState.Gone;  // Durum değişikliği: gemi gitti
                OnDeparted?.Invoke();  // Dinleyenleri billendir
                gameObject.SetActive(false);  // GameObject'i deaktif et (pool'a geri dönüş hazırlığı)
            }
            else
            {
                // Vinç'ten deniz'e doğru ara konumu hesapla
                transform.position = Vector3.Lerp(_cranePosition, _seaEndPosition, _transitionProgress);
            }
        }

        /// <summary>
        /// Geminin şu anki durumunu döndür (read-only).
        /// Genelde debugging ve durum kontrolü için kullanılır.
        /// </summary>
        public ShipState CurrentState => _linkedShip?.State ?? ShipState.Gone;

        /// <summary>
        /// Tüm konteyner'lar işlendi, gemi artık ayrılmaya hazır.
        /// Durum geçişini (ProcessingContainers → Departing) yönetir.
        /// </summary>
        public void InitiateDeparture()
        {
            // Ayrılış başlatma isteği logla (debugging için)
            UnityEngine.Debug.Log($"[ShipMovement] Ayrılış başlatıldı - Bağlı Gemi: {_linkedShip?.Definition.shipName}, Durum: {_linkedShip?.State}");
            
            // Bağlı gemi yoksa hata ver ve işlemi durdur
            if (_linkedShip == null)
            {
                UnityEngine.Debug.LogWarning("[ShipMovement] Ayrılış başlatılamadı - Bağlı gemi bulunamadı!");
                return;
            }

            // Eğer hala konteyner işleniyorsa (ProcessingContainers), Departing durumuna geç
            // Konteyner işleme durumundaysak, ayrılış durumuna geç
            if (_linkedShip.State == ShipState.ProcessingContainers)
            {
                UnityEngine.Debug.Log("[ShipMovement] Durum değişimi: ProcessingContainers → Departing");
                _linkedShip.State = ShipState.Departing;
            }

            // Departing veya Rejected durumlarında hareket başlat
            // Departing veya Rejected durumlarında hareket başlat
            if (_linkedShip.State == ShipState.Departing || _linkedShip.State == ShipState.Rejected)
            {
                UnityEngine.Debug.Log($"[ShipMovement] Ayrılış hareketi başlatıldı - Durum: {_linkedShip.State}, İlerleme sıfırlandı: {_transitionProgress} → 0");
                _transitionProgress = 0f;  // Hareket ilerlemesini sıfırla (Rejected için de!)
                CloseAllInteractions();    // Tüm vinç etkileşim noktalarını kapat
            }
            else
            {
                // Uygun olmayan durumdayken ayrılış başlatma denemesi
                UnityEngine.Debug.LogWarning($"[ShipMovement] Ayrılış başlatılamadı - Uygun olmayan durum: {_linkedShip.State}");
            }
        }

        /// <summary>
        /// Tüm vinç etkileşim noktalarını kapat.
        /// Gemi ayrılmaya başlarken oyuncu artık bu gemiyle etkileşim yapamaz.
        /// </summary>
        private void CloseAllInteractions()
        {
            if (_craneInteractionPoints == null) 
                return;

            // Tüm etkileşim noktalarını döngü ile kapat
            foreach (var point in _craneInteractionPoints)
            {
                if (point != null && point.IsActive)
                {
                    point.ForceCloseInteraction();  // Etkileşimi zorla kapat
                }
            }
        }

        /// <summary>
        /// Gemi üstünde (cargo hold) konteyner spawn et - pool'dan.
        /// Spawn sırası: satır x sütun (grid düzeni)
        /// Konteynerler şu sırada işlenir: slot 0, 1, 2... (FIFO)
        /// </summary>
        /// <param name="containerPool">Konteyner nesne havuzu (object pool)</param>
        /// <returns>Spawn edilen konteyner controller'ı, veya hata durumunda null</returns>
        public ContainerViewController SpawnContainerInCargo(Storia.Core.Pooling.ObjectPool<ContainerViewController> containerPool)
        {
            if (_cargoHoldSpawnPoint == null || containerPool == null)
                return null;

            // Cargo hold'un kapasitesi dolup dolmadığını kontrol et
            if (!ValidateCargoCapacity())
                return null;

            // Pool'dan yeni bir konteyner örneği al (Instantiate yapma, pool'dan al)
            // Pool'dan yeni bir konteyner örneği al (Instantiate yapma, pool'dan al)
            ContainerViewController controller = containerPool.Get();
            if (controller == null)
            {
                UnityEngine.Debug.LogError("[ShipMovement] HATA: Konteyner havuzundan örnek alınamadı!");
                return null;
            }
            
            // Konteyner'ın aktif olduğundan emin ol (önceki havuz durumundan bağımsız)
            if (!controller.gameObject.activeSelf)
            {
                controller.gameObject.SetActive(true);
            }

            // Cargo grid'de uygun pozisyonu hesapla ve konteyner'ı o konuma taşı
            Vector3 worldPosition = CalculateCargoSlotPosition(_nextCargoSlot);
            controller.transform.position = worldPosition;
            controller.transform.rotation = Quaternion.identity; // Rotasyonu sıfırla

            // Konteyner'ı geminin child'ı yap (gemi hareket ettiğinde konteynerler de hareket etsin)
            controller.transform.SetParent(transform);
            
            // Grid'de hangi satır ve sütunda olduğunu hesapla (logging ve pozisyon için)
            int row = _nextCargoSlot / _cargoGridColumns;
            int col = _nextCargoSlot % _cargoGridColumns;
            Vector3 localSlotPosition = new Vector3(
                col * _cargoGridCellSize,
                row * _cargoGridCellSize,
                0f
            );

            LogCargoSpawn(_nextCargoSlot, worldPosition);

            // Konteyner'ı cargo hold dictionary'sine ekle (meta veri: pozisyon + spawn sırası)
            _cargoHoldContainers[controller] = new ContainerInfo
            {
                localPosition = localSlotPosition,
                spawnIndex = _nextCargoSlot  // FIFO sırası için saklı
            };
            _nextCargoSlot++;  // Sonraki boş slot'u işaretle

            return controller;
        }

        /// <summary>
        /// Cargo hold'un kalan kapasitesini kontrol eder.
        /// Maksimum kapasite = satır sayısı × sütun sayısı
        /// </summary>
        /// <returns>Yer varsa true, doluysa false</returns>
        private bool ValidateCargoCapacity()
        {
            // Maksimum kapasite kontrolü: satır × sütun
            if (_nextCargoSlot >= (_cargoGridRows * _cargoGridColumns))
            {
                UnityEngine.Debug.LogWarning($"[ShipMovement] UYARI: Kargo deposu dolu! Kapasite: {_cargoGridRows * _cargoGridColumns}, Daha fazla konteyner yerleştirilemez.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Cargo slot indexinden dünya koordinatlarında pozisyon hesapla.
        /// Formül: 
        /// - satır = index / sütun sayısı
        /// - sütun = index % sütun sayısı
        /// - pozisyon = spawn point + (satır*yükseklik, sütun*genişlik, 0)
        /// </summary>
        /// <param name="slotIndex">Cargo hold'daki slot indexi (0-max arasında)</param>
        /// <returns>Dünya koordinatlarında pozisyon</returns>
        private Vector3 CalculateCargoSlotPosition(int slotIndex)
        {
            // Grid satır ve sütun hesapla
            int row = slotIndex / _cargoGridColumns;
            int col = slotIndex % _cargoGridColumns;

            // Gemi özel koordinat sisteminde (local) pozisyonu hesapla
            Vector3 localSlotPosition = new Vector3(
                col * _cargoGridCellSize,  // X ekseninde (sütun genişliği)
                row * _cargoGridCellSize,  // Y ekseninde (satır yüksekliği)
                0f                         // Z ekseninde (derinlik yok)
            );

            // Spawn point'i merkez olarak kullanarak dünya pozisyonunu hesapla
            return _cargoHoldSpawnPoint.position + localSlotPosition;
        }

        /// <summary>
        /// Konteyner spawn olayını log'a yaz (debugging ve takip için).
        /// </summary>
        private void LogCargoSpawn(int slotIndex, Vector3 worldPosition)
        {
            // Konteyner spawn olayını console'a yazdır
            UnityEngine.Debug.Log($"[ShipMovement] Kargo deposuna konteyner eklendi - Slot: {slotIndex}, Dünya Pozisyonu: {worldPosition}");
        }

        /// <summary>
        /// Konteyner'ı cargo hold'dan vinç'e (crane) taşı.
        /// Konteyner artık geminin child'ı olmaktan çıkar ve bağımsız hareket edebilir.
        /// </summary>
        /// <param name="containerController">Taşınacak konteyner'ın controller'ı</param>
        /// <param name="targetPosition">Hedef pozisyon (genelde vinç/crane pozisyonu)</param>
        public void MoveContainerToCrane(ContainerViewController containerController, Vector3 targetPosition)
        {
            // Geçerlilik kontrolü: konteyner ve cargo hold kaydı var mı?
            if (containerController == null || !_cargoHoldContainers.ContainsKey(containerController))
                return;

            // Konteyner'ın parent'ı olarak gemiyi çıkar (böylece gemi hareket etsede konteyner etkilenmez)
            containerController.transform.SetParent(null);

            // Konteyner'ı hedef pozisyona (vinç konumuna) taşı
            containerController.transform.position = targetPosition;
            
            // Şu anda vinç'te işlenmekte olan konteyner olarak işaretle
            _containerAtCrane = containerController;
        }

        /// <summary>
        /// Red edilen konteyner'ı cargo hold'a geri taşı.
        /// Reddetilen konteyner bir daha işlenmez (dictionary'den çıkarılır).
        /// Konteyner fiziksel olarak gemi içinde kalır ve gemi ayrılırken silinir.
        /// </summary>
        /// <param name="containerController">Geri taşınacak konteyner</param>
        public void ReturnContainerToCargo(ContainerViewController containerController)
        {
            // Geçerlilik kontrolü: konteyner ve cargo hold kaydı var mı?
            if (containerController == null || !_cargoHoldContainers.ContainsKey(containerController))
                return;

            // Konteyner'ı tekrar geminin child'ı yap (gemi hareket etsin diye)
            containerController.transform.SetParent(transform);

            // Cargo hold'daki orijinal local pozisyonuna geri taşı
            var info = _cargoHoldContainers[containerController];
            containerController.transform.localPosition = info.localPosition;
            
            // KRITIK: Reddetilen konteyner'ı dictionary'den sil (ikinci kez işlenmesin)
            _cargoHoldContainers.Remove(containerController);
            
            // Eğer bu konteyner vinç'teyse, referansı temizle
            if (_containerAtCrane == containerController)
                _containerAtCrane = null;
        }

        /// <summary>
        /// Kabul edilen konteyner'ı cargo hold'dan çıkar (yer tahsisine taşınacak).
        /// Konteyner artık gemi sisteminin parçası değildir.
        /// </summary>
        /// <param name="containerController">Çıkarılacak konteyner</param>
        public void RemoveContainerFromCargo(ContainerViewController containerController)
        {
            // Geçerlilik kontrolü: konteyner ve cargo hold kaydı var mı?
            if (containerController != null && _cargoHoldContainers.ContainsKey(containerController))
            {
                // Konteyner'ı cargo hold dictionary'sinden tamamen çıkar (kabul edildi)
                _cargoHoldContainers.Remove(containerController);
                
                // Konteyner'ın parent'ı olarak dünyayı ayarla (zone'a taşınmaya hazırla)
                containerController.transform.SetParent(null);
                
                // Eğer bu konteyner vinç'teyse, referansı temizle
                if (_containerAtCrane == containerController)
                    _containerAtCrane = null;
            }
        }

        /// <summary>
        /// Gemideki tüm kalan konteyner'ları döndür (spawn sırasına göre sıralı).
        /// Vinç'te olan konteyner hariç tutulur (zaten işleniyor).
        /// Sıralama: spawn sırasına göre (FIFO - ilk spawn edilen ilk işlenir).
        /// </summary>
        /// <returns>Cargo hold'da kalan konteyner'ların listesi (sıralı)</returns>
        public List<ContainerViewController> GetAllCargoContainers()
        {
            // Dictionary'deki konteyner'ları:
            // 1. Vinç'te olanı hariç tut
            // 2. Spawn sırasına göre sırala (spawnIndex'e göre)
            // 3. Liste olarak döndür
            var containers = _cargoHoldContainers
                .Where(kvp => kvp.Key != _containerAtCrane)  // Vinç'teki hariç
                .OrderBy(kvp => kvp.Value.spawnIndex)        // Spawn sırasına göre sırala
                .Select(kvp => kvp.Key)                      // Sadece key'i (controller'ı) al
                .ToList();
            
            return containers;
        }
    }
}

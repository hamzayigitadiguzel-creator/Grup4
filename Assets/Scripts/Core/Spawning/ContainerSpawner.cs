using UnityEngine;
using System.Collections.Generic;
using Storia.Presentation;
using Storia.Core.Interfaces;
using Storia.Data.Generated;
using Storia.Core.Ship;
using Storia.Core.Pooling;

namespace Storia.Core.Spawning
{
    /// <summary>
    /// Konteyner spawning ve lifecycle yönetimi.
    /// - Gemi üstünde (cargo hold) konteyner spawn
    /// - Vinç pozisyonuna taşıma
    /// - Zone'a taşıma
    /// - Gemi'ye geri taşıma
    /// - Object pooling ile konteyner yönetimi
    /// </summary>
    public sealed class ContainerSpawner : MonoBehaviour, ISpawner<(ContainerData, ContainerPresentation), ContainerViewController>
    {
        [Header("Prefab")]
        [SerializeField] private GameObject _containerPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform _initialSpawnPoint;
        [SerializeField] private Vector3 _spawnOffset = Vector3.up * 2f;
        
        [Header("Pooling Settings")]
        [SerializeField] private int _initialPoolSize = 20;
        [SerializeField] private int _maxPoolSize = 50;

        [Header("Runtime")]
        private List<ContainerViewController> _activeContainers = new List<ContainerViewController>();
        private List<ContainerViewController> _placedContainers = new List<ContainerViewController>();
        private ContainerViewController _currentContainer;
        private ShipMovement _currentShipMovement = null;
        
        // Object pool
        private ObjectPool<ContainerViewController> _containerPool;
        
        private void Awake()
        {
            // Object pool'u initialize et
            if (_containerPrefab != null)
            {
                _containerPool = new ObjectPool<ContainerViewController>(
                    _containerPrefab,
                    _initialPoolSize,
                    _maxPoolSize,
                    allowGrowth: true,
                    poolContainer: transform
                );
            }
        }
        
        public void SetCurrentShipMovement(ShipMovement shipMovement)
        {
            _currentShipMovement = shipMovement;
        }

        /// <summary>
        /// Vinç pozisyonu referansını döndürür.
        /// </summary>
        public Transform GetInitialSpawnPoint() => _initialSpawnPoint;

        /// <summary>
        /// Spawn offset değerini döndürür.
        /// </summary>
        public Vector3 GetSpawnOffset() => _spawnOffset;

        /// <summary>
        /// Yeni konteyner pool'dan al ve mevcut konteyneri deaktive et.
        /// </summary>
        /// <param name="containerData">Konteyner verisi</param>
        /// <param name="presentation">Görsel sunum bilgisi</param>
        /// <returns>Spawn edilen konteyner controller'ı, null güvenliği garanti edilir</returns>
        public ContainerViewController SpawnContainer(ContainerData containerData, ContainerPresentation presentation)
        {
            if (_containerPool == null)
            {
                UnityEngine.Debug.LogError("[ContainerSpawner] Container pool oluşturulmamış!");
                return null;
            }

            if (containerData == null)
            {
                UnityEngine.Debug.LogError("[ContainerSpawner] ContainerData null olamaz!");
                return null;
            }

            // Mevcut konteyneri gizle (sonraki konteyner için)
            if (_currentContainer != null)
            {
                _currentContainer.gameObject.SetActive(false);
            }

            // Pool'dan konteyner al
            ContainerViewController controller = _containerPool.Get();
            
            if (controller == null)
            {
                UnityEngine.Debug.LogError("[ContainerSpawner] Pool'dan konteyner alınamadı!");
                return null;
            }

            // Vinç pozisyonuna taşı
            Vector3 spawnPos = _initialSpawnPoint != null 
                ? _initialSpawnPoint.position + _spawnOffset 
                : Vector3.zero + _spawnOffset;
            
            controller.transform.position = spawnPos;
            controller.transform.rotation = Quaternion.identity;
            controller.Initialize(containerData, presentation);
            
            _activeContainers.Add(controller);
            _currentContainer = controller;

            return controller;
        }

        /// <summary>
        /// Gemi üstünde (cargo hold) tüm konteynerler spawn et.
        /// </summary>
        public void SpawnAllContainersInShip(List<ContainerData> shipContainers, ContainerPresentation[] presentations, ShipMovement shipMovement)
        {
            if (shipMovement == null || _containerPool == null)
                return;

            _currentShipMovement = shipMovement;

            for (int i = 0; i < shipContainers.Count; i++)
            {
                var containerData = shipContainers[i];
                var presentation = presentations != null && i < presentations.Length ? presentations[i] : null;

                // Konteyner spawn et (cargo hold'da) - pool kullanarak
                var controller = shipMovement.SpawnContainerInCargo(_containerPool);

                if (controller != null)
                {
                    // Konteyner verisi ve presentation'ı ile initialize et
                    controller.Initialize(containerData, presentation);
                    
                    // Konteyner'ın kesinlikle görünür olduğundan emin ol
                    controller.gameObject.SetActive(true);
                    
                    _activeContainers.Add(controller);
                    UnityEngine.Debug.Log($"[ContainerSpawner] Cargo hold'a konteyner {i + 1}/{shipContainers.Count} eklendi: {containerData.truth.containerId}");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[ContainerSpawner] Cargo hold'a konteyner {i + 1} eklenemedi!");
                }
            }
            
            UnityEngine.Debug.Log($"[ContainerSpawner] Toplam {shipContainers.Count} konteyner cargo hold'a spawn edildi.");
        }

        /// <summary>
        /// ISpawner implementasyonu: Tuple parametresi ile spawn
        /// </summary>
        public ContainerViewController Spawn((ContainerData, ContainerPresentation) data)
        {
            return SpawnContainer(data.Item1, data.Item2);
        }

        /// <summary>
        /// Mevcut konteyneri bir zone'a taşır ve placed list'ine ekler.
        /// </summary>
        public void MoveCurrentContainerToZone(Vector3 targetPosition)
        {
            if (_currentContainer == null) return;

            _currentContainer.transform.position = targetPosition;
            _currentContainer.gameObject.SetActive(true); // Zone'da görünür kal
            
            // Active list'ten çıkart, placed list'ine ekle
            _activeContainers.Remove(_currentContainer);
            _placedContainers.Add(_currentContainer);
            
            _currentContainer = null; // Artık "current" değil
        }

        /// <summary>
        /// Red edilen konteyneri pool'a geri ver.
        /// </summary>
        public void DestroyCurrentContainer()
        {
            if (_currentContainer == null) return;

            _activeContainers.Remove(_currentContainer);
            _containerPool?.Return(_currentContainer);
            _currentContainer = null;
        }

        /// <summary>
        /// ISpawner implementasyonu: Mevcut objeyi yok et
        /// </summary>
        public void DestroyCurrent() => DestroyCurrentContainer();

        /// <summary>
        /// Tüm konteynerleri temizler (yeni gün başında).
        /// Hem processing hem de placed containerları pool'a geri verir.
        /// </summary>
        public void ClearAllContainers()
        {
            foreach (var container in _activeContainers)
            {
                if (container != null)
                    _containerPool?.Return(container);
            }

            _activeContainers.Clear();
            _currentContainer = null;
        }

        /// <summary>
        /// Placed containerları temizler (gün sonu).
        /// Yerleştirilen konteynerleri pool'a geri verir.
        /// </summary>
        public void ClearPlacedContainers()
        {
            // Processing containerları pool'a geri ver
            ClearAllContainers();

            // Yerleştirilen containerları pool'a geri ver
            foreach (var container in _placedContainers)
            {
                if (container != null)
                    _containerPool?.Return(container);
            }

            _placedContainers.Clear();
        }

        /// <summary>
        /// ISpawner implementasyonu: Tüm objeleri temizle
        /// </summary>
        public void ClearAll() => ClearAllContainers();

        /// <summary>
        /// Mevcut konteyneri döndürür.
        /// </summary>
        public ContainerViewController GetCurrentContainer() => _currentContainer;

        /// <summary>
        /// Mevcut konteyneri ayarlar (dışarıdan kontrol için).
        /// </summary>
        public void SetCurrentContainer(ContainerViewController container)
        {
            _currentContainer = container;
        }

        /// <summary>
        /// ISpawner implementasyonu: Mevcut obje
        /// </summary>
        public ContainerViewController GetCurrent() => _currentContainer;
    }
}

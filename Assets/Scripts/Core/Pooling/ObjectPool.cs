using UnityEngine;
using System.Collections.Generic;

namespace Storia.Core.Pooling
{
    /// <summary>
    /// Generic object pooling sistemi.
    /// GameObject'lerin sürekli Instantiate/Destroy edilmesi yerine
    /// pool'dan alınıp geri verilmesini sağlar.
    /// </summary>
    /// <typeparam name="T">Pool edilecek component tipi (MonoBehaviour türevi)</typeparam>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly GameObject _prefab;
        private readonly Transform _poolContainer;
        private readonly Queue<T> _availableObjects;
        private readonly List<T> _allObjects;
        private readonly int _maxSize;
        private readonly bool _allowGrowth;

        /// <summary>
        /// Object Pool oluşturur.
        /// </summary>
        /// <param name="prefab">Pool edilecek prefab</param>
        /// <param name="initialSize">Başlangıç pool boyutu</param>
        /// <param name="maxSize">Maksimum pool boyutu (0 = sınırsız)</param>
        /// <param name="allowGrowth">Pool dolunca yeni obje oluşturulabilir mi?</param>
        /// <param name="poolContainer">Pool objelerinin parent'ı (opsiyonel)</param>
        public ObjectPool(GameObject prefab, int initialSize = 10, int maxSize = 0,
                         bool allowGrowth = true, Transform poolContainer = null)
        {
            _prefab = prefab;
            _maxSize = maxSize;
            _allowGrowth = allowGrowth;

            // Pool container oluştur (hiyerarşi düzeni için)
            if (poolContainer == null)
            {
                GameObject containerObj = new GameObject($"[Pool] {prefab.name}");
                _poolContainer = containerObj.transform;
            }
            else
            {
                _poolContainer = poolContainer;
            }

            _availableObjects = new Queue<T>(initialSize);
            _allObjects = new List<T>(initialSize);

            // İlk objeleri oluştur
            for (int i = 0; i < initialSize; i++)
            {
                CreateNewObject();
            }
        }

        /// <summary>
        /// Pool'dan obje al. Pool boşsa yeni obje oluşturur (allowGrowth=true ise).
        /// </summary>
        public T Get()
        {
            T obj = null;

            // Pool'da mevcut obje varsa al
            if (_availableObjects.Count > 0)
            {
                obj = _availableObjects.Dequeue();
            }
            // Pool boş, yeni obje oluştur (izin veriliyorsa)
            else if (_allowGrowth && (_maxSize == 0 || _allObjects.Count < _maxSize))
            {
                obj = CreateNewObject();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[ObjectPool] Pool dolu ve büyüme izni yok! Prefab: {_prefab.name}");
                return null;
            }

            // Objeyi aktifleştir
            if (obj != null)
            {
                obj.gameObject.SetActive(true);
            }

            return obj;
        }

        /// <summary>
        /// Objeyi pool'a geri ver.
        /// </summary>
        public void Return(T obj)
        {
            if (obj == null)
                return;

            // Obje bu pool'a ait mi kontrol et
            if (!_allObjects.Contains(obj))
            {
                UnityEngine.Debug.LogWarning($"[ObjectPool] Obje bu pool'a ait değil! {obj.name}");
                return;
            }

            // Objeyi deaktive et ve pool'a geri koy
            obj.gameObject.SetActive(false);
            obj.transform.SetParent(_poolContainer);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            _availableObjects.Enqueue(obj);
        }

        /// <summary>
        /// Pool'daki tüm objeleri yok et.
        /// </summary>
        public void Clear()
        {
            foreach (var obj in _allObjects)
            {
                if (obj != null)
                    Object.Destroy(obj.gameObject);
            }

            _availableObjects.Clear();
            _allObjects.Clear();
        }

        /// <summary>
        /// Yeni obje oluştur ve pool'a ekle.
        /// </summary>
        private T CreateNewObject()
        {
            GameObject newObj = Object.Instantiate(_prefab, _poolContainer);
            newObj.SetActive(false);

            T component = newObj.GetComponent<T>();
            if (component == null)
            {
                UnityEngine.Debug.LogError($"[ObjectPool] Prefab'da {typeof(T).Name} component'i bulunamadı!");
                Object.Destroy(newObj);
                return null;
            }

            _allObjects.Add(component);
            _availableObjects.Enqueue(component);

            return component;
        }

        /// <summary>
        /// Pool'daki toplam obje sayısı (aktif + pasif).
        /// </summary>
        public int TotalCount => _allObjects.Count;

        /// <summary>
        /// Pool'da kullanılabilir (pasif) obje sayısı.
        /// </summary>
        public int AvailableCount => _availableObjects.Count;

        /// <summary>
        /// Şu anda aktif obje sayısı.
        /// </summary>
        public int ActiveCount => _allObjects.Count - _availableObjects.Count;
    }
}

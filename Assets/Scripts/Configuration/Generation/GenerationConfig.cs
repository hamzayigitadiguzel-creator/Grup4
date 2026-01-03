using UnityEngine;

namespace Storia.Generation
{
    /// <summary>
    /// Prosedürel üretim için master konfigürasyon.
    /// Konteyner sayısı, gemi sayısı, kural karmaşıklığı, çelişki oranları vb.
    /// </summary>
    [CreateAssetMenu(menuName = "Storia/Generation/Generation Config", fileName = "GenerationConfig")]
    public sealed class GenerationConfig : ScriptableObject
    {
        [Header("Container Generation")]
        [SerializeField]
        [Range(10, 100)]
        [Tooltip("Minimum konteyner sayısı")]
        private int _minContainerCount = 20;

        [SerializeField]
        [Range(10, 100)]
        [Tooltip("Maksimum konteyner sayısı")]
        private int _maxContainerCount = 40;

        [Header("Ship Generation")]
        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Minimum gemi sayısı")]
        private int _minShips = 2;

        [SerializeField]
        [Range(1, 10)]
        [Tooltip("Maksimum gemi sayısı")]
        private int _maxShips = 5;

        [SerializeField]
        [Range(12f, 120f)]
        [Tooltip("Minimum yolculuk süresi (saat)")]
        private float _minVoyageDuration = 24f;

        [SerializeField]
        [Range(12f, 120f)]
        [Tooltip("Maksimum yolculuk süresi (saat)")]
        private float _maxVoyageDuration = 72f;

        [Header("Task Rule Generation")]
        [SerializeField]
        [Range(0.1f, 0.9f)]
        [Tooltip("Hedef konteyner oranı (0.3 = %30 konteyner hedef)")]
        private float _targetContainerRatio = 0.3f;

        [SerializeField]
        [Range(1, 5)]
        [Tooltip("Maksimum AND/OR rule derinliği")]
        private int _maxRuleComplexity = 2;

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("Composite rule (AND/OR) kullanma şansı")]
        private float _compositeRuleChance = 0.3f;

        [Header("Placement Rule Generation")]
        [SerializeField]
        [Range(0.3f, 1f)]
        [Tooltip("Konteynerlerin ne kadarı için placement rule tanımlı olacak")]
        private float _placementCoverageRatio = 0.8f;

        [SerializeField]
        [Tooltip("Cargo-based placement rule kullanılsın mı?")]
        private bool _useCargoBasedPlacement = true;

        [SerializeField]
        [Tooltip("ID-based placement rule kullanılsın mı?")]
        private bool _useIdBasedPlacement = false;

        [Header("Presentation (Conflict) Settings")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Çelişki oluşma şansı")]
        private float _conflictChance = 0.35f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Manifest tamper bias (0.5 = %50 manifest, %50 label)")]
        private float _manifestTamperBias = 0.5f;

        // Public accessors
        public int MinContainerCount => _minContainerCount;
        public int MaxContainerCount => _maxContainerCount;
        public int MinShips => _minShips;
        public int MaxShips => _maxShips;
        public float MinVoyageDuration => _minVoyageDuration;
        public float MaxVoyageDuration => _maxVoyageDuration;
        public float TargetContainerRatio => _targetContainerRatio;
        public int MaxRuleComplexity => _maxRuleComplexity;
        public float CompositeRuleChance => _compositeRuleChance;
        public float PlacementCoverageRatio => _placementCoverageRatio;
        public bool UseCargoBasedPlacement => _useCargoBasedPlacement;
        public bool UseIdBasedPlacement => _useIdBasedPlacement;
        public float ConflictChance => _conflictChance;
        public float ManifestTamperBias => _manifestTamperBias;

        /// <summary>
        /// Seed'e bağlı olarak konteyner sayısı belirle.
        /// </summary>
        public int CalculateContainerCount(DeterministicRng rng)
        {
            return rng.RangeInt(_minContainerCount, _maxContainerCount + 1);
        }

        /// <summary>
        /// Seed'e bağlı olarak gemi sayısı belirle.
        /// </summary>
        public int CalculateShipCount(DeterministicRng rng)
        {
            return rng.RangeInt(_minShips, _maxShips + 1);
        }

        /// <summary>
        /// Seed'e bağlı olarak yolculuk süresi üret.
        /// </summary>
        public float GenerateVoyageDuration(DeterministicRng rng)
        {
            float t = rng.Next01();
            return Mathf.Lerp(_minVoyageDuration, _maxVoyageDuration, t);
        }

        /// <summary>
        /// Kaç tane hedef konteyner olması gerektiğini hesapla.
        /// </summary>
        public int CalculateTargetContainerCount(int containerCount)
        {
            return Mathf.RoundToInt(containerCount * _targetContainerRatio);
        }

        private void OnValidate()
        {
            // Min/Max kontrolü
            if (_minContainerCount > _maxContainerCount)
                _maxContainerCount = _minContainerCount;

            if (_minShips > _maxShips)
                _maxShips = _minShips;

            if (_minVoyageDuration > _maxVoyageDuration)
                _maxVoyageDuration = _minVoyageDuration;

            // Gemi sayısı max konteyner sayısını geçemez
            if (_maxShips > _maxContainerCount)
                _maxShips = _maxContainerCount;
        }
    }
}

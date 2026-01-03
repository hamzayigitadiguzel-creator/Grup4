using UnityEngine;
using System.Collections.Generic;

namespace Storia.Generation
{
    /// <summary>
    /// Prosedürel üretim için kullanılacak tüm havuz verilerini tutar.
    /// Origin portlar, cargo türleri, konteyner ID formatları, gemi isimleri vb.
    /// </summary>
    [CreateAssetMenu(menuName = "Storia/Generation/Pools Config", fileName = "PoolsConfig")]
    public sealed class PoolsConfig : ScriptableObject
    {
        [Header("Container Pools")]
        [SerializeField]
        [Tooltip("Konteyner köken limanları havuzu")]
        private string[] _originPorts = 
        {
            "Pire", "İzmir", "Varna", "Trieste", "Marsilya",
            "İskenderun", "Antalya", "Barcelona", "Napoli", "Odesa"
        };

        [SerializeField]
        [Tooltip("Kargo türleri havuzu")]
        private string[] _cargoTypes = 
        {
            "Makine Parçası", "Gıda", "Tekstil", "Kimyasal", "Elektronik",
            "Mobilya", "İnşaat Malzemesi", "Otomotiv", "Tıbbi Malzeme", "Kitap"
        };

        [SerializeField]
        [Tooltip("Konteyner ID prefix'leri (örn: TRBU, USNY)")]
        private string[] _containerIdPrefixes = 
        {
            "TRBU", "TRIS", "TRAN", "USNY", "GRPI",
            "ITNA", "ESBA", "FRMA", "BGVA", "UAOD"
        };

        [Header("Ship Pools")]
        [SerializeField]
        [Tooltip("Gemi isim şablonları")]
        private string[] _shipNameTemplates = 
        {
            "M/V {0} Star", "M/V {0} Pride", "M/V {0} Spirit", "M/V {0} Voyager",
            "M/V {0} Navigator", "M/V {0} Trader", "M/V {0} Express", "M/V {0} Discovery"
        };

        [SerializeField]
        [Tooltip("Gemi isimleri için kullanılacak adjective'ler")]
        private string[] _shipAdjectives = 
        {
            "Aegean", "Mediterranean", "Black Sea", "Atlantic", "Pacific",
            "Golden", "Silver", "Blue", "Royal", "Imperial", "Eastern", "Western"
        };

        [Header("Zone Names")]
        [SerializeField]
        [Tooltip("Yerleştirme zone isimleri")]
        private string[] _zoneNames = 
        {
            "Zone A", "Zone B", "Zone C"
        };

        // Public read-only accessors
        public IReadOnlyList<string> OriginPorts => _originPorts;
        public IReadOnlyList<string> CargoTypes => _cargoTypes;
        public IReadOnlyList<string> ContainerIdPrefixes => _containerIdPrefixes;
        public IReadOnlyList<string> ShipNameTemplates => _shipNameTemplates;
        public IReadOnlyList<string> ShipAdjectives => _shipAdjectives;
        public IReadOnlyList<string> ZoneNames => _zoneNames;

        /// <summary>
        /// Rastgele bir origin port seç.
        /// </summary>
        public string GetRandomOriginPort(DeterministicRng rng)
        {
            if (_originPorts == null || _originPorts.Length == 0)
                return "Unknown";

            int index = rng.RangeInt(0, _originPorts.Length);
            return _originPorts[index];
        }

        /// <summary>
        /// Rastgele bir cargo type seç.
        /// </summary>
        public string GetRandomCargoType(DeterministicRng rng)
        {
            if (_cargoTypes == null || _cargoTypes.Length == 0)
                return "Unknown";

            int index = rng.RangeInt(0, _cargoTypes.Length);
            return _cargoTypes[index];
        }

        /// <summary>
        /// Rastgele bir container ID üret.
        /// Format: [PREFIX]-[NUMBER] (örn: TRBU-1042)
        /// </summary>
        public string GenerateContainerId(DeterministicRng rng)
        {
            if (_containerIdPrefixes == null || _containerIdPrefixes.Length == 0)
                return "UNKN-0000";

            string prefix = _containerIdPrefixes[rng.RangeInt(0, _containerIdPrefixes.Length)];
            int number = rng.RangeInt(1000, 10000);
            return $"{prefix}-{number}";
        }

        /// <summary>
        /// Rastgele bir gemi ismi üret.
        /// Format: M/V [Adjective] [Template] (örn: M/V Aegean Star)
        /// </summary>
        public string GenerateShipName(DeterministicRng rng)
        {
            if (_shipNameTemplates == null || _shipNameTemplates.Length == 0 ||
                _shipAdjectives == null || _shipAdjectives.Length == 0)
                return "M/V Unknown";

            string adjective = _shipAdjectives[rng.RangeInt(0, _shipAdjectives.Length)];
            string template = _shipNameTemplates[rng.RangeInt(0, _shipNameTemplates.Length)];
            
            return string.Format(template, adjective);
        }

        /// <summary>
        /// Rastgele bir gemi ID üret.
        /// Format: SH-[NUMBER] (örn: SH-001)
        /// </summary>
        public string GenerateShipId(DeterministicRng rng, int sequenceNumber)
        {
            return $"SH-{sequenceNumber:D3}";
        }

        /// <summary>
        /// Rastgele bir zone name seç.
        /// </summary>
        public string GetRandomZoneName(DeterministicRng rng)
        {
            if (_zoneNames == null || _zoneNames.Length == 0)
                return "Zone A";

            int index = rng.RangeInt(0, _zoneNames.Length);
            return _zoneNames[index];
        }

        /// <summary>
        /// Rastgele bir ZoneId + zone name seç (deterministic).
        /// ZoneId = index + 1 (1-based, display friendly).
        /// </summary>
        public (int zoneId, string zoneName) GetRandomZoneIdAndName(DeterministicRng rng)
        {
            if (_zoneNames == null || _zoneNames.Length == 0)
                return (1, "Zone A");

            int index = rng.RangeInt(0, _zoneNames.Length);
            int zoneId = index + 1; // 1-based ZoneId
            string zoneName = _zoneNames[index];
            return (zoneId, zoneName);
        }
    }
}

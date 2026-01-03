using UnityEngine;

namespace Storia.Core.Placement
{
    /// <summary>
    /// Liman yerleştirme bölgesi.
    /// Kabul edilen konteynerlerin yerleştirileceği zone'ları temsil eder.
    /// </summary>
    public sealed class PlacementZone : MonoBehaviour
    {
        [Header("Zone Ayarları")]
        [Tooltip("Zone kimliği. Her zone için benzersiz olmalı.")]
        [SerializeField] private int _zoneId;
        [Tooltip("Zone adı.")]
        [SerializeField] private string _zoneName;
        [Tooltip("Konteynerlerin spawn edileceği nokta.")]
        [SerializeField] private Transform _spawnPoint;
        [Tooltip("Zone içindeki grid boyutu (X: sütun sayısı, Y: satır sayısı).")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(5, 5);
        [Tooltip("Her hücrenin boyutu (metre cinsinden).")]
        [SerializeField] private float _cellSize = 2.5f;

        [Header("Debug")]
        [Tooltip("Zone için gizmo çizimi yapılıp yapılmayacağı.")]
        [SerializeField] private bool _drawGizmos = true;
        [Tooltip("Gizmo çizim rengi.")]
        [SerializeField] private Color _gizmoColor = Color.green;

        private int _nextSlotIndex = 0;

        public int ZoneId => _zoneId;
        public string ZoneName => _zoneName;

        /// <summary>
        /// Zone'da bir sonraki boş slot pozisyonunu döndürür.
        /// </summary>
        public Vector3 GetNextSlotPosition()
        {
            if (_spawnPoint == null)
                _spawnPoint = transform;

            int x = _nextSlotIndex % _gridSize.x;
            int z = _nextSlotIndex / _gridSize.x;

            _nextSlotIndex++;

            Vector3 localOffset = new Vector3(x * _cellSize, 0, z * _cellSize);
            return _spawnPoint.position + localOffset;
        }

        /// <summary>
        /// Zone'u reset eder (yeni gün başında).
        /// </summary>
        public void ResetZone()
        {
            _nextSlotIndex = 0;
        }

        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Editor'da duplicate ZoneId kontrolü
            PlacementZone[] allZones = FindObjectsByType<PlacementZone>(FindObjectsSortMode.None);
            System.Collections.Generic.Dictionary<int, PlacementZone> idMap = new System.Collections.Generic.Dictionary<int, PlacementZone>();

            foreach (var zone in allZones)
            {
                if (zone == null) continue;
                
                if (idMap.ContainsKey(zone._zoneId))
                {
                    Debug.LogError($"[PlacementZone] Çift ZoneId saptandı: {zone._zoneId} '{zone.gameObject.name}' ve '{idMap[zone._zoneId].gameObject.name}' üzerinde", this);
                }
                else
                {
                    idMap[zone._zoneId] = zone;
                }
            }
        }
        #endif

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            Transform point = _spawnPoint != null ? _spawnPoint : transform;

            Gizmos.color = _gizmoColor;

            // Grid çiz
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int z = 0; z < _gridSize.y; z++)
                {
                    Vector3 cellPos = point.position + new Vector3(x * _cellSize, 0, z * _cellSize);
                    Gizmos.DrawWireCube(cellPos + Vector3.up * 0.5f, new Vector3(_cellSize * 0.9f, 1f, _cellSize * 0.9f));
                }
            }

            // Zone etiketi (Scene view'da görünür)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(point.position + Vector3.up * 2f, _zoneName);
            #endif
        }
    }
}

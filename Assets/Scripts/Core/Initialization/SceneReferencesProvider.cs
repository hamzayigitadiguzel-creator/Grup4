using UnityEngine;
using Storia.UI;
using Storia.Core.Spawning;
using Storia.Core.Placement;
using Storia.Interaction;

namespace Storia.Core.Initialization
{
    /// <summary>
    /// Scene'deki MonoBehaviour referanslarını tutan veri yapısı.
    /// </summary>
    public sealed class SceneReferencesProvider : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private DayTimer _timer;
        [SerializeField] private HUDView _hudView;
        [SerializeField] private PrototypeUI _workstationUI;
        [SerializeField] private ContainerSpawner _containerSpawner;
        [SerializeField] private PlacementZone[] _placementZones;
        [SerializeField] private InteractionPoint _craneInteractionPoint;

        [Header("Developer Mode")]
        [SerializeField] private Diagnostics.DevModeManager _devModeManager;
        [SerializeField] private DevOverlayUI _devOverlayUI;

        // Temiz erişim için genel özellikler
        public DayTimer Timer => _timer;
        public IHUDView HUD => _hudView;
        public IWorkstationView Workstation => _workstationUI;
        public ContainerSpawner ContainerSpawner => _containerSpawner;
        public PlacementZone[] PlacementZones => _placementZones;
        public InteractionPoint CraneInteractionPoint => _craneInteractionPoint;
        public Storia.Diagnostics.DevModeManager DevModeManager => _devModeManager;
        public DevOverlayUI DevOverlayUI => _devOverlayUI;

        private void Awake()
        {
            // Doğrulama (defansif programlama)
            if (_timer == null)
                UnityEngine.Debug.LogError("[SceneReferencesProvider] DayTimer referansı eksik!");
            if (_hudView == null)
                UnityEngine.Debug.LogError("[SceneReferencesProvider] HUDView referansı eksik!");
            if (_workstationUI == null)
                UnityEngine.Debug.LogError("[SceneReferencesProvider] PrototypeUI referansı eksik!");
            if (_containerSpawner == null)
                UnityEngine.Debug.LogError("[SceneReferencesProvider] ContainerSpawner referansı eksik!");
            if (_placementZones == null || _placementZones.Length == 0)
                UnityEngine.Debug.LogError("[SceneReferencesProvider] PlacementZones dizisi boş!");
        }
    }
}

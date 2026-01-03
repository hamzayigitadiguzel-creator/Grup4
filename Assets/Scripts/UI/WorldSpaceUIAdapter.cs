using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Storia.UI
{
    /// <summary>
    /// PrototypeUI'yi InteractionPoint sistemiyle entegre eden adapter component.
    /// </summary>
    [RequireComponent(typeof(PrototypeUI))]
    public sealed class WorldSpaceUIAdapter : MonoBehaviour
    {
        private PrototypeUI _prototypeUI;
        private Canvas _canvas;
        private GraphicRaycaster _raycaster;

        private void Awake()
        {
            _prototypeUI = GetComponent<PrototypeUI>();
            _canvas = GetComponent<Canvas>();

            // Canvas'ı world space olarak ayarla
            if (_canvas != null)
            {
                _canvas.renderMode = RenderMode.WorldSpace;
                
                // World Space Canvas için Event Camera ayarla
                _canvas.worldCamera = UnityEngine.Camera.main;
            }

            // GraphicRaycaster ekle (UI etkileşimi için gerekli)
            _raycaster = GetComponent<GraphicRaycaster>();
            if (_raycaster == null)
            {
                _raycaster = gameObject.AddComponent<GraphicRaycaster>();
            }

            // EventSystem kontrolü
            EnsureEventSystem();
        }

        /// <summary>
        /// UI reference'ı döndürür.
        /// </summary>
        public PrototypeUI GetPrototypeUI()
        {
            return _prototypeUI;
        }

        private void EnsureEventSystem()
        {
            // Sahnede EventSystem yoksa oluştur
            if (EventSystem.current == null)
            {
                GameObject eventSystemGO = new GameObject("[EventSystem]");
                eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }
        }
    }
}

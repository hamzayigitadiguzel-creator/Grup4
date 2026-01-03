using System;
using UnityEngine;
using Storia.Camera;
using Storia.Core.Interfaces;
using Storia.Core.Ship;

namespace Storia.Interaction
{
    /// <summary>
    /// 3D dünyada yerleştirilmiş bir etkileşim noktası.
    /// Player proximity trigger ile yaklaştığında aktif olur.
    /// Etkileşime geçildiğinde world-space UI gösterir ve kamerayı kilitler.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class InteractionPoint : MonoBehaviour, IInteractable
    {
        [Header("Etkileşim Ayarları")]
        [Tooltip("Etkileşim noktasının UI paneli")]
        [SerializeField] private GameObject _uiPanel;
        [Tooltip("Etkileşim yarıçapı")]
        [SerializeField] private float _interactionRadius = 2f;
        
        [Header("Gemi Gereksinimi")]
        [SerializeField] 
        [Tooltip("Etkileşim için limanda bekleyen gemi gerekli mi?")]
        private bool _requiresShipAtCrane = true;

        [Header("Debug")]
        [Tooltip("Gizmos çizilsin mi?")]
        [SerializeField] private bool _drawGizmos = false;

        private bool _isPlayerInRange;
        private bool _isInteracting;
        private IInteractionHost _player;
        private IShipService _shipService;

        public bool IsActive => _isInteracting;
        public event Action<bool> OnInteractionStateChanged;

        /// <summary>
        /// Gemi hizmeti bağımlılığı ile başlat.
        /// Kompozisyon kökü (Day01PrototypeController) tarafından çağrılır.
        /// </summary>
        public void Initialize(IShipService shipService)
        {
            _shipService = shipService;
        }

        private void Awake()
        {
            // UI paneli başlangıçta kapalı
            if (_uiPanel != null)
            {
                _uiPanel.SetActive(false);
                UnityEngine.Debug.Log($"[InteractionPoint] UI Panel başarıyla ayarlandı: {_uiPanel.name}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[InteractionPoint] {gameObject.name} üzerinde UI Panel referansı eksik!");
            }

            // Trigger collider olarak ayarla
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                UnityEngine.Debug.LogError($"[InteractionPoint] {gameObject.name} üzerinde Collider bulunamadı!");
                return;
            }
            col.isTrigger = true;
            UnityEngine.Debug.Log($"[InteractionPoint] {gameObject.name} trigger olarak ayarlandı");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_player != null) 
            {
                UnityEngine.Debug.LogWarning($"[InteractionPoint] Player zaten kaydedilmiş!");
                return;
            }

            var player = other.GetComponent<IInteractionHost>();
            if (player != null)
            {
                _player = player;
                _isPlayerInRange = true;

                // Player'a bu InteractionPoint'i register et
                player.RegisterInteractionPoint(this);
                UnityEngine.Debug.Log($"[InteractionPoint] Player yakına girdi: {other.gameObject.name}");
            }
            else
            {
                UnityEngine.Debug.Log($"[InteractionPoint] {other.gameObject.name} IInteractionHost değil");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<IInteractionHost>();
            if (player != null && player == _player)
            {
                _isPlayerInRange = false;

                if (_isInteracting)
                    EndInteraction();

                // Player'dan unregister et
                player.UnregisterInteractionPoint(this);
                _player = null;
                UnityEngine.Debug.Log($"[InteractionPoint] Player uzaklaştı: {other.gameObject.name}");
            }
        }

        public void OnInteract()
        {
            // Toggle interaction: E tuşuna tekrar basınca kapat
            if (_isInteracting)
            {
                EndInteraction();
                return;
            }

            // Gemi kontrolü gerekiyorsa, limanda bekleyen gemi var mı kontrol et
            if (_requiresShipAtCrane)
            {
                if (!CanInteractWithShip())
                {
                    UnityEngine.Debug.Log("[InteractionPoint] Limanda bekleyen gemi yok, etkileşim iptal edildi.");
                    return;
                }
            }

            // Sadece player yakındaysa etkileşime geç
            if (_isPlayerInRange)
            {
                UnityEngine.Debug.Log($"[InteractionPoint] Etkileşim başladı: {gameObject.name}");
                StartInteraction();
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[InteractionPoint] Player yakınında değil! (isPlayerInRange={_isPlayerInRange}, hasPlayer={_player != null})");
            }
        }

        /// <summary>
        /// Limanda bekleyen gemi var mı kontrol et (AtCrane state).
        /// </summary>
        private bool CanInteractWithShip()
        {
            if (_shipService == null)
                return false;

            var currentShip = _shipService.CurrentShip;
            if (currentShip == null)
                return false;

            // Gemi vinç konumunda mı?
            return currentShip.State == ShipState.AtCrane || currentShip.State == ShipState.ProcessingContainers;
        }

        /// <summary>
        /// Dış sistemler tarafından UI'ı zorla kapat (gemi ayrıldığında).
        /// </summary>
        public void ForceCloseInteraction()
        {
            if (_isInteracting)
            {
                EndInteraction();
            }
        }

        public void OnInteractionEnd()
        {
            if (!_isInteracting) return;
            EndInteraction();
        }

        private void StartInteraction()
        {
            _isInteracting = true;

            // UI paneli aç
            if (_uiPanel != null)
                _uiPanel.SetActive(true);

            // Player kontrollerini kilitle
            if (_player != null)
                _player.SetFullLock(true);

            // Cursor'u serbest bırak (UI etkileşimi için)
            CameraLockManager.Instance.UnlockCursor();

            OnInteractionStateChanged?.Invoke(true);
        }

        private void EndInteraction()
        {
            _isInteracting = false;

            // UI paneli kapat
            if (_uiPanel != null)
                _uiPanel.SetActive(false);

            // Player kontrollerini serbest bırak
            if (_player != null)
                _player.SetFullLock(false);

            // Cursor'u kilitle (FPS modu için)
            CameraLockManager.Instance.LockCursor();

            OnInteractionStateChanged?.Invoke(false);
        }

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            Gizmos.color = _isInteracting ? Color.green : (_isPlayerInRange ? Color.yellow : Color.cyan);
            Gizmos.DrawWireSphere(transform.position, _interactionRadius);
        }
    }
}

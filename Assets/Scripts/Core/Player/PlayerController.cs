using UnityEngine;
using UnityEngine.InputSystem;
using Storia.Interaction;
using Storia.Core.Interfaces;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Storia.Player
{
    /// <summary>
    /// FPS karakter hareketi ve kamera kontrolü.
    /// Input System kullanarak WASD + mouse look sağlar.
    /// IInteractionHost implementasyonu ile etkileşim noktalarını yönetir.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour, IInteractionHost
    {
        [Header("Hareket")]
        [Tooltip("Hareket hızı (metre/saniye).")]
        [SerializeField] private float _moveSpeed = 5f;
        [Tooltip("Yerçekimi kuvveti (metre/saniye²).")]
        [SerializeField] private float _gravity = -9.81f;
        [Tooltip("Zıplama başlangıç hızı (metre/saniye).")]
        [SerializeField] private float _jumpForce = 5f;
        [Tooltip("Koşu hız çarpanı (1.5 = %50 hız artışı).")]
        [Range(1.1f, 3f)]
        [SerializeField] private float _sprintSpeedMultiplier = 1.5f;
        [Tooltip("Koşu modu: false = basılı tutma, true = toggle (bas-koş, dur-sıfırla).")]
        [SerializeField] private bool _sprintToggleMode = true;

        [Header("Kamera")]
        [Tooltip("Kamera transform referansı.")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Fare hassasiyeti (0.1-5 arasında).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _lookSensitivity = 1f;
        [Tooltip("Maksimum bakış açısı (derece cinsinden).")]
        [Range(30f, 90f)]
        [SerializeField] private float _maxLookAngle = 80f;

        [Header("Debug Bilgileri (Salt Okunur)")]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [Tooltip("Geçerli hareket hızı (metre/saniye).")]
        [SerializeField] private float _currentSpeed;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [Tooltip("Geçerli hareket yönü (normalize).")]
        [SerializeField] private Vector3 _currentDirection;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [Tooltip("Yerde mi?")]
        [SerializeField] private bool _isGrounded;
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        [Tooltip("Koşuyor mu?")]
        [SerializeField] private bool _isSprintingDebug;

        private CharacterController _characterController;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private float _verticalVelocity;
        private float _cameraPitch;

        private bool _isMovementLocked;
        private bool _isCameraLocked;
        private bool _isSprintActive;
        private IInteractable _activeInteractionPoint;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (_characterController == null)
            {
                Debug.LogError("[PlayerController] CharacterController component eksik!");
                enabled = false;
                return;
            }

            if (_cameraTransform == null)
            {
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    _cameraTransform = mainCam.transform;
                }
                else
                {
                    Debug.LogError("[PlayerController] Kamera bulunamadı! PlayerController için kamera referansı gerekli.");
                    enabled = false;
                    return;
                }
            }
        }

        private void OnEnable()
        {
            // PlayerInput component SendMessages davranışıyla çalışır
            // OnMove, OnLook, OnInteract metodları otomatik çağrılır

            Storia.Camera.CameraLockManager.Instance.LockCursor();
        }

        private void Update()
        {
            HandleMovement();
            HandleCameraRotation();
        }

        #region Hareket

        private void HandleMovement()
        {
            if (_isMovementLocked || _characterController == null)
            {
                // Hareket kilitliyse debug bilgilerini sıfırla
                _currentSpeed = 0f;
                _currentDirection = Vector3.zero;
                _isGrounded = _characterController?.isGrounded ?? false;
                _isSprintingDebug = false;
                return;
            }

            // Yatay hareket
            Vector3 moveDir = transform.right * _moveInput.x + transform.forward * _moveInput.y;

            // Toggle mode aktifse ve oyuncu durmuşsa sprint'ı iptal et
            if (_sprintToggleMode && _isSprintActive && moveDir.sqrMagnitude < 0.01f)
            {
                _isSprintActive = false;
                Debug.Log("[PlayerController] Koşu iptal edildi (duruldu).");
            }

            // Koşu hızını uygula
            float currentSpeed = _isSprintActive ? _moveSpeed * _sprintSpeedMultiplier : _moveSpeed;
            Vector3 movement = moveDir * currentSpeed;

            // Yerçekimi ve zıplama
            if (_characterController.isGrounded)
            {
                // Yerde olduğu zaman hızı sıfırla (düşüşü kontrol et)
                if (_verticalVelocity < 0)
                    _verticalVelocity = -2f;
            }
            else
            {
                // Havada olduğu zaman yerçekimini uygula
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            movement.y = _verticalVelocity;

            _characterController.Move(movement * Time.deltaTime);

            // Debug bilgilerini güncelle
            UpdateDebugInfo(movement);

            // Debug bilgilerini güncelle
            UpdateDebugInfo(movement);
        }

        private void UpdateDebugInfo(Vector3 movement)
        {
            // Mevcut hız (sadece yatay)
            Vector3 horizontalVelocity = new Vector3(movement.x, 0f, movement.z);
            _currentSpeed = horizontalVelocity.magnitude / Time.deltaTime;

            // Hareket yönü (normalize)
            _currentDirection = horizontalVelocity.magnitude > 0.01f
                ? horizontalVelocity.normalized
                : Vector3.zero;

            // Yer durumu
            _isGrounded = _characterController.isGrounded;

            // Sprint durumu
            _isSprintingDebug = _isSprintActive;
        }

        #endregion

        #region Kamera

        private void HandleCameraRotation()
        {
            if (_isCameraLocked || _cameraTransform == null)
                return;

            // Yatay rotasyon (Y-axis)
            transform.Rotate(Vector3.up, _lookInput.x * _lookSensitivity);

            // Dikey rotasyon (X-axis pitch)
            _cameraPitch -= _lookInput.y * _lookSensitivity;
            _cameraPitch = Mathf.Clamp(_cameraPitch, -_maxLookAngle, _maxLookAngle);

            _cameraTransform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        #endregion

        #region Giriş Callback'leri (Yeni Input Sistemi)

        // Bu metodlar Input System ve PlayerInput component tarafından kullanılır
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
        }

        public void OnLook(InputValue value)
        {
            _lookInput = value.Get<Vector2>();
        }

        public void OnInteract(InputValue value)
        {
            // E tuşu basıldığında etkileşim noktası varsa OnInteract çağır
            if (value.isPressed)
            {
                if (_activeInteractionPoint != null)
                {
                    Debug.Log($"[PlayerController] E tuşu basıldı, etkileşim noktasına gönderiliyor: {_activeInteractionPoint}");
                    _activeInteractionPoint.OnInteract();
                }
                else
                {
                    Debug.LogWarning("[PlayerController] E tuşu basıldı ama aktif etkileşim noktası yok!");
                }
            }
        }

        public void OnJump(InputValue value)
        {
            // Boşluk tuşu basıldığında ve yerde olduğu zaman zıpla
            if (value.isPressed && _characterController != null && _characterController.isGrounded && !_isMovementLocked)
            {
                _verticalVelocity = _jumpForce;
                Debug.Log("[PlayerController] Zıpladı.");
            }
        }

        public void OnSprint(InputValue value)
        {
            if (_isMovementLocked)
                return;

            if (_sprintToggleMode)
            {
                // Toggle Mode: Sadece tuş basıldığında (ilk basış) sprint durumunu değiştir
                if (value.isPressed)
                {
                    _isSprintActive = !_isSprintActive;
                    if (_isSprintActive)
                        Debug.Log($"[PlayerController] Koşu açık (toggle mode, {(_sprintSpeedMultiplier - 1f) * 100f:F0}% hız artışı).");
                    else
                        Debug.Log("[PlayerController] Koşu kapalı (toggle mode).");
                }
            }
            else
            {
                // Hold Mode: Tuşa basılı tutulduğu sürece sprint aktif
                _isSprintActive = value.isPressed;
                if (_isSprintActive)
                    Debug.Log($"[PlayerController] Koşu başladı (hold mode, {(_sprintSpeedMultiplier - 1f) * 100f:F0}% hız artışı).");
                else
                    Debug.Log("[PlayerController] Koşu bitti (hold mode).");
            }
        }

        #endregion

        #region Kilitle/Kilidi Aç

        /// <summary>
        /// Hareketi kilitle/aç.
        /// </summary>
        public void SetMovementLock(bool locked)
        {
            _isMovementLocked = locked;
            if (locked)
                _moveInput = Vector2.zero;
        }

        /// <summary>
        /// Kamera kontrolünü kilitle/aç.
        /// </summary>
        public void SetCameraLock(bool locked)
        {
            _isCameraLocked = locked;
            if (locked)
                _lookInput = Vector2.zero;
        }

        /// <summary>
        /// Hem hareketi hem kamerayı kilitle/aç.
        /// </summary>
        public void SetFullLock(bool locked)
        {
            SetMovementLock(locked);
            SetCameraLock(locked);
        }

        #endregion

        #region Etkileişim Noktası Kaydı

        /// <summary>
        /// IInteractionHost implementasyonu: InteractionPoint player proximity'e girdiğinde çağırır.
        /// </summary>
        /// <param name="point">Kaydedilecek etkileşim noktası</param>
        public void RegisterInteractionPoint(IInteractable point)
        {
            _activeInteractionPoint = point;
            UnityEngine.Debug.Log($"[PlayerController] InteractionPoint kaydedildi: {point}");
        }

        /// <summary>
        /// IInteractionHost implementasyonu: InteractionPoint player proximity'den çıktığında çağırır.
        /// </summary>
        /// <param name="point">Kaydedilenden çıkarılacak etkileşim noktası</param>
        public void UnregisterInteractionPoint(IInteractable point)
        {
            if (_activeInteractionPoint == point)
            {
                _activeInteractionPoint = null;
                UnityEngine.Debug.Log($"[PlayerController] InteractionPoint kaldırıldı: {point}");
            }
        }

        #endregion
    }
}

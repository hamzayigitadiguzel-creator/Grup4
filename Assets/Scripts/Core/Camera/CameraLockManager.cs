using UnityEngine;

namespace Storia.Camera
{
    /// <summary>
    /// Cursor ve kamera kilit durumunu merkezi olarak yönetir.
    /// UI interaction sırasında cursor visible + unlocked,
    /// 3D FPS sırasında cursor invisible + locked.
    /// Thread-safe singleton implementation.
    /// </summary>
    public sealed class CameraLockManager : MonoBehaviour
    {
        private static CameraLockManager _instance;
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Singleton instance. Thread-safe lazy initialization kullanır.
        /// </summary>
        public static CameraLockManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("[CameraLockManager]");
                            _instance = go.AddComponent<CameraLockManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Başlangıçta FPS modunda
            LockCursor();
        }

        /// <summary>
        /// FPS modu: cursor kilitleniyor ve gizleniyor.
        /// </summary>
        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// UI modu: cursor serbest ve görünür.
        /// </summary>
        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}

using Storia.Interaction;

namespace Storia.Core.Interfaces
{
    /// <summary>
    /// Interactable noktaları kayıt edebilen player kontratı.
    /// PlayerController ve InteractionPoint arasındaki bağımlılığı azaltır.
    /// </summary>
    public interface IInteractionHost
    {
        /// <summary>
        /// Bir etkileşim noktasını kaydeder (player proximity'e girdiğinde).
        /// </summary>
        /// <param name="interactionPoint">Kayıt edilecek etkileşim noktası</param>
        void RegisterInteractionPoint(IInteractable interactionPoint);

        /// <summary>
        /// Bir etkileşim noktasını kayıttan çıkarır (player proximity'den çıktığında).
        /// </summary>
        /// <param name="interactionPoint">Kayıttan çıkarılacak etkileşim noktası</param>
        void UnregisterInteractionPoint(IInteractable interactionPoint);

        /// <summary>
        /// Hareketi kilitle/aç.
        /// </summary>
        void SetMovementLock(bool locked);

        /// <summary>
        /// Kamera kontrolünü kilitle/aç.
        /// </summary>
        void SetCameraLock(bool locked);

        /// <summary>
        /// Hem hareketi hem kamerayı kilitle/aç.
        /// </summary>
        void SetFullLock(bool locked);
    }
}

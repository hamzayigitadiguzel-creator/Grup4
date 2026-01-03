using System;

namespace Storia.Interaction
{
    /// <summary>
    /// Etkileşime girebilir obje interface.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// Etkileşim başladığında tetiklenir.
        /// </summary>
        void OnInteract();

        /// <summary>
        /// Etkileşim sona erdiğinde tetiklenir.
        /// </summary>
        void OnInteractionEnd();

        /// <summary>
        /// Etkileşim noktasının aktif olup olmadığı.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Etkileşim durumu değiştiğinde tetiklenir (başladı/bitti).
        /// </summary>
        event Action<bool> OnInteractionStateChanged;
    }
}

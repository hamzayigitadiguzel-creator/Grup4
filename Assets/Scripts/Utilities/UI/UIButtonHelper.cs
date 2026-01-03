using UnityEngine.UI;
using System;

namespace Storia.UI.Utilities
{
    /// <summary>
    /// UI Button binding işlemlerini kolaylaştıran utility method'ları.
    /// RemoveAllListeners + AddListener pattern'ını standardize eder.
    /// </summary>
    public static class UIButtonHelper
    {
        /// <summary>
        /// Bir button'a callback bağla. Önceki listener'lar temizlenir.
        /// </summary>
        public static void BindButton(Button button, Action callback)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }

        /// <summary>
        /// Bir button'a parametre alan callback bağla. Önceki listener'lar temizlenir.
        /// </summary>
        public static void BindButton<T>(Button button, Action<T> callback, T parameter)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke(parameter));
        }

        /// <summary>
        /// Birden fazla button'a aynı callback'i bağla.
        /// </summary>
        public static void BindButtons(Action callback, params Button[] buttons)
        {
            foreach (var button in buttons)
            {
                BindButton(button, callback);
            }
        }

        /// <summary>
        /// Birden fazla button'a farklı callback'ler bağla.
        /// </summary>
        public static void BindButtons(params (Button, Action)[] bindings)
        {
            foreach (var (button, callback) in bindings)
            {
                BindButton(button, callback);
            }
        }
    }
}

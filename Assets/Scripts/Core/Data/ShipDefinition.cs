using UnityEngine;
using Storia.Data.Generated;

namespace Storia.Core.Data
{
    /// <summary>
    /// Gemi konfigürasyonu. Her gemi için temel bilgiler.
    /// ShipData'dan dönüştürülür (ShipInstance uyumluluğu için geçici köprü).
    /// </summary>
    [System.Serializable]
    public class ShipDefinition
    {
        [SerializeField] public string shipId;
        [SerializeField] public string shipName;
        [SerializeField] public string originPort;
        
        /// <summary>
        /// Bu gemi kaç konteynır taşıyacak.
        /// Değişken olabilir: aynı gemi birden fazla seed'de farklı konteynır sayısıyla spawn olabilir.
        /// </summary>
        [SerializeField] public int containerCount;
        
        /// <summary>
        /// Yolculuk süresi (saat cinsinden).
        /// Bir referans verisi; oyuncunun gemi bilgilerinde göreceği değer.
        /// </summary>
        [SerializeField] public float voyageDurationHours;
    }
}

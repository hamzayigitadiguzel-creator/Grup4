namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi durumu enum'ı.
    /// Gemi hareketini ve etkileşimi yönetmek için gerekli state'ler.
    /// </summary>
    public enum ShipState
    {
        /// <summary>Limandan uzakta deniz'de</summary>
        AtSea,
        
        /// <summary>Deniz'den limana doğru yaklaşıyor</summary>
        Approaching,
        
        /// <summary>Vinç konumunun tam önünde bekleme (oyuncunun etkileşim alabileceği nokta)</summary>
        AtCrane,
        
        /// <summary>Oyuncu tarafından kabul edilmiş, konteyner kararı aşaması</summary>
        ProcessingContainers,
        
        /// <summary>Kabul edilen tüm konteynerlar yüklendi/kararlaştırıldı, geri dönüş hazır</summary>
        Departing,
        
        /// <summary>Reddedilmiş veya zaman dolmuş, gemi limandan ayrılıyor</summary>
        Rejected,
        
        /// <summary>Gemi tamamen ortadan kayboldu, artık sistem'de değil</summary>
        Gone
    }
}

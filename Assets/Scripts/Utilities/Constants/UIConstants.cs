namespace Storia.Constants
{
    /// <summary>
    /// UI ile ilgili sabit değerler.
    /// UI text'leri, format string'leri ve lokalizasyon key'leri.
    /// </summary>
    public static class UIConstants
    {
        // Manifest/Label başlıkları
        public const string ManifestHeader = "[Manifest]";
        public const string LabelHeader = "[Etiket]";

        // Field format'ları
        public const string ContainerIdFormat = "ID: {0}";
        public const string OriginPortFormat = "Menşei: {0}";
        public const string CargoLabelFormat = "Yük: {0}";

        // Container başlığı
        public const string ContainerTitleFormat = "Konteyner: {0}";

        // End screen text'leri
        public const string DayEndedTitle = "Gün Bitti";
        public const string TotalDecisionsFormat = "Toplam: {0}";
        public const string CorrectDecisionsFormat = "Doğru: {0}";
        public const string WrongDecisionsFormat = "Yanlış: {0}";
        public const string CorrectPlacementFormat = "Yerleşim Doğru: {0}";
        public const string WrongPlacementFormat = "Yerleşim Yanlış: {0}";
        public const string DevSeedFormat = "\n\n[DEV] Seed: {0}";

        // HUD text'leri
        public const string TimeDisplayFormat = "Saat: {0}";

        // Ship panel text'leri
        public const string ShipIdFormat = "ID: {0}";
        public const string ShipOriginFormat = "Kaynak: {0}";
        public const string ShipContainerFormat = "Konteyner: {0}";
        public const string ShipVoyageFormat = "Seyahat Süresi: {0:F1} saat";

        // Dev mode text'leri
        public const string DevModeHeader = "<color=#FFD700>[DEV MODE]</color>";
        public const string DevGameTimeFormat = "Oyun Süresi: {0}";
        public const string DevRealTimeFormat = "<color=#00A0FF>Gerçek Zaman: {0}</color>";
        public const string DevElapsedFormat = "Geçen Süre: {0:F1}s / {1:F1}s kalan";
    }
}

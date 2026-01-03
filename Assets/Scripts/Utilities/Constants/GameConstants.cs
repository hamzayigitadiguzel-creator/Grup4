namespace Storia.Constants
{
    /// <summary>
    /// Proje genelinde kullanılan sabit değerler.
    /// </summary>
    public static class GameConstants
    {
        // Zone isimleri
        public const string ZoneA = "Zone A";
        public const string ZoneB = "Zone B";
        public const string ZoneC = "Zone C";

        // Buffer boyutları
        public const int DecisionLogInitialCapacity = 64;
        public const int TaskIdListInitialCapacity = 16;
        public const int StringBuilderSmallCapacity = 128;
        public const int StringBuilderMediumCapacity = 256;
        public const int StringBuilderLargeCapacity = 1024;

        // RNG varsayılan değerleri
        public const uint DefaultRngSeed = 0xA3C59AC3u;
        public const int FallbackSeed = 12345;

        // String mutasyon limitleri
        public const int MaxMutationAttempts = 10;
        public const int MaxDigitMutationAttempts = 10;

        // Cargo Hold Grid Parametreleri
        public const int CargoGridRows = 3;
        public const int CargoGridColumns = 4;
        public const float CargoGridCellSize = 4.5f;

        // Seed Hash Multiplier
        public const int SeedHashMultiplier = 486187739;

        // Float hassasiyet değerleri
        public const float MantissaPrecision = 16777216f; // 2^24
        public const uint MantissaMask = 0x00FFFFFFu;
    }
}

namespace Storia.Diagnostics
{
    /// <summary>
    /// Geliştirici modu için rastgele tohum (seed) override sistemi.
    /// </summary>
    public static class DevRunSeedOverride
    {
        // ========== Public Accessors (Read-only, Runtime davranışını değiştirmez) ==========
        /// <summary>Override var mı?</summary>
        public static bool HasOverride { get; private set; }
        /// <summary>Override edilen seed değeri</summary>
        public static int OverrideSeed { get; private set; }

        // ========== Public Methods ==========
        /// <summary>Override'i ayarla</summary>
        public static void SetOverride(int seed)
        {
            HasOverride = true;
            OverrideSeed = seed;
        }
        /// <summary>Override'i temizle</summary>
        public static void Clear()
        {
            HasOverride = false;
            OverrideSeed = 0;
        }
    }
}

namespace Storia.Core.Interfaces
{
    /// <summary>
    /// Spawner pattern kontratı.
    /// GameObject lifecycle yönetimini standardize eder.
    /// </summary>
    /// <typeparam name="TDefinition">Spawn edilecek objenin definition tipi</typeparam>
    /// <typeparam name="TController">Spawn edilen objenin controller tipi</typeparam>
    public interface ISpawner<TDefinition, TController>
    {
        /// <summary>
        /// Yeni bir obje spawn eder.
        /// </summary>
        TController Spawn(TDefinition definition);

        /// <summary>
        /// Mevcut objeyi yok eder.
        /// </summary>
        void DestroyCurrent();

        /// <summary>
        /// Tüm objeleri temizler.
        /// </summary>
        void ClearAll();

        /// <summary>
        /// Mevcut aktif objeyi döndürür.
        /// </summary>
        TController GetCurrent();
    }
}

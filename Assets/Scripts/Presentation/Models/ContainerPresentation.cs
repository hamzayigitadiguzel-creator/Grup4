/// <summary>
/// Konteyner'in sunulan (gösterilen) bilgilerini tutmak için veri yapısı.
/// 
/// Temel konsept:
/// - truth: Konteyner'in GERÇEK bilgileri (manifest ve label'ında aynı olmalı)
/// - shown: Oyuncuya gösterilen bilgiler (manifest veya label'da hatalı olabilir)
/// 
/// Örnek: 
/// - Gerçek ID: "TRBU-1001"
/// - Manifest'te gösterilen: "TRBU-1002" (hata!)
/// - Label'da gösterilen: "TRBU-1001" (doğru)
/// → IdMismatch çelişkisi oluşur
/// </summary>
public sealed class ContainerPresentation
{
    /// <summary>
    /// Konteyner manifest'inde gösterilen bilgiler.
    /// (Dijital form / belge - hatalı olabilir)
    /// </summary>
    public ContainerFields manifestShown;
    
    /// <summary>
    /// Konteyner fiziksel etiketinde (label) gösterilen bilgiler.
    /// (Konteyner üstündeki fiziksel etiket - hatalı olabilir)
    /// </summary>
    public ContainerFields labelShown;

    /// <summary>
    /// Manifest ve label arasında ne tür bir çelişki var?
    /// None = çelişki yok (sağlıklı konteyner)
    /// IdMismatch = ID'de farklılık var
    /// OriginMismatch = Origin port'ta farklılık var
    /// CargoMismatch = Cargo type'ında farklılık var
    /// </summary>
    public PresentationConflict conflict;
}

/// <summary>
/// Manifest ile label arasındaki çelişki türü.
/// Oyuncuya iki farklı bilgi verildiğinde ne tür hata var?
/// </summary>
public enum PresentationConflict
{
    /// <summary>Çelişki yok - manifest ve label uyumlu</summary>
    None,
    
    /// <summary>Konteyner ID'sinde hata var (manifest vs label)</summary>
    IdMismatch,
    
    /// <summary>Origin port'unda hata var (manifest vs label)</summary>
    OriginMismatch,
    
    /// <summary>Cargo type'ında hata var (manifest vs label)</summary>
    CargoMismatch
}
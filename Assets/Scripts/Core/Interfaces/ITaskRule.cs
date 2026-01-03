using System.Collections.Generic;

/// <summary>
/// Task rule interface - konteyner'in "hedef" olup olmadığını belirler.
/// 
/// Task Rule = Hangi konteynerler KABUL EDİLMESİ GEREKİYOR?
/// 
/// Implementasyon örnekleri:
/// - SimplIdListTaskRule: Sadece belirtilen ID'ler hedef
/// - CompositeRule: AND/OR kombinasyonları ile kompleks kurallar
/// 
/// Kullanım:
/// 1. Initialize: ContainerGenerator sonrası, hedef konteynerler'i işaretle
/// 2. Decision: Oyuncu konteyner kabul ettiğinde, rule'a sorarak doğru mu yanlış mı?
/// 3. UI: GetShortDescription() ile oyuncuya talimat göster
/// </summary>
public interface ITaskRule
{
    /// <summary>
    /// Bu konteyner hedef mi? (Kabul edilmesi gerekiyor mu?)
    /// </summary>
    /// <param name="truth">Konteyner'in gerçek verisi (truth)</param>
    /// <returns>True = hedef (kabul edilmeli), False = hedef değil (reddet)</returns>
    bool IsTarget(in ContainerFields truth);
    
    /// <summary>
    /// UI/oyuncu için kısa, okunabilir açıklama.
    /// Örnek: "ID=TRBU-1001 OR ID=TRBU-1002 OR ID=TRBU-1003"
    /// Veya: "(Cargo=Elektronik AND Origin=Pire) OR Cargo=Gıda"
    /// </summary>
    /// <returns>Açıklamalı metin</returns>
    string GetShortDescription();

    /// <summary>
    /// Eğer bu rule hedef ID'lerini listeleyebiliyorsa, listeyi döndür.
    /// Basit "ID OR ID OR ID" formatında olmalı (composite AND/OR varsa başarısız).
    /// 
    /// Kullanım: UI'de hedef konteynerler'i önceden vurgulama (opsiyonel)
    /// </summary>
    /// <param name="output">Doldurulacak hedef ID listesi</param>
    /// <returns>True = başarılı (output dolduruldu), False = yapılamadı</returns>
    bool TryGetTargetIds(List<string> output);
}

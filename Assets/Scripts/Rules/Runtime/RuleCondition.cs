namespace Storia.Rules.Runtime
{
    /// <summary>
    /// Kural koşulu türleri - konteyner hangi alan'ına göre kontrol edilir?
    /// </summary>
    public enum RuleConditionType
    {
        /// <summary>Konteyner ID'sine göre kontrol et (örn: "TRBU-1001")</summary>
        ContainerId,
        
        /// <summary>Köken limanına göre kontrol et (örn: "Pire")</summary>
        OriginPort,
        
        /// <summary>Kargo türüne göre kontrol et (örn: "Elektronik")</summary>
        CargoType
    }

    /// <summary>
    /// Composite rule operatörü - koşullar nasıl birleştirilir?
    /// </summary>
    public enum LogicalOperator
    {
        /// <summary>AND: Tüm koşullar DOĞRU olmalı (herkes match etmeli)</summary>
        And,
        
        /// <summary>OR: En az BİR koşul DOĞRU olmalı (herhangi biri match etmeli)</summary>
        Or
    }

    /// <summary>
    /// Tek bir kural koşulu (composite tree'de leaf node).
    /// 
    /// Örnekler:
    /// - "ContainerId == TRBU-1042"
    /// - "OriginPort == Pire"
    /// - "CargoType == Elektronik"
    /// 
    /// Kullanım:
    /// - CompositeRule'a eklenip, AND/OR ile birleştirilir
    /// - Matches() ile ContainerFields'a karşı test edilir
    /// </summary>
    public sealed class RuleCondition
    {
        /// <summary>Hangi alanda kontrol yapılacak? (ID, Origin, Cargo)</summary>
        public RuleConditionType type;
        
        /// <summary>Eşleşme değeri (örn: "TRBU-1001" veya "Pire")</summary>
        public string value;

        /// <summary>
        /// Koşul oluştur.
        /// </summary>
        /// <param name="type">Koşul tipi (ContainerId, OriginPort, CargoType)</param>
        /// <param name="value">Eşleşme değeri</param>
        public RuleCondition(RuleConditionType type, string value)
        {
            this.type = type;
            this.value = value;
        }

        /// <summary>
        /// Bu koşul verilen ContainerFields'a uyuyor mu?
        /// Algoritma: type'a göre switch, ilgili field'ı value ile karşılaştır.
        /// </summary>
        /// <param name="fields">Kontrol edilecek konteyner verisi</param>
        /// <returns>True = koşul match etti, False = match etmedi</returns>
        public bool Matches(in ContainerFields fields)
        {
            switch (type)
            {
                case RuleConditionType.ContainerId:
                    // Konteyner ID'si = value mi?
                    return fields.containerId == value;
                
                case RuleConditionType.OriginPort:
                    // Origin port = value mi?
                    return fields.originPort == value;
                
                case RuleConditionType.CargoType:
                    // Cargo type = value mi?
                    return fields.cargoLabel == value;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// UI/logging için okunabilir format.
        /// Örn: "ContainerId=TRBU-1001"
        /// </summary>
        public override string ToString()
        {
            return $"{type}={value}";
        }
    }
}

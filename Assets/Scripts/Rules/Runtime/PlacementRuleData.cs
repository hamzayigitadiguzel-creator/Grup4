using System.Collections.Generic;

namespace Storia.Rules.Runtime
{
    /// <summary>
    /// Placement rule için runtime mapping sistemi.
    /// 
    /// Amaç: Konteynerler hangi zone'a yerleştirilmeli?
    /// 
    /// Rule tipi'ne göre mapping:
    /// - Cargo-based: Cargo Type → ZoneId (örn: "Elektronik" → Zone 1)
    /// - ID-based: Container ID → ZoneId (örn: "TRBU-1001" → Zone 3)
    /// - Origin-based: Origin Port → ZoneId (örn: "Pire" → Zone 2)
    /// 
    /// Kullanım:
    /// 1. PlacementRuleGenerator mapping'ler ekler (AddMapping)
    /// 2. DecisionManager placement'i doğrula (TryGetExpectedZoneId)
    /// 3. UI talimatları göster (BuildUiInstruction)
    /// </summary>
    public sealed class PlacementRuleData
    {
        /// <summary>Key → ZoneId mapping (örn: "Elektronik" → 1)</summary>
        private readonly Dictionary<string, int> _mapping;
        
        /// <summary>Mapping key tipi (Cargo, ID, Origin)</summary>
        private readonly RuleConditionType _keyType;
        
        /// <summary>ZoneId → Zone Name mapping (UI display için)</summary>
        private readonly Dictionary<int, string> _zoneIdToNameMap;

        /// <summary>
        /// Placement rule oluştur.
        /// </summary>
        /// <param name="keyType">Hangi alanı key olarak kullanacağız? (Cargo, ID, Origin)</param>
        public PlacementRuleData(RuleConditionType keyType)
        {
            _keyType = keyType;
            _mapping = new Dictionary<string, int>();
            _zoneIdToNameMap = new Dictionary<int, string>();
        }

        /// <summary>
        /// Mapping ekle (key → zoneId).
        /// Örn: "Elektronik" → Zone ID 1
        /// </summary>
        /// <param name="key">Key değer (Cargo type, Container ID veya Origin port)</param>
        /// <param name="zoneId">Hedef zone ID'si</param>
        /// <param name="zoneNameForDisplay">UI'de gösterilecek zone adı</param>
        public void AddMapping(string key, int zoneId, string zoneNameForDisplay)
        {
            if (!string.IsNullOrEmpty(key))
            {
                // Mapping'i ekle: key → zoneId
                _mapping[key] = zoneId;
                
                // Zone name'i ekle (ilk kez ekleniyorsa)
                if (!_zoneIdToNameMap.ContainsKey(zoneId))
                    _zoneIdToNameMap[zoneId] = zoneNameForDisplay;
            }
        }

        /// <summary>
        /// Konteyner için beklenen zone ID'yi bul.
        /// Algoritma:
        /// 1. ContainerFields'dan key'i çıkar (type'a göre Cargo/ID/Origin)
        /// 2. Mapping'de key'i ara
        /// 3. ZoneId döndür
        /// </summary>
        /// <param name="truth">Konteyner'in gerçek verisi</param>
        /// <param name="expectedZoneId">Bulunan zone ID (çıktı)</param>
        /// <returns>True = mapping bulundu, False = mapping yok</returns>
        public bool TryGetExpectedZoneId(in ContainerFields truth, out int expectedZoneId)
        {
            expectedZoneId = 0;

            // Type'a göre uygun field'ı çıkar (key olarak)
            string key = GetKeyFromFields(in truth);
            if (string.IsNullOrEmpty(key))
                return false;

            // Mapping'de key'i ara
            return _mapping.TryGetValue(key, out expectedZoneId);
        }

        /// <summary>
        /// ContainerFields'dan uygun key'i çıkar (type'a göre).
        /// </summary>
        /// <param name="fields">Konteyner verisi</param>
        /// <returns>Key değeri (Cargo type, ID veya Origin port)</returns>
        private string GetKeyFromFields(in ContainerFields fields)
        {
            switch (_keyType)
            {
                case RuleConditionType.ContainerId:
                    return fields.containerId;
                
                case RuleConditionType.OriginPort:
                    return fields.originPort;
                
                case RuleConditionType.CargoType:
                    return fields.cargoLabel;
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Placement rule'un kısa açıklaması (logging için).
        /// Örn: "CargoType→ZoneId (5 mappings)"
        /// </summary>
        public string GetShortDescription()
        {
            return $"{_keyType}→ZoneId ({_mapping.Count} mappings)";
        }

        /// <summary>
        /// UI'de gösterilecek detaylı placement talimatları.
        /// Format: "Yerleştirme Talimatı (Cargo):\n- Elektronik → Zone A\n- Gıda → Zone B"
        /// </summary>
        public string BuildUiInstruction()
        {
            // Mapping yoksa default mesaj
            if (_mapping.Count == 0)
                return "Yerleştirme Talimatı: (Tanımlı değil)";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Yerleştirme Talimatı ({_keyType}):");

            // Her mapping'i listele
            foreach (var kvp in _mapping)
            {
                // Zone name'i bul, yoksa "Zone {ID}" format'ı kullan
                string zoneName = _zoneIdToNameMap.ContainsKey(kvp.Value) ? _zoneIdToNameMap[kvp.Value] : $"Zone {kvp.Value}";
                sb.AppendLine($"- {kvp.Key} → {zoneName}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Zone ID'den display name al (logging ve UI için).
        /// </summary>
        /// <param name="zoneId">Zone ID</param>
        /// <returns>Zone adı (örn: "Zone A" veya "Zone 1")</returns>
        public string GetZoneNameForDisplay(int zoneId)
        {
            return _zoneIdToNameMap.ContainsKey(zoneId) ? _zoneIdToNameMap[zoneId] : $"Zone {zoneId}";
        }
    }
}

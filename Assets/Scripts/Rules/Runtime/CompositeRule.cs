using System.Collections.Generic;
using System.Text;

namespace Storia.Rules.Runtime
{
    /// <summary>
    /// AND/OR operatörleri ile birleştirilebilen composite rule.
    /// 
    /// Amaç: Basit ID listelerinden kompleks AND/OR kombinasyonlarına kadar çeşitli task rule'ları desteklemek.
    /// 
    /// Örnekler:
    /// - Basit: "ID=TRBU-1042 OR ID=TRBU-1043 OR ID=TRBU-1044"
    /// - Kompleks: "(Cargo=Elektronik AND Origin=Pire) OR (Cargo=Gıda AND Origin=İstanbul) OR ID=TRBU-1000"
    /// 
    /// Yapı:
    /// - _operator: AND veya OR
    /// - _conditions: Leaf node'lar (basit koşullar)
    /// - _subRules: İç içe composite rule'lar (nested logic)
    /// 
    /// Örnek ağaç:
    /// ```
    /// CompositeRule(OR)
    ///   ├─ Condition: ID=TRBU-1001
    ///   ├─ Condition: ID=TRBU-1002
    ///   └─ SubRule(AND)
    ///      ├─ Condition: Cargo=Elektronik
    ///      └─ Condition: Origin=Pire
    /// ```
    /// </summary>
    public sealed class CompositeRule : ITaskRule
    {
        /// <summary>Bu rule'un operatörü (AND veya OR)</summary>
        private readonly LogicalOperator _operator;
        
        /// <summary>Bu level'deki basit koşullar (leaf node'lar)</summary>
        private readonly List<RuleCondition> _conditions;
        
        /// <summary>Bu level'deki alt composite rule'lar (nested logic)</summary>
        private readonly List<CompositeRule> _subRules;

        /// <summary>
        /// Composite rule oluştur.
        /// </summary>
        /// <param name="op">Operatör (AND veya OR) - bu level'in koşullarını nasıl birleştireceğiz?</param>
        public CompositeRule(LogicalOperator op)
        {
            _operator = op;
            _conditions = new List<RuleCondition>();
            _subRules = new List<CompositeRule>();
        }

        /// <summary>
        /// Basit koşul ekle (leaf node).
        /// Örn: "ID=TRBU-1001" veya "Cargo=Elektronik"
        /// </summary>
        /// <param name="condition">Eklenecek koşul</param>
        public void AddCondition(RuleCondition condition)
        {
            if (condition != null)
                _conditions.Add(condition);
        }

        /// <summary>
        /// Alt composite rule ekle (nested logic).
        /// Örn: (AND grubu) veya (OR grubu)
        /// </summary>
        /// <param name="subRule">Eklenecek alt rule</param>
        public void AddSubRule(CompositeRule subRule)
        {
            if (subRule != null)
                _subRules.Add(subRule);
        }

        /// <summary>
        /// Bu rule konteyner'in hedef olup olmadığını kontrol et.
        /// Algoritma:
        /// - AND: Tüm conditions VE subRules true dönmeli (&&)
        /// - OR: En az bir condition VEYA subRule true dönmeli (||)
        /// </summary>
        /// <param name="truth">Konteyner'in gerçek verisi</param>
        /// <returns>True = hedef, False = hedef değil</returns>
        public bool IsTarget(in ContainerFields truth)
        {
            if (_operator == LogicalOperator.And)
            {
                // AND: Tüm conditions match etmeli
                foreach (var condition in _conditions)
                {
                    if (!condition.Matches(in truth))
                        return false;  // Bir condition farz etmedi, AND başarısız
                }

                // AND: Tüm subRules de match etmeli
                foreach (var subRule in _subRules)
                {
                    if (!subRule.IsTarget(in truth))
                        return false;  // Bir subRule farz etmedi, AND başarısız
                }

                // En az bir koşul olmalı (boş rule = hata)
                return _conditions.Count > 0 || _subRules.Count > 0;
            }
            else // OR
            {
                // OR: En az bir condition match etmeli
                foreach (var condition in _conditions)
                {
                    if (condition.Matches(in truth))
                        return true;  // Bir condition sağlandı, OR başarılı
                }

                // OR: Veya en az bir subRule match etmeli
                foreach (var subRule in _subRules)
                {
                    if (subRule.IsTarget(in truth))
                        return true;  // Bir subRule sağlandı, OR başarılı
                }

                // Hiçbiri sağlanmadı
                return false;
            }
        }

        /// <summary>
        /// UI/oyuncu için okunabilir açıklama.
        /// Örn: "ID=TRBU-1001 VEYA ID=TRBU-1002"
        /// </summary>
        public string GetShortDescription()
        {
            return BuildDescription();
        }

        /// <summary>
        /// Eğer bu rule basit "ID OR ID OR ID" formatındaysa, ID listesini döndür.
        /// Nested AND/OR varsa, yapılamaz (false dönüş).
        /// </summary>
        /// <param name="output">Doldurulacak ID listesi</param>
        /// <returns>True = sadece ID listesi varsa, False = yoksa</returns>
        public bool TryGetTargetIds(List<string> output)
        {
            // Composite rule'dan ID listesi çıkarmak karmaşık (nested AND/OR'lar yüzünden)
            // Sadece basit "ID OR ID OR ID" durumu için çalışır
            
            // Eğer OR değilse veya subRule varsa, yapılamaz
            if (_operator != LogicalOperator.Or || _subRules.Count > 0)
                return false;

            // Tüm condition'lar ContainerId tipi olmalı
            bool hasOnlyIdConditions = true;
            foreach (var condition in _conditions)
            {
                if (condition.type != RuleConditionType.ContainerId)
                {
                    hasOnlyIdConditions = false;
                    break;
                }
            }

            // ID listesi boş olamaz
            if (!hasOnlyIdConditions || _conditions.Count == 0)
                return false;

            // Output listesine tüm ID'leri ekle
            foreach (var condition in _conditions)
            {
                output.Add(condition.value);
            }

            return true;
        }

        /// <summary>
        /// UI için okunabilir açıklama oluştur (recursive).
        /// AND: Parantez içinde "VE" ile birleştir
        /// OR: "VEYA" ile birleştir (parantez yok)
        /// 
        /// Örn:
        /// - "ID=TRBU-1001 VEYA ID=TRBU-1002 VEYA ID=TRBU-1003"
        /// - "(Cargo=Elektronik VE Origin=Pire) VEYA Cargo=Gıda"
        /// </summary>
        private string BuildDescription()
        {
            StringBuilder sb = new StringBuilder();

            // AND grupları parantez ile vurgula
            if (_operator == LogicalOperator.And)
                sb.Append("(");

            // Tüm condition'ları operatör ile birleştir
            for (int i = 0; i < _conditions.Count; i++)
            {
                if (i > 0)
                    sb.Append(_operator == LogicalOperator.And ? " VE " : " VEYA ");

                sb.Append(_conditions[i].ToString());
            }

            // Tüm subRule'ları operatör ile birleştir
            for (int i = 0; i < _subRules.Count; i++)
            {
                if (_conditions.Count > 0 || i > 0)
                    sb.Append(_operator == LogicalOperator.And ? " VE " : " VEYA ");

                // Alt rule'ın açıklamasını (parantezler ile) ekle
                sb.Append(_subRules[i].BuildDescription());
            }

            // AND grupları kapat
            if (_operator == LogicalOperator.And)
                sb.Append(")");

            return sb.ToString();
        }

        /// <summary>
        /// ToString override - BuildDescription() ile aynı.
        /// </summary>
        public override string ToString()
        {
            return BuildDescription();
        }
    }
}

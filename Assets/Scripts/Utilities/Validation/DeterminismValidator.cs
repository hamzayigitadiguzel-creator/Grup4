using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Storia.Data.Generated;
using Storia.Managers.Decision;
using Storia.Generators;

namespace Storia.Utilities.Validation
{
    /// <summary>
    /// Determinizm doğrulama için hash hesaplama utility'si.
    /// 
    /// Sorumlulukları:
    /// 1. Container, gemi, hedef, yerleştirme verilerinden hash hesaplama
    /// 2. Aynı seed ile farklı run'larda aynı hash'lerin üretildiğini doğrulama
    /// 3. SHA256 hash algoritması kullanarak 16-karakterlik hex string döndürme
    /// 4. RNG determinizm ve output determinizm'ini doğrulama
    /// 
    /// Kullanım:
    /// - Day01PrototypeController.StartDay()'de hash'ler hesaplanır
    /// - QA test'lerde: aynı seed = aynı hash = determinizm sağlanmış
    /// - Hash'ler konsola yazılır (EditorDebug log dosyasında kaydedilir)
    /// 
    /// Hash türleri:
    /// - ContainerHash: Container ID sırası (procedural generation output)
    /// - ShipHash: Gemi adı sırası (generation output)
    /// - TargetHash: Hedef container ID'leri (task rule generation output)
    /// - PlacementHash: Zone assignment'lar (placement rule generation output)
    /// - PipelineHash: Tüm aşamaların kombinasyonu (tam pipeline determinizm)
    /// </summary>
    public static class DeterminismValidator
    {
        /// <summary>
        /// Container ID listesinden SHA256 hash üretir.
        /// 
        /// Aynı seed ile üretilen container'lar aynı sıraya ve ID'ye sahip olmalı.
        /// Hash değişirse: container generation determinizmi bozulmuş.
        /// </summary>
        /// <param name="containers">ContainerGenerator tarafından üretilen container listesi</param>
        /// <returns>16-karakterlik hex hash (örn: "a1b2c3d4e5f6g7h8")</returns>
        public static string ComputeContainerHash(List<ContainerData> containers)
        {
            if (containers == null || containers.Count == 0)
                return "BOŞ";

            // Container ID'lerini sırası korunarak birleştir
            var idList = string.Join(",", containers.Select(c => c.truth.containerId));
            return ComputeSHA256Hash(idList);
        }

        /// <summary>
        /// Gemi adı listesinden SHA256 hash üretir.
        /// 
        /// Aynı seed ile üretilen gemiler aynı sıraya ve adlara sahip olmalı.
        /// Hash değişirse: ship generation determinizmi bozulmuş.
        /// </summary>
        /// <param name="ships">ShipGenerator tarafından üretilen gemi listesi</param>
        /// <returns>16-karakterlik hex hash</returns>
        public static string ComputeShipHash(List<ShipData> ships)
        {
            if (ships == null || ships.Count == 0)
                return "BOŞ";

            // Gemi adlarını sırası korunarak birleştir
            var shipNames = string.Join(",", ships.Select(s => s.shipName));
            return ComputeSHA256Hash(shipNames);
        }

        /// <summary>
        /// DecisionManager'daki decision log'dan hash üretir.
        /// 
        /// Aynı seed + aynı oyuncu kararları → aynı log → aynı hash.
        /// Hash değişirse: decision logging veya decision order değişmiş.
        /// </summary>
        /// <param name="manager">Oyunun decision history'si tutan manager</param>
        /// <returns>16-karakterlik hex hash, ya da hata durumunda "NULL_MANAGER", "BOŞ_LOG"</returns>
        public static string ComputeDecisionLogHash(DecisionManager manager)
        {
            if (manager == null)
                return "NULL_MANAGER";

            int logCount = manager.GetDecisionLogCount();
            if (logCount == 0)
                return "BOŞ_LOG";

            // Decision log'un string reprezentasyonundan hash üret
            // (Seed parametresi burada 0 - sadece log içeriği hash'lenir, seed değil)
            string logText = manager.BuildEndOfDayLogText(0);
            return ComputeSHA256Hash(logText);
        }

        /// <summary>
        /// Task rule tarafından işaretlenen hedef container ID'lerinden hash üretir.
        /// 
        /// Aynı seed ile seçilen hedefler aynı olmalı.
        /// Hash değişirse: task rule selection determinizmi bozulmuş.
        /// Not: ID'ler sıralı şekilde hash'lenir (sıra bağımsız karşılaştırma için).
        /// </summary>
        /// <param name="containers">ContainerGenerator tarafından üretilen container listesi (isTarget flag'ı kontrol edilir)</param>
        /// <returns>16-karakterlik hex hash, ya da "HEDEF_YOK" if hiç hedef yoksa</returns>
        public static string ComputeTargetContainerHash(List<ContainerData> containers)
        {
            if (containers == null || containers.Count == 0)
                return "BOŞ";

            // Hedef olan container ID'lerini sırayla filtrele ve alfabetik sırala
            var targetIds = containers
                .Where(c => c.isTarget)
                .Select(c => c.truth.containerId)
                .OrderBy(id => id); // Sıralama: set comparison için

            var targetList = string.Join(",", targetIds);
            return targetList.Length > 0 ? ComputeSHA256Hash(targetList) : "HEDEF_YOK";
        }

        /// <summary>
        /// Placement rule tarafından atanan zone assignment'larından hash üretir.
        /// 
        /// Aynı seed ile placement'lar aynı container→zone eşleştirmesi olmalı.
        /// Hash değişirse: placement rule assignment determinizmi bozulmuş.
        /// Not: Pair'ler sıralı şekilde hash'lenir (sıra bağımsız karşılaştırma için).
        /// </summary>
        /// <param name="containers">PlacementRuleGenerator tarafından expectedZoneId ile doldurulmuş containerlar</param>
        /// <returns>16-karakterlik hex hash, ya da "YERLEŞTIRME_YOK" if hiç yerleştirme yoksa</returns>
        public static string ComputePlacementHash(List<ContainerData> containers)
        {
            if (containers == null || containers.Count == 0)
                return "BOŞ";

            // Container:Zone pair'lerini filtrele ve sırayla birleştir
            var placementPairs = containers
                .Where(c => c.expectedZoneId != 0)
                .Select(c => $"{c.truth.containerId}:{c.expectedZoneId}")
                .OrderBy(pair => pair); // Sıralama: set comparison için

            var placementList = string.Join(",", placementPairs);
            return placementList.Length > 0 ? ComputeSHA256Hash(placementList) : "YERLEŞTIRME_YOK";
        }

        /// <summary>
        /// Tüm generation pipeline'ının kombinasyon hash'ı.
        /// 
        /// Sorumluluk:
        /// 1. Container output determinizmi (ContainerHash)
        /// 2. Ship output determinizmi (ShipHash)
        /// 3. Task rule output determinizmi (TargetHash)
        /// 4. Placement rule output determinizmi (PlacementHash)
        /// 5. RNG ilerleme kontrolü (checkpoints)
        /// 
        /// Aynı seed → aynı pipeline hash → sistem deterministik.
        /// Hash değişirse: herhangi bir generator veya RNG'de değişim vardır.
        /// </summary>
        /// <param name="result">Tüm generator'lardan output'lar ve RNG checkpoint'leri içeren result</param>
        /// <returns>16-karakterlik hex hash, ya da "NULL_RESULT" if result null'sa</returns>
        public static string ComputePipelineHash(GenerationResult result)
        {
            if (result == null)
                return "NULL_RESULT";

            // Tüm hash'leri ve checkpoint'leri birleştir (| ayırıcısı ile)
            string data = string.Join("|",
                ComputeContainerHash(result.Containers),
                ComputeShipHash(result.Ships),
                ComputeTargetContainerHash(result.Containers),
                ComputePlacementHash(result.Containers),
                result.RngCallCountCheckpoints != null 
                    ? string.Join(",", result.RngCallCountCheckpoints)
                    : "KONTROL_NOKTASI_YOK"
            );
            
            return ComputeSHA256Hash(data);
        }

        /// <summary>
        /// SHA256 hash hesaplar ve ilk 8 byte'ını hex string'e döner.
        /// 
        /// Algoritma:
        /// 1. Input string'i UTF-8 byte'larına çevir
        /// 2. SHA256 hash hesapla (32 byte)
        /// 3. İlk 8 byte'ı (64 bit) hex string'e çevir (16 karakter)
        /// 4. Determinizm doğrulama için yeterli (hash collision'lar nadirdir)
        /// 
        /// Kısaltma sebebi: console output okunabilirliği (full hash = 64 karakter)
        /// </summary>
        /// <param name="input">Hash'lenecek metni (örn: "TRBU-0001,TRBU-0002,...")</param>
        /// <returns>16-karakterlik hex string (örn: "a1b2c3d4e5f6g7h8"), ya da "BOŞ_GIRDI" if input null/empty</returns>
        private static string ComputeSHA256Hash(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "BOŞ_GIRDI";

            using (SHA256 sha256 = SHA256.Create())
            {
                // String'i UTF-8 byte'larına çevir
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                
                // SHA256 hash'ini hesapla (32 byte)
                byte[] hashBytes = sha256.ComputeHash(bytes);

                // İlk 8 byte'ı hex string'e çevir (16 karakter)
                StringBuilder sb = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}

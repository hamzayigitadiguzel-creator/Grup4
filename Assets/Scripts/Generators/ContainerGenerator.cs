using System.Collections.Generic;
using Storia.Data.Generated;
using Storia.Generation;

namespace Storia.Generators
{
    /// <summary>
    /// Seed ve config'e göre konteyner listesi üretir.
    /// </summary>
    public static class ContainerGenerator
    {
        /// <summary>
        /// Konteyner listesi üret.
        /// Derleme zamanında uygulanan ardışık düzen sıralaması için yazılan sonucu döndürür.
        /// </summary>
        /// <param name="rng">Deterministik RNG</param>
        /// <param name="config">Üretim konfigürasyonu</param>
        /// <param name="pools">Havuz verileri</param>
        /// <returns>Konteynerler ve RNG kontrol noktası içeren yazılmış sonuç</returns>
        public static GenerationOrchestrator.ContainersGeneratedResult Generate(
            DeterministicRng rng,
            GenerationConfig config,
            PoolsConfig pools)
        {
            if (rng == null || config == null || pools == null)
                throw new System.ArgumentNullException("RNG, Config ve Pools null olamaz");

            int containerCount = config.CalculateContainerCount(rng);
            List<ContainerData> containers = new List<ContainerData>(containerCount);

            // Benzersiz ID'ler için set
            HashSet<string> usedIds = new HashSet<string>();

            for (int i = 0; i < containerCount; i++)
            {
                // Benzersiz ID üret
                string containerId = GenerateUniqueId(rng, pools, usedIds);
                usedIds.Add(containerId);

                // Origin ve cargo rastgele seç
                string originPort = pools.GetRandomOriginPort(rng);
                string cargoLabel = pools.GetRandomCargoType(rng);

                ContainerData container = ContainerData.Create(
                    containerId,
                    originPort,
                    cargoLabel,
                    isTarget: false, // Task generator tarafından belirlenecek
                    expectedZoneId: 0 // Placement generator tarafından belirlenecek
                );

                containers.Add(container);
            }

            return new GenerationOrchestrator.ContainersGeneratedResult(containers, rng);
        }

        /// <summary>
        /// Benzersiz konteyner ID üret.
        /// </summary>
        private static string GenerateUniqueId(
            DeterministicRng rng,
            PoolsConfig pools,
            HashSet<string> usedIds)
        {
            const int maxAttempts = 100;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                string id = pools.GenerateContainerId(rng);
                
                if (!usedIds.Contains(id))
                    return id;

                attempts++;
            }

            // Fallback: zaman damgası tabanlı benzersiz kimlik
            return $"FALLBACK-{usedIds.Count:D4}";
        }
    }
}

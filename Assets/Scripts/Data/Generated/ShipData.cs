using System.Collections.Generic;

namespace Storia.Data.Generated
{
    /// <summary>
    /// Prosedürel olarak üretilen gemi verisi.
    /// </summary>
    public sealed class ShipData
    {
        public string shipId;
        public string shipName;
        public string originPort;
        public float voyageDurationHours;

        /// <summary>
        /// Bu gemiye atanan konteynerler (runtime'da doldurulur).
        /// </summary>
        public List<ContainerData> containers;

        public int ContainerCount => containers?.Count ?? 0;

        public ShipData(string shipId, string shipName, string originPort, float voyageDuration)
        {
            this.shipId = shipId;
            this.shipName = shipName;
            this.originPort = originPort;
            this.voyageDurationHours = voyageDuration;
            this.containers = new List<ContainerData>();
        }

        /// <summary>
        /// Gemiye konteyner ata.
        /// </summary>
        public void AddContainer(ContainerData container)
        {
            if (containers == null)
                containers = new List<ContainerData>();

            containers.Add(container);
        }

        /// <summary>
        /// Gemiye birden fazla konteyner ata.
        /// </summary>
        public void AddContainers(IEnumerable<ContainerData> containerList)
        {
            if (containers == null)
                containers = new List<ContainerData>();

            containers.AddRange(containerList);
        }

        public override string ToString()
        {
            return $"Ship[{shipId}, {shipName}, {originPort}] Containers={ContainerCount}";
        }
    }
}

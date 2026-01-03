namespace Storia.Data.Generated
{
    /// <summary>
    /// Prosedürel olarak üretilen konteyner verisi.
    /// </summary>
    public sealed class ContainerData
    {
        /// <summary>
        /// Ground truth - gerçek konteyner bilgileri.
        /// </summary>
        public ContainerFields truth;

        /// <summary>
        /// Bu konteyner görev listesinde hedef mi? (Generator tarafından belirlenir)
        /// </summary>
        public bool isTarget;

        /// <summary>
        /// Placement rule için beklenen ZoneId (varsa, 0 = no expected zone).
        /// </summary>
        public int expectedZoneId;

        public ContainerData(ContainerFields truth, bool isTarget = false, int expectedZoneId = 0)
        {
            this.truth = truth;
            this.isTarget = isTarget;
            this.expectedZoneId = expectedZoneId;
        }

        /// <summary>
        /// Hızlı oluşturucu.
        /// </summary>
        public static ContainerData Create(string containerId, string originPort, string cargoLabel, 
            bool isTarget = false, int expectedZoneId = 0)
        {
            return new ContainerData(
                new ContainerFields
                {
                    containerId = containerId,
                    originPort = originPort,
                    cargoLabel = cargoLabel
                },
                isTarget,
                expectedZoneId
            );
        }

        public override string ToString()
        {
            return $"Container[{truth.containerId}, {truth.originPort}, {truth.cargoLabel}] Target={isTarget}";
        }
    }
}

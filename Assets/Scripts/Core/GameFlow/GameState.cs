namespace Storia.Core.GameFlow
{
    /// <summary>
    /// Oyunun mevcut ana durumunu temsil eden enum.
    /// 
    /// State Flow:
    /// DayInitialization → AwaitingShip → ShipArrival → ShipDecision → 
    /// ContainerEvaluation → PlacementSelection → AwaitingShip (loop) → DayEnd
    /// </summary>
    public enum GameState
    {
        /// <summary>
        /// Gün başlatılıyor (initialization, generation, UI setup).
        /// </summary>
        DayInitialization,

        /// <summary>
        /// Sonraki gemi bekleniyor (idle state).
        /// </summary>
        AwaitingShip,

        /// <summary>
        /// Gemi limana yaklaşıyor/geldi (ship spawn, arrival animation).
        /// </summary>
        ShipArrival,

        /// <summary>
        /// Gemi kabul/red kararı bekleniyor (ship panel açık).
        /// </summary>
        ShipDecision,

        /// <summary>
        /// Konteyner değerlendirmesi yapılıyor (container panel açık, accept/reject).
        /// </summary>
        ContainerEvaluation,

        /// <summary>
        /// Placement zone seçimi bekleniyor (placement panel açık).
        /// </summary>
        PlacementSelection,

        /// <summary>
        /// Gün sona erdi, sonuçlar gösteriliyor.
        /// </summary>
        DayEnd
    }
}

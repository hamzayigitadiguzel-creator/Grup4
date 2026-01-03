using System.Collections.Generic;
using Storia.Data.Generated;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Gemi yaşam döngüsü yönetimi için hizmet arayüzü.
    /// Yalnızca harici sistemler tarafından gerekli olan işlemleri gösterir.
    /// Tüketicileri ShipManager singleton uygulamasından ayırır.
    /// </summary>
    public interface IShipService
    {
        // Olaylar
        event System.Action<ShipInstance> OnShipArrived;
        event System.Action OnShipMovementCompleted;

        // Komutlar
        void InitializeShipsForDay(List<ShipData> generatedShips);
        void MakeShipDecision(ShipInstance ship, bool accepted);
        void AutoResolveOnTimeUp();
        ShipInstance GetNextShip();

        // Sorgular
        ShipInstance CurrentShip { get; }
        ShipMovement GetCurrentShipMovement();
    }
}

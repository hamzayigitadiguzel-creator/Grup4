using UnityEngine;
using Storia.Core.Data;
using Storia.Data.Generated;
using System.Collections.Generic;

namespace Storia.Core.Ship
{
    /// <summary>
    /// Runtime gemi örneği. ShipDefinition'dan türetilen, state ve decision bilgilerini taşıyan nesne.
    /// </summary>
    public class ShipInstance
    {
        public ShipDefinition Definition { get; private set; }
        public ShipState State { get; set; }
        
        /// <summary>Oyuncu tarafından gemi kararı verildi mi?</summary>
        public bool ShipDecisionMade { get; set; } = false;
        
        /// <summary>Oyuncu gemiyi kabul etti mi? (ShipDecisionMade = true ise bu anlamlı)</summary>
        public bool ShipAccepted { get; set; } = false;
        
        /// <summary>Bu gemiye ait konteyner listesi.</summary>
        public List<ContainerData> Containers { get; private set; }
        
        /// <summary>Bu gemideki toplam konteyner sayısı.</summary>
        public int ContainerCount => Containers?.Count ?? 0;
        
        /// <summary>Bu gemi için kaç tane konteyner henüz karar bekleniyor?</summary>
        public int PendingContainerDecisions { get; set; }

        public ShipInstance(ShipDefinition definition, List<ContainerData> containers)
        {
            Definition = definition;
            Containers = containers ?? new List<ContainerData>();
            State = ShipState.AtSea;
            PendingContainerDecisions = ContainerCount;
        }

        /// <summary>
        /// Konteyner kararı işlendiğinde (kabul/red), pending sayısını azalt.
        /// </summary>
        public void DecreaseContainerPendingCount()
        {
            PendingContainerDecisions = Mathf.Max(0, PendingContainerDecisions - 1);
        }

        /// <summary>
        /// Bu geminin tüm kararları tamamlandı mı?
        /// </summary>
        public bool IsFullyProcessed()
        {
            return ShipDecisionMade && PendingContainerDecisions == 0;
        }
    }
}

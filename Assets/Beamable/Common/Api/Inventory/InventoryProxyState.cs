using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Inventory
{
    [Serializable]
    public class InventoryProxyState
    {
        public Dictionary<string, long> currencies;
        public Dictionary<string, List<ItemProxy>> items;
    }
}
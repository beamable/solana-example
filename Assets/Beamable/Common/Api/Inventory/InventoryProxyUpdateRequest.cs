using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Inventory
{
    [Serializable]
    public class InventoryProxyUpdateRequest
    {
        public string id;
        public string transaction;
        public Dictionary<string, long> currencies;
        public List<ItemCreateRequest> newItems;
    }
}
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Inventory
{
    [Serializable]
    public class ItemProxy
    {
        public string proxyId;
        public List<ItemProperty> properties;
    }
}
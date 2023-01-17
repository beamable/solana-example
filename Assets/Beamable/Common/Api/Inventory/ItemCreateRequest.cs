using System.Collections.Generic;

namespace Beamable.Common.Api.Inventory
{
    public class ItemCreateRequest
    {
        public string contentId;
        public List<ItemProperty> properties;
    }
}
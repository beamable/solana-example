using System.Collections.Generic;

namespace Beamable.Microservices.SolanaFederation.Models
{
	public class InventoryItem
	{
		public string contentId;
		public List<ItemProperty> properties;
	}

	public class ItemProperty
	{
		public string name;
		public string value;
	}
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	[Serializable]
	public class NftExternalMetadata
	{
		public string name;
		public string symbol;
		public string description;
		public string image;
		public string animation_url;
		public string external_url;
		public IList<MetadataAttribute> attributes;

		public static NftExternalMetadata Generate(Dictionary<string, string> properties, string name)
		{
			var image = properties.GetValueOrDefault("image");
			if (image is not null) properties.Remove("image");

			var symbol = properties.GetValueOrDefault("symbol");
			if (symbol is not null) properties.Remove("symbol");

			var description = properties.GetValueOrDefault("description");
			if (description is not null) properties.Remove("description");

			var animation_url = properties.GetValueOrDefault("animation_url");
			if (animation_url is not null) properties.Remove("animation_url");

			var external_url = properties.GetValueOrDefault("external_url");
			if (external_url is not null) properties.Remove("external_url");

			return new NftExternalMetadata
			{
				name = name,
				image = image,
				symbol = symbol,
				description = description,
				animation_url = animation_url,
				external_url = external_url,
				attributes = properties.Select(p => new MetadataAttribute
				{
					trait_type = p.Key,
					value = p.Value
				}).ToList()
			};
		}
	}

	public class MetadataAttribute
	{
		public string trait_type;
		public string value;
	}
}
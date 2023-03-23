using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	[Serializable]
	public class NftExternalMetadata
	{
		[JsonExtensionData]
		public Dictionary<string, object> SpecialProperties { get; }

		[JsonProperty("attributes")]
		public IList<MetadataAttribute> Attributes { get; }

		public NftExternalMetadata(Dictionary<string, string> properties, string contentId)
		{
			SpecialProperties = new Dictionary<string, object>();
			Attributes = new List<MetadataAttribute>();
			
			foreach (var property in properties)
				if (property.Key.StartsWith("$"))
					SpecialProperties.Add(property.Key.TrimStart('$'), property.Value);
				else
					Attributes.Add(new MetadataAttribute(property.Key, property.Value));
		}
		
		public Dictionary<string, string> GetProperties()
		{
			var properties = new Dictionary<string, string>();

			foreach (var data in SpecialProperties) properties.Add($"${data.Key}", data.Value.ToString());
			foreach (var attribute in Attributes) properties.Add(attribute.trait_type, attribute.value);

			return properties;
		}
		
		public static class SpecialProperty
		{
			public const string Symbol = "$symbol";
			public const string Image = "$image";
			public const string Description = "$description";
			public const string AnimationUri = "$animation_url";
			public const string ExternalUri = "$external_url";
		}
	}

	public class MetadataAttribute
	{
		public string trait_type;
		public string value;

		public MetadataAttribute(string trait_type, string value)
		{
			this.trait_type = trait_type;
			this.value = value;
		}
	}
}
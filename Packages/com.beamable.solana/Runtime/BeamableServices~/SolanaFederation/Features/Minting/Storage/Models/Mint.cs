using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models
{
	public record Mint
	{
		[BsonElement("_id")] public ObjectId ID { get; set; } = ObjectId.GenerateNewId();
		public string ContentId { get; set; }
		public string PublicKey { get; set; }
	}
}
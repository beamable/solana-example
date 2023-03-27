using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Beamable.Microservices.SolanaFederation.Features.Transaction.Storage.Models
{
	internal class TransactionRecord
	{
		[BsonElement("_id")]
		public string Id { get; set; }

		public DateTime ExpireAt { get; set; } = DateTime.Now.AddDays(1);
	}
}
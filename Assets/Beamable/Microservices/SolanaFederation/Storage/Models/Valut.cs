using MongoDB.Bson;
using Solnet.KeyStore;
using System;

namespace Beamable.Microservices.SolanaFederation.Storage.Models
{
	public record Valut
	{
		private static readonly SecretKeyStoreService KeystoreService = new SecretKeyStoreService();

		public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
		public string Name { get; set; }
		public string Value { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public byte[] DecryptValue()
		{
			return KeystoreService.DecryptKeyStoreFromJson(Configuration.RealmSecret, Value);
		}
	}
}

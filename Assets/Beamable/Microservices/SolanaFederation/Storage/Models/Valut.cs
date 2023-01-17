using MongoDB.Bson;
using Solnet.KeyStore;
using System;

namespace Assets.Beamable.Microservices.SolanaFederation.Storage.Models
{
	public class Valut
	{
		private static SecretKeyStoreService keystoreService = new SecretKeyStoreService();

		public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
		public string Name { get; set; }
		public string Value { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public byte[] DecryptValue()
		{
			return keystoreService.DecryptKeyStoreFromJson(Configuration.RealmSecret, Value);
		}
	}
}

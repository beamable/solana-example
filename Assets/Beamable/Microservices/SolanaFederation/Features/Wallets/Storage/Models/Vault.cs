using System;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Solana.Unity.KeyStore;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Storage.Models
{
	public record Vault
	{
		private static readonly SecretKeyStoreService KeystoreService = new();

		[BsonElement("_id")] public ObjectId ID { get; set; } = ObjectId.GenerateNewId();

		public string Name { get; set; }
		public string Value { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public byte[] DecryptValue()
		{
			return KeystoreService.DecryptKeyStoreFromJson(Configuration.RealmSecret, Value);
		}

		public Wallet ToWallet()
		{
			var decryptedKeystore = DecryptValue();
			var mnemonicString = Encoding.UTF8.GetString(decryptedKeystore);

			var restoredMnemonic = new Mnemonic(mnemonicString);
			return new Wallet(restoredMnemonic);
		}
	}
}
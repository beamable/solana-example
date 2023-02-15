using System;
using System.Text;
using Beamable.Solana.Editor;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Solana.Unity.KeyStore.Model;
using Solana.Unity.KeyStore.Services;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Storage.Models
{
	public record Vault
	{
		private static readonly KeyStoreScryptService KeystoreService = new();

		[BsonElement("_id")] public ObjectId ID { get; set; } = ObjectId.GenerateNewId();

		public string Name { get; set; }
		public KeyStore<ScryptParams> Value { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public byte[] DecryptValue()
		{
			return KeystoreService.DecryptKeyStore(SolanaConfiguration.Instance.RealmSecret, Value);
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
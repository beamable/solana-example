using MongoDB.Bson;
using Solnet.KeyStore;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System;
using System.Text;

namespace Assets.Beamable.Microservices.SolanaFederation.Models
{
	class PersistedWallet
	{
		public ObjectId _id { get; set; } = ObjectId.GenerateNewId();
		public string Name { get; set; }
		public string KeyStore { get; set; }
		public DateTime Created { get; set; } = DateTime.Now;

		public Wallet DecryptWallet()
		{
			var keystoreService = new SecretKeyStoreService();

			var decryptedKeystore = keystoreService.DecryptKeyStoreFromJson(Configuration.RealmSecret, KeyStore);
			var mnemonicString = Encoding.UTF8.GetString(decryptedKeystore);

			var restoredMnemonic = new Mnemonic(mnemonicString);
			return new Wallet(restoredMnemonic);
		}
	}
}

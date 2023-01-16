using Assets.Beamable.Microservices.SolanaFederation.Extensions;
using Assets.Beamable.Microservices.SolanaFederation.Models;
using Beamable.Common;
using MongoDB.Driver;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System.Threading.Tasks;

namespace Assets.Beamable.Microservices.SolanaFederation.Services
{
	public class WalletService
	{
		public static async Task<Wallet> GetRealmWallet(IMongoDatabase db)
		{
			var newMnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
			var newWallet = new Wallet(newMnemonic);
			var newPersistedWallet = newWallet.ToPersistedWallet();

			var collection = db.GetCollection<PersistedWallet>(Storage.Collections.Valut);

			var filter = Builders<PersistedWallet>.Filter.Eq(x => x.Name, Configuration.RealmWalletName);

			var update = Builders<PersistedWallet>.Update
				.SetOnInsert(x => x.Name, newPersistedWallet.Name)
				.SetOnInsert(x => x.Created, newPersistedWallet.Created)
				.SetOnInsert(x => x.KeyStore, newPersistedWallet.KeyStore);

			var wallet = await collection.FindOneAndUpdateAsync(
				filter,
				update,
				new FindOneAndUpdateOptions<PersistedWallet>
				{
					IsUpsert = true,
					ReturnDocument = ReturnDocument.After,
				});

			if (wallet.KeyStore == newPersistedWallet.KeyStore)
			{
				BeamableLogger.Log("Created realm wallet '{RealmWalletName}' {RealmWallet}", Configuration.RealmWalletName, newWallet.Account.PublicKey.Key);
			}

			return wallet.DecryptWallet();
		}
	}
}

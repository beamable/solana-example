using Assets.Beamable.Microservices.SolanaFederation.Extensions;
using Assets.Beamable.Microservices.SolanaFederation.Storage;
using Beamable.Common;
using MongoDB.Driver;
using Solnet.Wallet;
using Solnet.Wallet.Bip39;
using System.Threading.Tasks;

namespace Assets.Beamable.Microservices.SolanaFederation.Services
{
	public class WalletService
	{
		private static Wallet cachedWallet = null;

		public static async ValueTask<Wallet> GetRealmWallet(IMongoDatabase db)
		{
			if (cachedWallet is null)
			{
				cachedWallet = await ComputeRealmWallet(db);
			}
			return cachedWallet;
		}

		private static async Task<Wallet> ComputeRealmWallet(IMongoDatabase db)
		{
			var maybeExistingWallet = await ValutCollection.GetByName(db, Configuration.RealmWalletName);
			if (maybeExistingWallet is not null)
			{
				return maybeExistingWallet.ToWallet();
			}
			else
			{
				BeamableLogger.Log("Can't find a persisted realm wallet. Creating a new wallet...");
				var newMnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
				var newWallet = new Wallet(newMnemonic);
				var newPersistedWallet = newWallet.ToValut();

				var insertSuccessful = await ValutCollection.TryInsert(db, newPersistedWallet);
				if (insertSuccessful)
				{
					BeamableLogger.Log("Created realm wallet '{RealmWalletName}' {RealmWallet}", Configuration.RealmWalletName, newWallet.Account.PublicKey.Key);
					return newWallet;
				}
				else
				{
					BeamableLogger.LogWarning("Wallet already created, fetching again");
					return await ComputeRealmWallet(db);
				}
			}
		}
	}
}

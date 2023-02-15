using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage;
using Beamable.Solana.Editor;
using MongoDB.Driver;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets
{
	public static class WalletService
	{
		private static Wallet _cachedWallet;

		public static async ValueTask<Wallet> GetOrCreateRealmWallet(IMongoDatabase db)
		{
			return _cachedWallet ??= await ComputeRealmWallet(db);
		}

		private static async Task<Wallet> ComputeRealmWallet(IMongoDatabase db)
		{
			var maybeExistingWallet = await VaultCollection.GetByName(db, SolanaConfiguration.Instance.RealmWalletName);
			if (maybeExistingWallet is not null) return maybeExistingWallet.ToWallet();

			BeamableLogger.Log("Can't find a persisted realm wallet. Creating a new wallet...");
			var newMnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
			var newWallet = new Wallet(newMnemonic);
			var newPersistedWallet = newWallet.ToVault();

			var insertSuccessful = await VaultCollection.TryInsert(db, newPersistedWallet);
			if (insertSuccessful)
			{
				BeamableLogger.Log("Created realm wallet {RealmWalletName} {RealmWallet}", SolanaConfiguration.Instance.RealmWalletName,
					newWallet.Account.PublicKey.Key);
				if (SolanaConfiguration.Instance.AirDropAmount > 0) await newWallet.Account.Airdrop(SolanaConfiguration.Instance.AirDropAmount);
				return newWallet;
			}

			BeamableLogger.LogWarning("Wallet already created, fetching again");
			return await ComputeRealmWallet(db);
		}
	}
}
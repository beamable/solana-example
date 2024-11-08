using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets
{
	public static class WalletService
	{
		private const string RealmWalletName = "default-wallet";
		private static Wallet _cachedWallet;

		public static async ValueTask<Wallet> GetOrCreateRealmWallet()
		{
			return _cachedWallet ??= await ComputeRealmWallet();
		}

		private static async Task<Wallet> ComputeRealmWallet()
		{
			var maybeExistingWallet = await VaultCollection.GetByName(RealmWalletName);
			if (maybeExistingWallet is not null) return maybeExistingWallet.ToWallet();

			BeamableLogger.Log("Can't find a persisted realm wallet. Creating a new wallet...");
			var newMnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
			var newWallet = new Wallet(newMnemonic);
			var newPersistedWallet = newWallet.ToVault(RealmWalletName);

			var insertSuccessful = await VaultCollection.TryInsert(newPersistedWallet);
			if (insertSuccessful)
			{
				BeamableLogger.Log("Created realm wallet {RealmWalletName} {RealmWallet}", RealmWalletName,
					newWallet.Account.PublicKey.Key);
				await FundWallet(Configuration.AirDropAmount, newWallet);
				return newWallet;
			}

			BeamableLogger.LogWarning("Wallet already created, fetching again");
			return await ComputeRealmWallet();
		}

		private static async Task FundWallet(int amount, Wallet wallet)
		{
			if (amount > 0)
			{
				try
				{
					await wallet.Account.Airdrop(amount);
				}
				catch (Exception ex)
				{
					BeamableLogger.LogWarning("Airdrop {amount} failed. If you are on a production chain, you should fund your wallet {wallet} manually using a crypto exchange.", amount, wallet.Account.PublicKey.Key);
					BeamableLogger.LogError(ex);
				}
			}
			else
			{
				BeamableLogger.Log("Please fund your wallet {wallet} using a crypto exchange to be able to make transactions (mint tokens).", wallet.Account.PublicKey.Key);
			}
		}
	}
}
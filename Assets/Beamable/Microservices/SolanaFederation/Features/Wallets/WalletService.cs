﻿using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage;
using MongoDB.Driver;
using Solnet.Programs.Utilities;
using Solnet.Rpc;
using Solnet.Wallet.Bip39;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets
{
	public class WalletService
	{
		private static Solnet.Wallet.Wallet _cachedWallet;

		public static async ValueTask<Solnet.Wallet.Wallet> GetRealmWallet(IMongoDatabase db)
		{
			return _cachedWallet ??= await ComputeRealmWallet(db);
		}

		private static async Task<Solnet.Wallet.Wallet> ComputeRealmWallet(IMongoDatabase db)
		{
			var maybeExistingWallet = await ValutCollection.GetByName(db, Configuration.RealmWalletName);
			if (maybeExistingWallet is not null)
			{
				return maybeExistingWallet.ToWallet();
			}

			BeamableLogger.Log("Can't find a persisted realm wallet. Creating a new wallet...");
			var newMnemonic = new Mnemonic(WordList.English, WordCount.TwentyFour);
			var newWallet = new Solnet.Wallet.Wallet(newMnemonic);
			var newPersistedWallet = newWallet.ToValut();

			var insertSuccessful = await ValutCollection.TryInsert(db, newPersistedWallet);
			if (insertSuccessful)
			{
				BeamableLogger.Log("Created realm wallet '{RealmWalletName}' {RealmWallet}", Configuration.RealmWalletName, newWallet.Account.PublicKey.Key);
				if (Configuration.AirDropAmount > 0)
				{
					await newWallet.Account.Airdrop(Configuration.AirDropAmount);
				}
				return newWallet;
			}

			BeamableLogger.LogWarning("Wallet already created, fetching again");
			return await ComputeRealmWallet(db);
		}
	}
}
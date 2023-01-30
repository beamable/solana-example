using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Runtime.Collections;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using Solana.Unity.Metaplex;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Collections
{
	internal static class CollectionService
	{
		private static readonly ConcurrentDictionary<string, Account> CachedCollections = new();

		public static async ValueTask<Account> GetOrCreateCollection(string name, Wallet realmWallet)
		{
			var collectionKey = $"_collection.{name}";

			if (CachedCollections.TryGetValue(collectionKey, out var cachedCollection)) return cachedCollection;

			var collectionAccount = realmWallet.GetAccount(collectionKey);

			var tokenMintInfo = await SolanaRpcClient.GetTokenMintInfoAsync(collectionAccount.PublicKey);

			if (tokenMintInfo is not null)
			{
				CachedCollections.TryAdd(collectionKey, collectionAccount);
			}
			else
			{
				BeamableLogger.Log("{CollectionName} is not minted. Minting collection {TokenAddress}", name,
					collectionAccount.PublicKey.Key);

				// Calculate a program derived metadata
				PublicKey.TryFindProgramAddress(
					new List<byte[]>
					{
						Encoding.UTF8.GetBytes("metadata"),
						MetadataProgram.ProgramIdKey,
						collectionAccount.PublicKey
					},
					MetadataProgram.ProgramIdKey,
					out var metadataAddress,
					out _
				);

				var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
					TokenProgram.MintAccountDataSize
				);

				TransactionManager.AddInstruction(
					SystemProgram.CreateAccount( // Create an account for the collection with lamport balance for rent exemption
						realmWallet.Account,
						collectionAccount.PublicKey,
						minBalanceForExemption,
						TokenProgram.MintAccountDataSize,
						TokenProgram.ProgramIdKey
					));

				TransactionManager.AddInstruction(TokenProgram.InitializeMint( // Initialize mint - make it a token
					collectionAccount.PublicKey,
					0,
					realmWallet.Account.PublicKey,
					realmWallet.Account.PublicKey
				));

				TransactionManager.AddInstruction(
					MetadataProgram.CreateMetadataAccountV3( // Create a metadata account for assigning a "name" to the token
						metadataAddress,
						collectionAccount.PublicKey,
						realmWallet.Account.PublicKey,
						realmWallet.Account.PublicKey,
						realmWallet.Account.PublicKey,
						new MetadataV3
						{
							name = name,
							symbol = "",
							uri = "",
							creators = new List<Creator> { new(realmWallet.Account.PublicKey, 100, true) }
						},
						false,
						true
					));

				TransactionManager.AddSigner(collectionAccount);
				TransactionManager.AddSuccessCallback(_ =>
				{
					CachedCollections.TryAdd(collectionKey, collectionAccount);
					return Task.CompletedTask;
				});
			}

			return collectionAccount;
		}
	}
}
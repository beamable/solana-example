using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using MongoDB.Driver;
using Solana.Unity.Metaplex;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	public class MintingService
	{
		public static async Task<Mint> GetOrCreateMint(IMongoDatabase db, string contentId)
		{
			var maybeExistingMing = await MintCollection.Get(db, contentId);
			if (maybeExistingMing is not null) return maybeExistingMing;

			var realmWallet = await WalletService.GetRealmWallet(db);
			var mintAccount = new Account();
			var mint = mintAccount.ToMint(contentId);

			var collection = await MintCollection.Get(db);

			// We are using a transaction to guard against a race condition - minting the same contentId from multiple instances
			using (var session = await db.Client.StartSessionAsync())
			{
				try
				{
					session.StartTransaction();
					await collection.InsertOneAsync(session, mint);

					// Mint it only if insert was successful
					await CreateMint(contentId, realmWallet.Account, mintAccount);
				}
				catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
				{
					BeamableLogger.LogWarning("Mint for {ContentId} already created by another instance. Retrying fetch.",
						contentId);
					await session.AbortTransactionAsync();
					return await GetOrCreateMint(db, contentId);
				}
				catch
				{
					await session.AbortTransactionAsync();
					throw;
				}

				await session.CommitTransactionAsync();
			}

			return mint;
		}

		private static async Task CreateMint(string contentId, Account owner, Account mintAccount)
		{
			var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
				TokenProgram.MintAccountDataSize
			);

			BeamableLogger.Log("Creating mint {MintAccount} for {ContentId} with {LamportAmount} lamport balance.",
				mintAccount.PublicKey.Key, contentId, minBalanceForExemption);

			var blockHash = await SolanaRpcClient.GetLatestBlockHashAsync();

			// Calculate program derived metadata
			PublicKey.TryFindProgramAddress(
				new List<byte[]> {
					Encoding.UTF8.GetBytes("metadata"),
					MetadataProgram.ProgramIdKey,
					mintAccount.PublicKey
				},
				MetadataProgram.ProgramIdKey,
				out var metadataAddress,
				out _
			);

			var createMintTransaction = new TransactionBuilder()
				.SetFeePayer(owner.PublicKey)
				.SetRecentBlockHash(blockHash)
				.AddInstruction(
					SystemProgram
						.CreateAccount( // Create an account for the mint/token with lamport balance for rent exemption
							owner.PublicKey,
							mintAccount.PublicKey,
							minBalanceForExemption,
							TokenProgram.MintAccountDataSize,
							TokenProgram.ProgramIdKey
						)
				)
				.AddInstruction(
					TokenProgram.InitializeMint( // Initialize mint - make it a token
						mintAccount,
						0,
						owner.PublicKey,
						owner.PublicKey
					)
				)
				.AddInstruction(
					MetadataProgram
						.CreateMetadataAccountV3( // Create a metadata account for assigning a "name" to the token
							metadataAddress,
							mintAccount.PublicKey,
							owner.PublicKey,
							owner.PublicKey,
							owner.PublicKey,
							new MetadataV3 {
								name = contentId,
								symbol = "",
								uri = ""
							},
							false,
							true
						)
				)
				.Build(new List<Account> { owner, mintAccount });

			var response = await SolanaRpcClient.SendTransactionAsync(createMintTransaction);

			BeamableLogger.Log("Successfully created mint {MintAccount} for {ContentId} with transaction {Transaction}",
				mintAccount.PublicKey.Key, contentId, response);
		}
	}
}
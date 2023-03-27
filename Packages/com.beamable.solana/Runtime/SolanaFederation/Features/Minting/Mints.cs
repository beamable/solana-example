using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Collections;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using MongoDB.Driver;
using Solana.Unity.Metaplex;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	public class Mints
	{
		private readonly IMongoDatabase _db;
		private readonly Dictionary<string, Mint> _mintsByContent;
		private readonly Dictionary<string, Mint> _mintsByTokens;
		private readonly Wallet _realmWallet;

		public Mints(Wallet realmWallet, IMongoDatabase db)
		{
			_mintsByContent = new Dictionary<string, Mint>();
			_mintsByTokens = new Dictionary<string, Mint>();
			_realmWallet = realmWallet;
			_db = db;
		}

		private void AddMint(Mint mint)
		{
			_mintsByContent[mint.ContentId] = mint;
			_mintsByTokens[mint.PublicKey] = mint;
		}

		public Mint GetByContent(string contentId)
		{
			return _mintsByContent.GetValueOrDefault(contentId);
		}

		public Mint GetByToken(string token)
		{
			return _mintsByTokens.GetValueOrDefault(token);
		}

		public bool ContainsMint(string mint)
		{
			return _mintsByTokens.ContainsKey(mint);
		}
		
		public async Task LoadPersisted()
		{
			var persistedMints = await MintCollection.GetAll(_db);
			persistedMints.ForEach(AddMint);
		}

		public async Task<List<PlayerTokenInfo>> MintNewCurrency(string id, Dictionary<string, long> currencies,
			Wallet realmWallet, PlayerTokenState playerTokenState)
		{
			var missingCurrencyIds = currencies.Keys
				.Except(_mintsByContent.Keys)
				.ToHashSet();

			// Mint missing tokens
			if (missingCurrencyIds.Any())
			{
				var newMints = new List<Mint>();

				var defaultCollection =
					await CollectionService.GetOrCreateCollection(Configuration.DefaultTokenCollectionName, realmWallet);

				var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
					TokenProgram.MintAccountDataSize
				);

				foreach (var currency in currencies)
					if (missingCurrencyIds.Contains(currency.Key))
					{
						var mintAccount = realmWallet.GetAccount(currency.Key);
						BeamableLogger.Log("{ContentId} is not minted. Minting token {TokenAddress}", currency.Key,
							mintAccount.PublicKey.Key);
						AddNewMintInstructions(realmWallet, mintAccount, minBalanceForExemption, currency.Key, defaultCollection,
							"", "");
						var mint = new Mint { ContentId = currency.Key, PublicKey = mintAccount.PublicKey.Key };
						AddMint(mint);
						newMints.Add(mint);
					}

				if (newMints.Any())
					TransactionManager.AddSuccessCallback(async _ => { await MintCollection.Upsert(_db, newMints); });
			}

			var playerKey = new PublicKey(id);

			// Compute new token transactions
			var newTokens = playerTokenState
				.GetNewCurrencyFromRequest(currencies, this)
				.ToList();
			var newInstructions = newTokens
				.Select(c => c.GetInstructions(playerKey, realmWallet.Account.PublicKey))
				.SelectMany(x => x)
				.ToList();

			TransactionManager.AddInstructions(newInstructions);

			return newTokens;
		}

		private static void AddNewMintInstructions(Wallet realmWallet, Account mintAccount, ulong minBalanceForExemption,
			string contentId, Account defaultCollection, string symbol, string uri)
		{
			// Calculate a program derived metadata
			PublicKey.TryFindProgramAddress(
				new List<byte[]>
				{
					Encoding.UTF8.GetBytes("metadata"),
					MetadataProgram.ProgramIdKey,
					mintAccount.PublicKey
				},
				MetadataProgram.ProgramIdKey,
				out var metadataAddress,
				out _
			);

			TransactionManager.AddInstruction(SystemProgram
				.CreateAccount( // Create an account for the mint/token with lamport balance for rent exemption
					realmWallet.Account,
					mintAccount.PublicKey,
					minBalanceForExemption,
					TokenProgram.MintAccountDataSize,
					TokenProgram.ProgramIdKey
				));

			TransactionManager.AddInstruction(TokenProgram.InitializeMint( // Initialize mint - make it a token
				mintAccount.PublicKey,
				0,
				realmWallet.Account.PublicKey,
				realmWallet.Account.PublicKey
			));

			TransactionManager.AddInstruction(MetadataProgram
				.CreateMetadataAccountV3( // Create a metadata account for assigning a "name" to the token
					metadataAddress,
					mintAccount.PublicKey,
					realmWallet.Account.PublicKey,
					realmWallet.Account.PublicKey,
					realmWallet.Account.PublicKey,
					new MetadataV3
					{
						name = contentId,
						symbol = symbol,
						uri = uri,
						creators = new List<Creator> { new(realmWallet.Account.PublicKey, 100, true) },
						collection = new Collection(defaultCollection)
					},
					false,
					false
				));

			TransactionManager.AddSigner(mintAccount);
		}

		public async Task<List<PlayerTokenInfo>> MintNewItems(List<ItemCreateRequest> newItems, Wallet realmWallet,
			IMongoDatabase db,
			string playerWalletAddress, IBeamableRequester beamableRequester)
		{
			if (!newItems.Any()) return new List<PlayerTokenInfo>();

			var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
				TokenProgram.MintAccountDataSize
			);

			var defaultCollection =
				await CollectionService.GetOrCreateCollection(Configuration.DefaultTokenCollectionName, realmWallet);

			var playerKey = new PublicKey(playerWalletAddress);

			var newMints = new List<Mint>();
			var playerTokens = new List<PlayerTokenInfo>();

			foreach (var newItem in newItems)
			{
				var propertyMap = newItem.properties;
				var mintAccount = new Account();
				BeamableLogger.Log("Minting NFT {TokenAddress} for {ContentId}", mintAccount.PublicKey, newItem.contentId);

				var mintExternalMetadata = new NftExternalMetadata(propertyMap);
				var metadataUri = await NtfExternalMetadataService.SaveMetadata(beamableRequester, mintExternalMetadata);

				AddNewMintInstructions(realmWallet, mintAccount, minBalanceForExemption, newItem.contentId, defaultCollection,
					"",
					metadataUri);

				var playerTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(playerKey, mintAccount);

				BeamableLogger.Log("Adding CreateAssociatedTokenAccount instruction for content {ContentId}, mint {Mint}",
					newItem.contentId, mintAccount.PublicKey.Key);
				TransactionManager.AddInstruction(AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
					realmWallet.Account,
					playerKey,
					mintAccount
				));

				BeamableLogger.Log(
					"Adding MintTo {Amount} instruction for content {ContentId}, mint {Mint}, player wallet {Wallet}", 1,
					newItem.contentId, mintAccount.PublicKey.Key, playerKey.Key);
				TransactionManager.AddInstruction(TokenProgram.MintTo(
					mintAccount,
					playerTokenAccount,
					1,
					realmWallet.Account
				));

				var mint = new Mint
				{
					ContentId = newItem.contentId,
					PublicKey = mintAccount.PublicKey
				};
				newMints.Add(mint);
				AddMint(mint);
				playerTokens.Add(new PlayerTokenInfo
				{
					Amount = 1,
					Mint = mintAccount.PublicKey,
					ContentId = newItem.contentId,
					TokenAccount = playerTokenAccount,
					Properties = propertyMap
				});
			}

			if (newMints.Any())
				TransactionManager.AddSuccessCallback(async _ => { await MintCollection.Upsert(_db, newMints); });

			return playerTokens;
		}
	}
}
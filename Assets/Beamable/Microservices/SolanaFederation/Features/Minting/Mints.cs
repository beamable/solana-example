using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Collections;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
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

		private void AddMints(IEnumerable<Mint> mints)
		{
			mints.ToList().ForEach(AddMint);
		}

		public Mint GetByContent(string contentId)
		{
			return _mintsByContent.GetValueOrDefault(contentId);
		}

		public Mint GetByToken(string token)
		{
			return _mintsByTokens.GetValueOrDefault(token);
		}

		public bool ContainsContent(string contentId)
		{
			return _mintsByContent.ContainsKey(contentId);
		}

		public bool ContainsMint(string mint)
		{
			return _mintsByTokens.ContainsKey(mint);
		}

		public async Task<IList<Mint>> LoadMissing(IList<string> contentIds)
		{
			var missingContentIds = contentIds
				.Where(c => !_mintsByContent.ContainsKey(c))
				.ToList();

			var missingMints = missingContentIds
				.Select(contentId => new Mint
					{ ContentId = contentId, PublicKey = _realmWallet.GetAccount(contentId).PublicKey.Key })
				.ToList();

			if (missingMints.Any())
			{
				AddMints(missingMints);
				await MintCollection.Upsert(_db, missingMints);
			}

			return missingMints;
		}

		public async Task LoadPersisted()
		{
			var persistedMints = await MintCollection.GetAll(_db);
			persistedMints.ForEach(AddMint);
		}
		
		public async ValueTask EnsureMinted(IList<string> contentIds, Wallet realmWallet)
		{
			var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
				TokenProgram.MintAccountDataSize
			);

			var defaultCollection = await CollectionService.GetOrCreateCollection(Configuration.DefaultTokenCollectionName, realmWallet);

			foreach (var contentId in contentIds)
			{
				var mintAccount = realmWallet.GetAccount(contentId);
				var tokenMintInfo = await SolanaRpcClient.GetTokenMintInfoAsync(mintAccount.PublicKey);

				if (tokenMintInfo is null)
				{
					BeamableLogger.Log("{ContentId} is not minted. Minting token {TokenAddress}", contentId,
						mintAccount.PublicKey.Key);

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
								symbol = "",
								uri = "",
								creators = new List<Creator> { new(realmWallet.Account.PublicKey, 100, true) },
								collection = new Collection(defaultCollection, false)
							},
							false,
							false
						));
					
					TransactionManager.AddSigner(mintAccount);
				}
			}
		}
	}
}
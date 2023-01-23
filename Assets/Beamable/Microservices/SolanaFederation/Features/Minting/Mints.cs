using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using MongoDB.Driver;
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
	}
}
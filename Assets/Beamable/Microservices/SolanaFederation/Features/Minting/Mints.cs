using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using MongoDB.Driver;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	public class Mints
	{
		private readonly IMongoDatabase _db;
		private readonly Dictionary<string, string> _mintsByContent;
		private readonly Dictionary<string, string> _mintsByTokens;

		public Mints(IList<Mint> mints, IMongoDatabase db)
		{
			_db = db;
			_mintsByContent = mints.ToDictionary(x => x.ContentId, x => x.PublicKey);
			_mintsByTokens = mints.ToDictionary(x => x.PublicKey, x => x.ContentId);
		}

		public HashSet<string> ContentIds
		{
			get
			{
				return _mintsByContent.Keys.ToHashSet();
			}
		}

		public HashSet<string> Tokens
		{
			get
			{
				return _mintsByTokens.Keys.ToHashSet();
			}
		}

		private void AddMint(Mint mint)
		{
			_mintsByContent[mint.ContentId] = mint.PublicKey;
			_mintsByTokens[mint.PublicKey] = mint.ContentId;
		}

		public string GetByContent(string contentId)
		{
			return _mintsByContent.GetValueOrDefault(contentId);
		}

		public string GetByToken(string token)
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

		public async ValueTask EnsureExist(string contentId)
		{
			if (!ContainsContent(contentId))
			{
				var newMint = await MintingService.GetOrCreateMint(_db, contentId);
				AddMint(newMint);
			}
		}

		public async ValueTask EnsureExist(IEnumerable<string> contentIds)
		{
			foreach (var contentId in contentIds) await EnsureExist(contentId);
		}
	}
}
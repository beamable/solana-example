using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using MongoDB.Driver;

namespace Beamable.Microservices.SolanaFederation.Features.Minting.Storage
{
	public static class MintCollection
	{
		private static IMongoCollection<Mint> _collection;

		private static async ValueTask<IMongoCollection<Mint>> Get()
		{
			if (_collection is null)
			{
				_collection = ServiceContext.Database.GetCollection<Mint>("mint");
				await _collection.Indexes.CreateOneAsync(
					new CreateIndexModel<Mint>(
						Builders<Mint>.IndexKeys
							.Ascending(x => x.ContentId)
							.Ascending(x => x.PublicKey),
						new CreateIndexOptions { Unique = true }
					)
				);
			}

			return _collection;
		}

		public static async Task<List<Mint>> GetAll()
		{
			var collection = await Get();
			var mints = await collection
				.Find(x => true)
				.ToListAsync();
			return mints;
		}

		public static async Task Upsert(IEnumerable<Mint> mints)
		{
			var collection = await Get();
			var ops = mints
				.Select(mint => new ReplaceOneModel<Mint>
					(Builders<Mint>.Filter.Where(x => x.ContentId == mint.ContentId && x.PublicKey == mint.PublicKey), mint)
					{ IsUpsert = true })
				.ToList();
			await collection.BulkWriteAsync(ops);
		}
	}
}
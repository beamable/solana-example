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

		public static async ValueTask<IMongoCollection<Mint>> Get(IMongoDatabase db)
		{
			if (_collection is null)
			{
				_collection = db.GetCollection<Mint>("mint");
				await _collection.Indexes.CreateOneAsync(
					new CreateIndexModel<Mint>(
						Builders<Mint>.IndexKeys.Ascending(x => x.ContentId),
						new CreateIndexOptions { Unique = true }
					)
				);
			}

			return _collection;
		}

		public static async Task<List<Mint>> GetAll(IMongoDatabase db)
		{
			var collection = await Get(db);
			var mints = await collection
				.Find(x => true)
				.ToListAsync();
			return mints;
		}

		public static async Task Upsert(IMongoDatabase db, IEnumerable<Mint> mints)
		{
			var collection = await Get(db);
			var ops = mints
				.Select(mint => new ReplaceOneModel<Mint>
					(Builders<Mint>.Filter.Where(x => x.ContentId == mint.ContentId), mint) { IsUpsert = true })
				.ToList();
			await collection.BulkWriteAsync(ops);
		}
	}
}
using Assets.Beamable.Microservices.SolanaFederation.Storage.Models;
using MongoDB.Driver;
using System.Threading.Tasks;

namespace Assets.Beamable.Microservices.SolanaFederation.Storage
{
	public static class ValutCollection
	{
		private static IMongoCollection<Valut> collection = null;

		private static async ValueTask<IMongoCollection<Valut>> Get(IMongoDatabase db)
		{
			if (collection is null)
			{
				collection = db.GetCollection<Valut>("valut");
				await collection.Indexes.CreateOneAsync(
						new CreateIndexModel<Valut>(
							Builders<Valut>.IndexKeys.Ascending(x => x.Name),
								new CreateIndexOptions() { Unique = true }
						)
					);
			}
			return collection;
		}

		public static async Task<Valut> GetByName(IMongoDatabase db, string name)
		{
			var collection = await Get(db);
			return await collection
				.Find(x => x.Name == name)
				.FirstOrDefaultAsync();
		}

		public static async Task<bool> TryInsert(IMongoDatabase db, Valut valut)
		{
			var collection = await Get(db);
			try
			{
				await collection.InsertOneAsync(valut);
				return true;
			}
			catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
			{
				return false;
			}
		}
	}
}

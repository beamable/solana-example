using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Storage.Models;
using MongoDB.Driver;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Storage
{
	public static class VaultCollection
	{
		private static IMongoCollection<Vault> _collection;

		private static async ValueTask<IMongoCollection<Vault>> Get()
		{
			if (_collection is null)
			{
				_collection = ServiceContext.Database.GetCollection<Vault>("vault");
				await _collection.Indexes.CreateOneAsync(
					new CreateIndexModel<Vault>(
						Builders<Vault>.IndexKeys.Ascending(x => x.Name),
						new CreateIndexOptions { Unique = true }
					)
				);
			}

			return _collection;
		}

		public static async Task<Vault> GetByName(string name)
		{
			var collection = await Get();
			return await collection
				.Find(x => x.Name == name)
				.FirstOrDefaultAsync();
		}

		public static async Task<bool> TryInsert(Vault vault)
		{
			var collection = await Get();
			try
			{
				await collection.InsertOneAsync(vault);
				return true;
			}
			catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
			{
				return false;
			}
		}
	}
}
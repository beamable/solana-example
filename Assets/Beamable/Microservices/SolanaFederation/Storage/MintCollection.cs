using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Models;
using Beamable.Microservices.SolanaFederation.Storage.Models;
using MongoDB.Driver;

namespace Beamable.Microservices.SolanaFederation.Storage
{
    public class MintCollection
    {
        private static IMongoCollection<Mint> _collection = null;

        private static async ValueTask<IMongoCollection<Mint>> Get(IMongoDatabase db)
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

        public static async Task<Mint> Get(IMongoDatabase db, string contentId)
        {
            var collection = await Get(db);
            return await collection
                .Find(x => x.ContentId == contentId)
                .FirstOrDefaultAsync();
        }

        public static async Task<Mints> GetAll(IMongoDatabase db)
        {
            var collection = await Get(db);
            var mints = await collection
                .Find(x => true)
                .ToListAsync();
            return new Mints(mints);
        }

        public static async Task Insert(IMongoDatabase db, Mint mint)
        {
            var collection = await Get(db);
            await collection.InsertOneAsync(mint);
        }
    }
}
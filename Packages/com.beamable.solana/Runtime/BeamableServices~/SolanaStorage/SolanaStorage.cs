using Beamable.Common;
using MongoDB.Driver;

namespace Beamable.Server
{
	[StorageObject("SolanaStorage")]
	public class SolanaStorage : MongoStorageObject
	{
	}

	public static class SolanaStorageExtension
	{
		/// <summary>
		/// Get an authenticated MongoDB instance for SolanaStorage
		/// </summary>
		/// <returns></returns>
		public static Promise<IMongoDatabase> SolanaStorageDatabase(this IStorageObjectConnectionProvider provider)
			=> provider.GetDatabase<SolanaStorage>();

		/// <summary>
		/// Gets a MongoDB collection from SolanaStorage by the requested name, and uses the given mapping class.
		/// If you don't want to pass in a name, consider using <see cref="SolanaStorageCollection{TCollection}()"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> SolanaStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider, string name)
			where TCollection : StorageDocument
			=> provider.GetCollection<SolanaStorage, TCollection>(name);

		/// <summary>
		/// Gets a MongoDB collection from SolanaStorage by the requested name, and uses the given mapping class.
		/// If you want to control the collection name separate from the class name, consider using <see cref="SolanaStorageCollection{TCollection}(string)"/>
		/// </summary>
		/// <param name="name">The name of the collection</param>
		/// <typeparam name="TCollection">The type of the mapping class</typeparam>
		/// <returns>When the promise completes, you'll have an authorized collection</returns>
		public static Promise<IMongoCollection<TCollection>> SolanaStorageCollection<TCollection>(
			this IStorageObjectConnectionProvider provider)
			where TCollection : StorageDocument
			=> provider.GetCollection<SolanaStorage, TCollection>();
	}
}

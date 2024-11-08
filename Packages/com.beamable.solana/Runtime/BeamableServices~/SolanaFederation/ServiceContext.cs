using Beamable.Common.Api;
using MongoDB.Driver;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation
{
	internal static class ServiceContext
	{
		public static IMongoDatabase Database;
		public static Wallet RealmWallet;
		public static IBeamableRequester Requester;
	}
}
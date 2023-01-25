using System;

namespace Beamable.Microservices.SolanaFederation
{
	internal static class Configuration
	{
		public const string SolanaCluster = "https://api.devnet.solana.com";
		public const int MaxRpcRequestsPerSec = 6;

		public const int AuthenticationChallengeTtlSec = 600;

		public const string RealmWalletName = "default-wallet";
		public const int AirDropAmount = 1;

		public static readonly string RealmSecret = Environment.GetEnvironmentVariable("SECRET");
	}
}
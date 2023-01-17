using System;

namespace Beamable.Microservices.SolanaFederation
{
    static class Configuration
    {
        public const string SolanaCluster = "https://api.devnet.solana.com";
        
        public const int AuthenticationChallengeTtlSec = 600;
        
        public const string RealmWalletName = "default";
        public const int AirDropAmount = 10;

        public static readonly string RealmSecret = Environment.GetEnvironmentVariable("SECRET");
    }
}
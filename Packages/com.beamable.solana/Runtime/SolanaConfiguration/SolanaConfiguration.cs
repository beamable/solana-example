using System;

namespace Beamable.Solana.Configuration
{
    public class SolanaConfigurationConstants : IConfigurationConstants
    {
        public string GetSourcePath(Type type)
        {
            return "Packages/com.beamable.solana/Editor/SolanaConfiguration.asset";
        }
    }

    public class SolanaConfiguration : AbsModuleConfigurationObject<SolanaConfigurationConstants>
    {
        public enum RpcCluster
        {
            MainNet = 0,
            DevNet = 1,
            TestNet = 2,
            Custom = 3
        }

        public static SolanaConfiguration Instance => Get<SolanaConfiguration>();

        public RpcCluster SelectedCluster;

        [HideIfEnumValue("SelectedCluster", HideIf.NotEqual, (int)RpcCluster.Custom)]
        public string CustomClusterAddress;

        public int MaxRpcRequestsPerSec = 6;
        public int AuthenticationChallengeTtlSec = 600;
        public string RealmWalletName = "default-wallet";
        public string DefaultTokenCollectionName = "Beamable";
        public int AirDropAmount = 1;

        public string SolanaCluster => BuildClusterAddress();
        public readonly string RealmSecret = Environment.GetEnvironmentVariable("SECRET");

        private string BuildClusterAddress()
        {
            return SelectedCluster == RpcCluster.Custom
                ? CustomClusterAddress
                : $"https://api.{GetClusterString(SelectedCluster)}.solana.com";
        }

        private static string GetClusterString(RpcCluster rpcCluster)
        {
            return rpcCluster switch
            {
                RpcCluster.MainNet => "mainnet-beta",
                RpcCluster.DevNet => "devnet",
                RpcCluster.TestNet => "testnet",
                _ => "mainnet-beta"
            };
        }
    }
}
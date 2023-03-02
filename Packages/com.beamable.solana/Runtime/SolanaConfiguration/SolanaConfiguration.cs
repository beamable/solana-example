using System;
using Beamable.Solana.Common;

namespace Beamable.Solana.Configuration
{
    public class SolanaConfigurationConstants : IConfigurationConstants
    {
        public const string ASSET_PATH = "Assets/Beamable/Resources/SolanaConfiguration.asset";
        public string GetSourcePath(Type type)
        {
            return "Packages/com.beamable.solana/Editor/SolanaConfiguration.asset";
        }
    }


    public class SolanaConfiguration : AbsModuleConfigurationObject<SolanaConfigurationConstants>
    {
        public static SolanaConfiguration Instance => Get<SolanaConfiguration>();
        public static SolanaConfigData SharedInstance => Instance.sharedConfiguration;

        public SolanaConfigData sharedConfiguration;
    }
}
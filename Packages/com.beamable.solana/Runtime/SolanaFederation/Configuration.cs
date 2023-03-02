using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Beamable.Server.Api.RealmConfig;

namespace Beamable.Microservices.SolanaFederation
{
	internal static class Configuration
	{
		private const string ConfigurationNamespace = "federation_solana";

		private static T GetValue<T>(string key, T defaultValue) where T : IConvertible
		{
			var namespaceConfig = RealmConfig.GetValueOrDefault(ConfigurationNamespace) ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
			var value = namespaceConfig.GetValueOrDefault(key);
			if (value is null)
				return defaultValue;
			return (T)Convert.ChangeType(value, typeof(T));
		}

		public static RealmConfig RealmConfig { get; internal set; }

		public static readonly string RealmSecret = Environment.GetEnvironmentVariable("SECRET");

		#region ConfigurationValues

		public static string ClusterAddress => GetValue(nameof(ClusterAddress), "https://api.devnet.solana.com");
		public static string RealmWalletName => GetValue(nameof(RealmWalletName), "default-wallet");
		public static string DefaultTokenCollectionName => GetValue(nameof(DefaultTokenCollectionName), "Beamable");
		public static int MaxRpcRequestsPerSec => GetValue(nameof(MaxRpcRequestsPerSec), 6);
		public static int AuthenticationChallengeTtlSec => GetValue(nameof(AuthenticationChallengeTtlSec), 600);
		public static int AirDropAmount => GetValue(nameof(AirDropAmount), 1);

		#endregion
	}
}
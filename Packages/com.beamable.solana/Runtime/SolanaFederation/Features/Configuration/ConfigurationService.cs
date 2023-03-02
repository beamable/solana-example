using System.IO;
using Beamable.Solana.Common;
using UnityEngine;

namespace Beamable.Microservices.SolanaFederation.Features.Configuration
{
	public static class ConfigurationService
	{
		public static SolanaConfigData Configuration { get; private set; }

		static ConfigurationService()
		{
			var configJson = File.ReadAllText(SolanaConstants.SERVIER_CONFIG_PATH);
			Configuration = JsonUtility.FromJson<SolanaConfigData>(configJson);
		}
	}
}
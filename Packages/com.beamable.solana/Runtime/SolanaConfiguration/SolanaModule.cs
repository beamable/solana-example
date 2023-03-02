#if UNITY_EDITOR
using UnityEditor;

namespace Beamable.Solana.Configuration
{
	[InitializeOnLoad]
	public static class SolanaModule
	{
		static SolanaModule()
		{
			Initialize();
		}

		private static void Initialize()
		{
			try
			{
				_ = SolanaConfiguration.SharedInstance;
			}
			catch (ModuleConfigurationNotReadyException)
			{
				EditorApplication.delayCall += Initialize;
			}
		}
	}
}
#endif
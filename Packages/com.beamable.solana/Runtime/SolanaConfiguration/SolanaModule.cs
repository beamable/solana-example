using UnityEditor;

namespace Beamable.Solana.Editor
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
				_ = SolanaConfiguration.Instance;
			}
			catch (ModuleConfigurationNotReadyException)
			{
				EditorApplication.delayCall += Initialize;
			}
		}
	}
}
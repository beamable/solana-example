using System.IO;
using Beamable.Common.Dependencies;
using Beamable.Microservices.SolanaFederation;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Solana.Common;
using Beamable.Solana.Configuration;
using UnityEngine;

namespace Beamable.Solana.Editor
{
	public class SolanaFederationBuildHook : IMicroserviceBuildHook<SolanaFederation>
	{
		public void Execute(IMicroserviceBuildContext ctx)
		{
			var json = JsonUtility.ToJson(SolanaConfiguration.SharedInstance);
			var containerPath = Path.Combine(ctx.Descriptor.BuildPath, SolanaConstants.SERVIER_CONFIG_PATH);
			var targetDir = Path.GetDirectoryName(containerPath);
			
			Directory.CreateDirectory(targetDir);
			File.WriteAllText(containerPath, json);

			ctx.CommitFile(SolanaConstants.SERVIER_CONFIG_PATH);
		}
	}

	[BeamContextSystem]
	public class Registrations
	{
		[RegisterBeamableDependencies(-1, RegistrationOrigin.EDITOR)]
		public static void Register(IDependencyBuilder builder)
		{
			builder.AddSingleton<IMicroserviceBuildHook<SolanaFederation>, SolanaFederationBuildHook>();
		}
	}
}
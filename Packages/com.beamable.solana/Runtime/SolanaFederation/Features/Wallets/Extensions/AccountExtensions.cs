using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Solana.Configuration;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions
{
	public static class AccountExtensions
	{
		public static async Task Airdrop(this Account account, int amount, int waitSecAfterAirdrop = 30)
		{
			BeamableLogger.Log("Requesting airdrop of {Amount} to {PublicKey}", amount, account.PublicKey.Key);
			var rpcClient = ClientFactory.GetClient(SolanaConfiguration.Instance.SolanaCluster);
			try
			{
				var airdropResponse =
					await rpcClient.RequestAirdropAsync(account.PublicKey.Key, SolHelper.ConvertToLamports(amount));
				BeamableLogger.Log("Airdrop finished with {@AirdropResponse}", airdropResponse);
				BeamableLogger.Log("Waiting for {WaitSec}s after airdrop for cluster state sync", waitSecAfterAirdrop);
				await Task.Delay(waitSecAfterAirdrop * 1000);
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError(ex);
			}
		}
	}
}
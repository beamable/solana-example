using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions
{
	public static class AccountExtensions
	{
		public static Mint ToMint(this Account account, string contentId)
		{
			return new Mint {
				ContentId = contentId,
				PublicKey = account.PublicKey
			};
		}

		public static async Task Airdrop(this Account account, int amount)
		{
			BeamableLogger.Log("Requesting airdrop of {Amount} to {PublicKey}", amount, account.PublicKey.Key);
			var rpcClient = ClientFactory.GetClient(Configuration.SolanaCluster);
			try
			{
				var airdropResponse = await rpcClient.RequestAirdropAsync(account.PublicKey.Key, SolHelper.ConvertToLamports(amount));
				BeamableLogger.Log("Airdrop finished with {@AirdropResponse}", airdropResponse);
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError(ex);
			}
		}
	}
}
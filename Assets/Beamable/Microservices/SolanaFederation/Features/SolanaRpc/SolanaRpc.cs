using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc.Extensions;
using Solana.Unity.Programs;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;

namespace Beamable.Microservices.SolanaFederation.Features.SolanaRpc
{
	public static class SolanaRpcClient
	{
		private static readonly IRpcClient Client =
			ClientFactory.GetClient(Configuration.SolanaCluster);

		private static readonly TokenBucket TokenBucket = new(Configuration.MaxRpcRequestsPerSec, 1000);

		private static async Task AcquireToken()
		{
			while (!await TokenBucket.TryConsume())
			{
				BeamableLogger.LogWarning("RPC Client was rate limited, waiting 100ms");
				await Task.Delay(100);
			}

			;
		}

		public static async Task<ulong> GetMinimumBalanceForRentExemptionAsync(long accountDataSize,
			Commitment commitment = Commitment.Confirmed)
		{
			BeamableLogger.Log("Calling GetMinimumBalanceForRentExemptionAsync");
			await AcquireToken();
			var result = await Client.GetMinimumBalanceForRentExemptionAsync(accountDataSize, commitment);
			result.ThrowIfError();
			return result.Result;
		}

		public static async Task<string> GetLatestBlockHashAsync()
		{
			BeamableLogger.Log("Calling GetRecentBlockHashAsync");
			await AcquireToken();
			var result = await Client.GetRecentBlockHashAsync();
			result.ThrowIfError();
			return result.Result.Value.Blockhash;
		}

		public static async Task<string> SendTransactionAsync(byte[] transaction,
			bool skipPreflight = false,
			Commitment commitment = Commitment.Finalized)
		{
			BeamableLogger.Log("Calling SendTransactionAsync");
			await AcquireToken();
			var result = await Client.SendTransactionAsync(transaction, skipPreflight, commitment);
			result.ThrowIfError();
			return result.Result;
		}

		public static async Task<AccountInfo> GetAccountInfoAsync(string pubKey)
		{
			BeamableLogger.Log("Calling GetAccountInfoAsync");
			await AcquireToken();
			var result = await Client.GetAccountInfoAsync(pubKey);
			result.ThrowIfError();
			return result.Result.Value;
		}

		public static async Task<List<TokenAccount>> GetTokenAccountsByOwnerAsync(string ownerPubKey)
		{
			BeamableLogger.Log("Calling GetTokenAccountsByOwnerAsync");
			await AcquireToken();
			var result = await Client.GetTokenAccountsByOwnerAsync(ownerPubKey, tokenProgramId: TokenProgram.ProgramIdKey);
			result.ThrowIfError();
			return result.Result.Value ?? new List<TokenAccount>();
		}

		public static async Task<TokenMintInfo> GetTokenMintInfoAsync(string pubKey)
		{
			BeamableLogger.Log("Calling GetTokenMintInfoAsync");
			await AcquireToken();
			var result = await Client.GetTokenMintInfoAsync(pubKey);
			result.ThrowIfError();
			return result.Result.Value;
		}
	}
}
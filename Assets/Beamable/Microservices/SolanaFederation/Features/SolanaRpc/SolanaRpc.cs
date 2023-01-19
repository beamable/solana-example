using System.Collections.Generic;
using System.Threading.Tasks;
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

		public static async Task<ulong> GetMinimumBalanceForRentExemptionAsync(long accountDataSize,
			Commitment commitment = Commitment.Confirmed)
		{
			var result = await Client.GetMinimumBalanceForRentExemptionAsync(accountDataSize, commitment);
			result.ThrowIfError();
			return result.Result;
		}

		public static async Task<string> GetLatestBlockHashAsync()
		{
			var result = await Client.GetRecentBlockHashAsync();
			result.ThrowIfError();
			return result.Result.Value.Blockhash;
		}

		public static async Task<string> SendTransactionAsync(byte[] transaction,
			bool skipPreflight = false,
			Commitment commitment = Commitment.Finalized)
		{
			var result = await Client.SendTransactionAsync(transaction, skipPreflight, commitment);
			result.ThrowIfError();
			return result.Result;
		}

		public static async Task<AccountInfo> GetAccountInfoAsync(string pubKey)
		{
			var result = await Client.GetAccountInfoAsync(pubKey);
			result.ThrowIfError();
			return result.Result.Value;
		}

		public static async Task<List<TokenAccount>> GetTokenAccountsByOwnerAsync(string ownerPubKey)
		{
			var result = await Client.GetTokenAccountsByOwnerAsync(ownerPubKey, tokenProgramId: TokenProgram.ProgramIdKey);
			result.ThrowIfError();
			return result.Result.Value ?? new List<TokenAccount>();
		}
	}
}
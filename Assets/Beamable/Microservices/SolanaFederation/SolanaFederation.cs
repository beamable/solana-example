using Assets.Beamable.Microservices.SolanaFederation.Exceptions;
using Assets.Beamable.Microservices.SolanaFederation.Services;
using Beamable.Common;
using Beamable.Server;
using Beamable.Common.Api.Auth;
using System;
using Assets.Beamable.Microservices.SolanaFederation;
using MongoDB.Driver;
using System.Threading.Tasks;
using Assets.Beamable.Microservices.SolanaFederation.Extensions;
using System.Net.Http;
using Solnet.Rpc;
using Solnet.Wallet;

namespace Beamable.Microservices
{
	[Microservice("SolanaFederation")]
	public class SolanaFederation : Microservice
	{
		private readonly IRpcClient _rpcClient;
		private Lazy<Task<Wallet>> _cachedRealmWallet => new Lazy<Task<Wallet>>(async () => await WalletService.GetRealmWallet(await GetDb()));

		public SolanaFederation()
		{
			BeamableLogger.Log($"Fetching RPC client for {Configuration.SolanaCluster}");
			_rpcClient = ClientFactory.GetClient(Configuration.SolanaCluster, null, null, null);
		}

		private async Task<IMongoDatabase> GetDb() => await Storage.GetDatabase<SolanaStorage>();

		[ClientCallable("authenticate")]
		public ExternalAuthenticationResponse Authenticate(string token, string challenge, string solution)
		{
			if (string.IsNullOrEmpty(token))
			{
				BeamableLogger.LogError("We didn't receive a token (public key)");
				throw new InvalidAuthenticationRequest("Token (public key) is required");
			}

			if (!string.IsNullOrEmpty(challenge) && !string.IsNullOrEmpty(solution))
			{
				// Verify the solution
				if (AuthenticationService.IsSignatureValid(token, challenge, solution))
				{
					// User identity confirmed
					return new ExternalAuthenticationResponse { user_id = token };
				}
				else
				{
					// Signature is invalid, user identity isn't confirmed
					BeamableLogger.LogWarning("Invalid signature {signature} for challenge {challenge} and wallet {wallet}", solution, challenge, token);
					throw new UnauthorizedException();
				}
			}
			else
			{
				// Generate a challenge
				return new ExternalAuthenticationResponse { challenge = Guid.NewGuid().ToString(), challenge_ttl = Configuration.AuthenticationChallengeTtlSec };
			}
		}

		[ClientCallable("account/balance")]
		public async Task<ulong> GetBalance(string publicKey)
		{
			var accountInfoResponse = await _rpcClient.GetAccountInfoAsync(publicKey);
			accountInfoResponse.ThrowIfError();
			return accountInfoResponse.Result.Value.Lamports;
		}

		[ClientCallable("account/realm/balance")]
		public async Task<ulong> GetRealmAccountBalance()
		{
			BeamableLogger.Log("Fetching realm wallet");
			var realmWallet = await _cachedRealmWallet.Value;
			BeamableLogger.Log("Realm wallet is {r}", realmWallet.Account.PublicKey.Key);
			return await GetBalance(realmWallet.Account.PublicKey.Key);
		}
	}
}

using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Microservices.SolanaFederation.Exceptions;
using Beamable.Microservices.SolanaFederation.Extensions;
using Beamable.Microservices.SolanaFederation.Services;
using Beamable.Server;
using MongoDB.Driver;
using Solnet.Rpc;

namespace Beamable.Microservices.SolanaFederation
{
    [Microservice("SolanaFederation")]
    public class SolanaFederation : Microservice
    {
        private readonly IRpcClient _rpcClient;

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
                    BeamableLogger.LogWarning(
                        "Invalid signature {signature} for challenge {challenge} and wallet {wallet}", solution,
                        challenge, token);
                    throw new UnauthorizedException();
                }
            }
            else
            {
                // Generate a challenge
                return new ExternalAuthenticationResponse
                {
                    challenge = Guid.NewGuid().ToString(), challenge_ttl = Configuration.AuthenticationChallengeTtlSec
                };
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
            var realmWallet = await WalletService.GetRealmWallet(await GetDb());
            BeamableLogger.Log("Realm wallet is {r}", realmWallet.Account.PublicKey.Key);
            return await GetBalance(realmWallet.Account.PublicKey.Key);
        }
    }
}
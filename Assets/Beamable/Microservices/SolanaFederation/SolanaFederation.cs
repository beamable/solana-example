using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Inventory;
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
        public SolanaFederation()
        {
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
            var accountInfoResponse = await SolanaRpc.Client.GetAccountInfoAsync(publicKey);
            accountInfoResponse.ThrowIfError();
            return accountInfoResponse.Result.Value.Lamports;
        }

        [ClientCallable("account/realm/balance")]
        public async Task<ulong> GetRealmAccountBalance()
        {
            BeamableLogger.Log("Fetching realm wallet");
            var realmWallet = await WalletService.GetRealmWallet(await GetDb());
            BeamableLogger.Log("Realm wallet is {RealmWallet}", realmWallet.Account.PublicKey.Key);
            return await GetBalance(realmWallet.Account.PublicKey.Key);
        }

        [ClientCallable("inventory/transaction/start")]
        public async Task<InventoryProxyState> StartInventoryTransaction(InventoryProxyUpdateRequest request)
        {
            /*
             * FLOW v0.1 - Currency implementation:
             *  - fetch the realm wallet
             *  - fetch players account manifest              
             *  - request validation (only handle currency increase)
             *  - get or create mints
             *  - create a single mint&transfer transaction 
             *  - return players new manifest
             */

            await Task.Yield();
            return null;
        }
        
        // Not used currently
        [ClientCallable("inventory/transaction/end")]
        public InventoryProxyState EndInventoryTransaction(InventoryProxyUpdateRequest request)
        {
            return new InventoryProxyState();
        }
    }
}
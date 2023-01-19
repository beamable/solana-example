using System;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Authentication;
using Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Server;
using MongoDB.Driver;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation
{
    /*
     * TODO:
     * - Custom thread-safe RPC client rate limiting and make it configurable
     */
    [Microservice("SolanaFederation")]
    public class SolanaFederationService : Microservice
    {
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

        [ClientCallable("inventory/transaction/start")]
        public async Task<InventoryProxyState> StartInventoryTransaction(InventoryProxyUpdateRequest request)
        {
            var db = await GetDb();

            // Fetch current mints
            var mints = await MintCollection.GetAll(db);

            // Ensure all contentIds are minted 
            await mints.EnsureExist(request.currencies.Keys);
            await mints.EnsureExist(request.newItems.Select(x => x.contentId));

            // Compute the current player token state
            var playerTokenState = await PlayerTokenState.Compute(request.id, mints);
            
            var playerKey = new PublicKey(request.id);
            var realmWallet = await WalletService.GetRealmWallet(db);
            
            // Compute new token transactions
            var newTokens = playerTokenState
                .GetNewTokensFromRequest(request, mints)
                .ToList();
            var newInstructions = newTokens
                .Select(c => c.GetInstructions(playerKey, realmWallet.Account.PublicKey))
                .SelectMany(x => x)
                .ToList();

            if (newInstructions.Any())
            {
                // Send the transaction
                var transactionId = await TransactionExecutor.Execute(newInstructions, realmWallet);
                BeamableLogger.Log("Transaction {TransactionId} processed successfully", transactionId);
            }
            else
            {
                BeamableLogger.LogWarning("No transaction instructions were generated for the request");
            }

            return playerTokenState
                .MergeIn(newTokens)
                .ToProxyState();
        }

        // Not used currently
        [ClientCallable("inventory/transaction/end")]
        public InventoryProxyState EndInventoryTransaction(InventoryProxyUpdateRequest request)
        {
            return new InventoryProxyState();
        }
        
        [ClientCallable("account/balance")]
        public async Task<ulong> GetBalance(string publicKey)
        {
            var accountInfoResponse = await SolanaRpcClient.GetAccountInfoAsync(publicKey);
            return accountInfoResponse.Lamports;
        }

        [ClientCallable("account/realm")]
        public async Task<string> GetRealmAccount()
        {
            var realmWallet = await WalletService.GetRealmWallet(await GetDb());
            return realmWallet.Account.PublicKey.Key;
        }
    }
}
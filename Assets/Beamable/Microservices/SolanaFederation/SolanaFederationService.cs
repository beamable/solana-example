using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Authentication;
using Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions;
using Beamable.Microservices.SolanaFederation.Features.Minting;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.PlayerAssets;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Server;
using MongoDB.Driver;
using Solnet.Programs;
using Solnet.Rpc.Builders;
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
        public SolanaFederationService()
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
            var accountInfoResponse = await SolanaRpcClient.GetAccountInfoAsync(publicKey);
            return accountInfoResponse.Lamports;
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
             * FLOW v0.1 - SFT implementation:
             *  - fetch the realm wallet
             *  - fetch players account manifest              
             *  - request validation (only handle currency increase)
             *  - get or create mints
             *  - create a single mint&transfer transaction 
             *  - return players new manifest
             */
            var playerAccountTokensResponse =
                await SolanaRpcClient.GetTokenAccountsByOwnerAsync(request.id);

            var db = await GetDb();

            var mints = await MintCollection.GetAll(db);

            var newCurrency = request
                .currencies
                .Keys
                .Except(mints.ContentIds)
                .ToList();

            // Mint missing currency tokens
            foreach (var newCurrencyForMint in newCurrency)
            {
                var mint = await MintingService.GetOrCreateMint(db, newCurrencyForMint);
                mints.AddMint(new Mint { ContentId = newCurrencyForMint, PublicKey = mint.PublicKey });
            }

            var playerState = new PlayerTokenState(mints, playerAccountTokensResponse);

            var blockHash = await SolanaRpcClient.GetLatestBlockHashAsync();

            var realmWallet = await WalletService.GetRealmWallet(db);

            var transactionBuilder = new TransactionBuilder()
                .SetFeePayer(realmWallet.Account.PublicKey)
                .SetRecentBlockHash(blockHash);

            var playerKey = new PublicKey(request.id);

            var hasInstructions = false;
            foreach (var currency in request.currencies)
            {
                var currencyMint = new PublicKey(mints.GetByContent(currency.Key));
                var currentAmount = playerState.GetTokenAmount(currencyMint);

                var delta = currency.Value - currentAmount;
                if (delta > 0)
                {
                    var needsTokenAccount = !playerState.ContainsToken(currencyMint);
                    if (needsTokenAccount)
                    {
                        transactionBuilder.AddInstruction(
                            AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                                realmWallet.Account.PublicKey,
                                playerKey,
                                currencyMint
                            )
                        );
                    }
                    transactionBuilder.AddInstruction(
                        TokenProgram.MintTo(
                            currencyMint,
                            AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(playerKey, currencyMint),
                            (ulong)delta,
                            realmWallet.Account.PublicKey
                        )
                    );

                    hasInstructions = true;
                }
                else if (delta < 0)
                {
                    BeamableLogger.LogWarning("Currency {ContentId} has an amount decrease. Ignoring.", currency.Key);
                }
            }

            if (hasInstructions)
            {
                var transaction = transactionBuilder.Build(new List<Account> { realmWallet.Account });
                var transactionId = await SolanaRpcClient.SendTransactionAsync(transaction);
            }

            return playerState.ToProxyState();
        }

        // Not used currently
        [ClientCallable("inventory/transaction/end")]
        public InventoryProxyState EndInventoryTransaction(InventoryProxyUpdateRequest request)
        {
            return new InventoryProxyState();
        }
    }
}
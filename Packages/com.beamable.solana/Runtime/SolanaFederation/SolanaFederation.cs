using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Authentication;
using Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions;
using Beamable.Microservices.SolanaFederation.Features.Collections;
using Beamable.Microservices.SolanaFederation.Features.Configuration;
using Beamable.Microservices.SolanaFederation.Features.Minting;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Server;
using Beamable.Solana.Common;
using UnityEngine;

// THOUGHTS
// 1. SolanaConfiguration asset is not a part of the container
// 2. Should we modify docker build process and copy it there?? In this case we would need YAML parser to get data from it.
// 3. Maybe it would be better to serialize config asset to json during build process and copy json into container? (for the moment seems to be the most reasonable?)
// 4. Either way we need to have a mechanism to access this data here. It can't be done by accessing static
//    SolanaConfiguration.Instance like we do in Beamable Unity SDK solution.
// 5. Maybe we could have mongo collection created at some point that will hold configuration data? Is it even possible 
//    to have more collection dependencies in one microservice? And is it possible to handle this on the fly during MS 
//    build process?


namespace Beamable.Microservices.SolanaFederation
{
    [Microservice("SolanaFederation")]
    public class SolanaFederation : Microservice, IFederatedInventory<SolanaCloudIdentity>
    {
        [InitializeServices]
        public static async Task Initialize(IServiceInitializer initializer)
        {
            // check configuration
            var config = ConfigurationService.Configuration;
            Debug.Log($"Solana config. cluster=[{config.SolanaCluster}] realm-wallet=[{config.RealmWalletName}] airdrop-amount=[{config.AirDropAmount}]");
           
            
            var storage = initializer.GetService<IStorageObjectConnectionProvider>();
            var db = await storage.SolanaStorageDatabase();

            TransactionManager.InitTransaction();

            // Fetch the realm wallet on service start for early initialization
            var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
            TransactionManager.AddSigner(realmWallet.Account);

            // Fetch the default token collection on service start for early initialization
            var _ = await CollectionService.GetOrCreateCollection(
                ConfigurationService.Configuration.DefaultTokenCollectionName, realmWallet);

            if (TransactionManager.HasInstructions()) await TransactionManager.Execute(realmWallet);
        }

        public Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution)
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
                    // User identity is confirmed
                    return Promise<FederatedAuthenticationResponse>.Successful(new FederatedAuthenticationResponse
                        { user_id = token });
                // Signature is invalid, user identity isn't confirmed
                BeamableLogger.LogWarning(
                    "Invalid signature {signature} for challenge {challenge} and wallet {wallet}", solution,
                    challenge, token);
                throw new UnauthorizedException();
            }

            // Generate a challenge
            return Promise<FederatedAuthenticationResponse>.Successful(new FederatedAuthenticationResponse
            {
                challenge = $"Please sign this random message to authenticate, {Guid.NewGuid()}",
                challenge_ttl = ConfigurationService.Configuration.AuthenticationChallengeTtlSec
            });
        }

        public async Promise<FederatedInventoryProxyState> StartInventoryTransaction(string id, string transaction,
            Dictionary<string, long> currencies, List<ItemCreateRequest> newItems)
        {
            BeamableLogger.Log("Processing start transaction request {TransactionId}", transaction);
            var db = await Storage.SolanaStorageDatabase();

            TransactionManager.InitTransaction();

            var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
            // All mints are initiated using the realm wallet so it needs to sign every transaction
            TransactionManager.AddSigner(realmWallet.Account);

            var mints = new Mints(realmWallet, db);

            // Load persisted content/mint mappings
            await mints.LoadPersisted();

            // Compute the current player token state
            var playerTokenState = await PlayerTokenState.Compute(id, mints);

            // Mint new items as NFTs
            var newItemTokens = await mints.MintNewItems(newItems, realmWallet, db, id, Requester);
            playerTokenState.MergeIn(newItemTokens);

            // TODO: update support for NFT metadata

            // Mint new currency as FTs 
            var newCurrencyTokens = await mints.MintNewCurrency(id, currencies, realmWallet, playerTokenState);
            playerTokenState.MergeIn(newCurrencyTokens);

            // Execute the transaction
            await TransactionManager.Execute(realmWallet);

            // Return the new federated state
            return playerTokenState.ToProxyState();
        }

        public async Promise<FederatedInventoryProxyState> GetInventoryState(string id)
        {
            var db = await Storage.SolanaStorageDatabase();
            var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
            var mints = new Mints(realmWallet, db);

            // Load persisted content/mint mappings
            await mints.LoadPersisted();

            // Compute the current player token state
            var playerTokenState = await PlayerTokenState.Compute(id, mints);

            // Return the federated state
            return playerTokenState.ToProxyState();
        }
    }
}
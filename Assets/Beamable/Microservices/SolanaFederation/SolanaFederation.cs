using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Authentication;
using Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions;
using Beamable.Microservices.SolanaFederation.Features.Collections;
using Beamable.Microservices.SolanaFederation.Features.Minting;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Microservices.SolanaFederation.Models;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation
{
	[Microservice("SolanaFederation")]
	public class SolanaFederation : Microservice, IFederatedLogin<SolanaCloudIdentity>
	{
		[InitializeServices]
		public static async Task Initialize(IServiceInitializer initializer)
		{
			var storage = initializer.GetService<IStorageObjectConnectionProvider>();
			var db = await storage.SolanaStorageDatabase();

			TransactionManager.InitTransaction();

			// Fetch the realm wallet on service start for early initialization
			var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
			TransactionManager.AddSigner(realmWallet.Account);

			// Fetch the default token collection on service start for early initialization
			var _ = await CollectionService.GetOrCreateCollection(Configuration.DefaultTokenCollectionName, realmWallet);

			if (TransactionManager.HasInstructions()) await TransactionManager.Execute(realmWallet);
		}
		
		[ClientCallable("solana/authenticate")]
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
					// User identity confirmed
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
				challenge = Guid.NewGuid().ToString(), challenge_ttl = Configuration.AuthenticationChallengeTtlSec
			});
		}

		[ClientCallable("solana/inventory/State")]
		public async Task<InventoryProxyState> GetInventoryState(string id)
		{
			var db = await Storage.SolanaStorageDatabase();
			var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
			var mints = new Mints(realmWallet, db);

			// Load persisted content/mint mappings
			await mints.LoadPersisted();

			// Compute the current player token state
			var playerTokenState = await PlayerTokenState.Compute(id, mints);

			return playerTokenState.ToProxyState();
		}

		[ClientCallable("solana/inventory/Put")]
		public async Task<InventoryProxyState> InventoryPut(string id, string transaction,
			Dictionary<string, long> currencies, List<InventoryItem> newItems)
		{
			BeamableLogger.Log("Processing start transaction request {TransactionId}", transaction);
			var db = await Storage.SolanaStorageDatabase();

			TransactionManager.InitTransaction();

			var realmWallet = await WalletService.GetOrCreateRealmWallet(db);
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

			await TransactionManager.Execute(realmWallet);

			return playerTokenState.ToProxyState();
		}
	}
}
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
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets;
using Beamable.Server;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation
{
	[Microservice("SolanaFederation")]
	public class SolanaFederation : Microservice
	{
		[InitializeServices]
		public static async Task Initialize(IServiceInitializer initializer)
		{
			var storage = initializer.GetService<IStorageObjectConnectionProvider>();
			var db = await storage.SolanaStorageDatabase();
			
			// Fetch the realm wallet on service start to force initialization
			var _ = await WalletService.GetRealmWallet(db);
		}

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
					// User identity confirmed
					return new ExternalAuthenticationResponse { user_id = token };
				// Signature is invalid, user identity isn't confirmed
				BeamableLogger.LogWarning(
					"Invalid signature {signature} for challenge {challenge} and wallet {wallet}", solution,
					challenge, token);
				throw new UnauthorizedException();
			}

			// Generate a challenge
			return new ExternalAuthenticationResponse
			{
				challenge = Guid.NewGuid().ToString(), challenge_ttl = Configuration.AuthenticationChallengeTtlSec
			};
		}

		[ClientCallable("inventory")]
		public async Task<InventoryProxyState> GetInventoryState(string id)
		{
			var db = await Storage.SolanaStorageDatabase();
			var realmWallet = await WalletService.GetRealmWallet(db);
			var mints = new Mints(realmWallet, db);

			// Load persisted content/mint mappings
			await mints.LoadPersisted();

			// Compute the current player token state
			var playerTokenState = await PlayerTokenState.Compute(id, mints);

			return playerTokenState.ToProxyState();
		}

		[ClientCallable("inventory/transaction/start")]
		public async Task<InventoryProxyState> StartInventoryTransaction(string id, string transaction,
			Dictionary<string, long> currencies, List<ItemCreateRequest> newItems)
		{
			BeamableLogger.Log("Processing start transaction request {TransactionId}", transaction);
			var db = await Storage.SolanaStorageDatabase();
			var realmWallet = await WalletService.GetRealmWallet(db);

			TransactionManager.InitTransaction(realmWallet);

			var mints = new Mints(realmWallet, db);

			// Load persisted content/mint mappings
			await mints.LoadPersisted();

			var contentIds = currencies.Keys
				.Union(newItems.Select(x => x.contentId))
				.ToList();

			// Find and persist missing mints
			var missingMints = await mints.LoadMissing(contentIds);

			// Add instructions for creating missing mints
			await MintingService.EnsureMinted(contentIds, realmWallet);

			// Compute the current player token state
			var playerTokenState = await PlayerTokenState.Compute(id, mints);

			var playerKey = new PublicKey(id);

			// Compute new token transactions
			var newTokens = playerTokenState
				.GetNewTokensFromRequest(currencies, newItems, mints)
				.ToList();
			var newInstructions = newTokens
				.Select(c => c.GetInstructions(playerKey, realmWallet.Account.PublicKey))
				.SelectMany(x => x)
				.ToList();

			TransactionManager.AddInstructions(newInstructions);
			await TransactionManager.Execute(realmWallet);

			playerTokenState.MergeIn(newTokens);

			return playerTokenState.ToProxyState();
		}

		// Not used currently
		[ClientCallable("inventory/transaction/end")]
		public InventoryProxyState EndInventoryTransaction(string id, string transaction,
			Dictionary<string, long> currencies, List<ItemCreateRequest> newItems)
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
			var realmWallet = await WalletService.GetRealmWallet(await Storage.SolanaStorageDatabase());
			return realmWallet.Account.PublicKey.Key;
		}
	}
}
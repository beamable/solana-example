using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Minting;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Solana.Unity.Metaplex;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets
{
	public class PlayerTokenState
	{
		private readonly List<PlayerTokenInfo> _tokens;

		private PlayerTokenState(List<PlayerTokenInfo> tokens)
		{
			_tokens = tokens;
		}

		public static async Task<PlayerTokenState> Compute(string pubKey, Mints mints)
		{
			var tokenAccounts = await SolanaRpcClient.GetTokenAccountsByOwnerAsync(pubKey);

			var playerTokens = tokenAccounts
				.Where(x => mints.ContainsMint(x.Account.Data.Parsed.Info.Mint))
				.Select(x => new PlayerTokenInfo
				{
					TokenAccount = new PublicKey(x.PublicKey),
					Mint = new PublicKey(x.Account.Data.Parsed.Info.Mint),
					ContentId = mints.GetByToken(x.Account.Data.Parsed.Info.Mint).ContentId,
					Amount = x.Account.Data.Parsed.Info.TokenAmount.AmountDecimal
				})
				.ToList();

			var state = new PlayerTokenState(playerTokens);
			await state.LoadItemsMetadata();
			
			return state;
		}

		private async Task LoadItemsMetadata()
		{
			try
			{
				foreach (var token in _tokens.Where(t => !t.IsCurrency()))
				{
					var metadataAccount = await SolanaRpcClient.FetchMetadataAccount(token.Mint);
					if (metadataAccount is not null && !string.IsNullOrEmpty(metadataAccount.metadataV3?.uri))
					{
						var metadata = await SolanaRpcClient.FetchOffChainData(metadataAccount.metadataV3.uri);
						if (metadata is not null && metadata.attributes?.Any() == true)
						{
							token.Properties = metadata.attributes.ToDictionary(x => x.trait_type, x => x.value);
						}
					}
				}
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError("Error loading metadata");
				BeamableLogger.LogError(ex);
			}
		}

		public decimal GetCurrencyAmount(string contentId)
		{
			return _tokens
				.FirstOrDefault(x => x.ContentId == contentId)
				?.Amount ?? 0;
		}

		public IEnumerable<PlayerTokenInfo> GetNewCurrencyFromRequest(Dictionary<string, long> currencies, Mints mints)
		{
			foreach (var newCurrency in currencies)
			{
				var currentAmount = GetCurrencyAmount(newCurrency.Key);
				var newAmount = currentAmount + newCurrency.Value;
				
				if (newAmount > currentAmount)
				{
					var mint = mints.GetByContent(newCurrency.Key);
					yield return new PlayerTokenInfo
					{
						Amount = newCurrency.Value,
						ContentId = newCurrency.Key,
						Mint = new PublicKey(mint.PublicKey),
						TokenAccount = _tokens.FirstOrDefault(x => x.ContentId == newCurrency.Key)?.TokenAccount
					};
				}
			}
		}

		public PlayerTokenState MergeIn(IEnumerable<PlayerTokenInfo> tokenInfos)
		{
			foreach (var tokenInfo in tokenInfos)
				if (tokenInfo.IsCurrency())
				{
					var maybeExistingCurrency = _tokens.FirstOrDefault(x => x.ContentId == tokenInfo.ContentId);
					if (maybeExistingCurrency is not null)
						maybeExistingCurrency.Amount += tokenInfo.Amount;
					else
						_tokens.Add(tokenInfo);
				}
				else
				{
					_tokens.Add(tokenInfo);
				}

			return this;
		}

		public FederatedInventoryProxyState ToProxyState()
		{
			var tokens = _tokens.Where(x => x.Amount > 0).ToList();
			var currencies = tokens
				.Where(x => x.IsCurrency())
				.ToList();

			var items = tokens
				.Except(currencies)
				.GroupBy(x => x.ContentId)
				.ToList();

			return new FederatedInventoryProxyState
			{
				currencies = currencies.ToDictionary(x => x.ContentId, x => decimal.ToInt64(x.Amount)),
				items = items.ToDictionary(x => x.Key, x => x.Select(gv => new FederatedItemProxy
				{
					proxyId = gv.Mint.Key,
					properties = gv.Properties.Select(p => new ItemProperty
					{
						name = p.Key,
						value = p.Value
					}).ToList()
				}).ToList())
			};
		}
	}

	public record PlayerTokenInfo
	{
		public PublicKey TokenAccount { get; set; }
		public PublicKey Mint { get; set; }
		public string ContentId { get; set; }
		public decimal Amount { get; set; }
		public Dictionary<string, string> Properties { get; set; } = new();

		public bool IsCurrency()
		{
			return ContentId.StartsWith("currency.", StringComparison.InvariantCultureIgnoreCase);
		}

		public IEnumerable<TransactionInstruction> GetInstructions(PublicKey ownerKey, PublicKey realmWalletKey)
		{
			if (TokenAccount is null)
			{
				BeamableLogger.Log("Adding CreateAssociatedTokenAccount instruction for content {ContentId}, mint {Mint}",
					ContentId, Mint.Key);
				yield return AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
					realmWalletKey,
					ownerKey,
					Mint
				);
			}

			var tokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(ownerKey, Mint);
			BeamableLogger.Log(
				"Adding MintTo {Amount} instruction for content {ContentId}, mint {Mint}, player wallet {Wallet}", Amount,
				ContentId, Mint.Key, ownerKey.Key);
			yield return TokenProgram.MintTo(
				Mint,
				tokenAccount,
				(ulong)Amount,
				realmWalletKey
			);
		}
	}
}
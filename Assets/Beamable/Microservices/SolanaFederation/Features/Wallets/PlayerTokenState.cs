using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Features.Minting;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets
{
	internal class PlayerTokenState
	{
		private readonly Dictionary<string, PlayerTokenInfo> _tokensByContent;

		private PlayerTokenState(Dictionary<string, PlayerTokenInfo> tokensByContent)
		{
			_tokensByContent = tokensByContent;
		}

		public static async Task<PlayerTokenState> Compute(string pubKey, Mints mints)
		{
			var tokenAccounts = await SolanaRpcClient.GetTokenAccountsByOwnerAsync(pubKey);

			var playerTokens = tokenAccounts
				.Where(x => mints.ContainsMint(x.Account.Data.Parsed.Info.Mint))
				.Select(x => new PlayerTokenInfo {
					TokenAccount = new PublicKey(x.PublicKey),
					Mint = new PublicKey(x.Account.Data.Parsed.Info.Mint),
					ContentId = mints.GetByToken(x.Account.Data.Parsed.Info.Mint),
					Amount = x.Account.Data.Parsed.Info.TokenAmount.AmountDecimal
				})
				.ToDictionary(x => x.ContentId, x => x);

			return new PlayerTokenState(playerTokens);
		}

		public decimal GetTokenAmount(string contentId)
		{
			return _tokensByContent
				.GetValueOrDefault(contentId)
				?.Amount ?? 0;
		}

		public IEnumerable<PlayerTokenInfo> GetNewTokensFromRequest(InventoryProxyUpdateRequest request, Mints mints)
		{
			foreach (var newCurrency in request.currencies)
			{
				var currentAmount = GetTokenAmount(newCurrency.Key);

				if (newCurrency.Value > currentAmount)
				{
					var mint = mints.GetByContent(newCurrency.Key);
					yield return new PlayerTokenInfo {
						Amount = newCurrency.Value - currentAmount,
						ContentId = newCurrency.Key,
						Mint = new PublicKey(mint),
						TokenAccount = _tokensByContent.GetValueOrDefault(newCurrency.Key)?.TokenAccount
					};
				}
			}

			foreach (var newItem in request.newItems)
			{
				var mint = mints.GetByContent(newItem.contentId);
				yield return new PlayerTokenInfo {
					Amount = 1,
					ContentId = newItem.contentId,
					Mint = new PublicKey(mint),
					TokenAccount = _tokensByContent.GetValueOrDefault(newItem.contentId)?.TokenAccount
				};
			}
		}

		public PlayerTokenState MergeIn(IEnumerable<PlayerTokenInfo> tokenInfos)
		{
			foreach (var tokenInfo in tokenInfos)
				if (!_tokensByContent.ContainsKey(tokenInfo.ContentId))
					_tokensByContent[tokenInfo.ContentId] = tokenInfo;
				else
				{
					var currentTokenInfo = _tokensByContent[tokenInfo.ContentId];
					if (currentTokenInfo.IsCurrency())
						currentTokenInfo.Amount = tokenInfo.Amount;
					else
						currentTokenInfo.Amount += tokenInfo.Amount;
				}

			return this;
		}

		public bool ContainsToken(string contentId)
		{
			return _tokensByContent.ContainsKey(contentId);
		}

		public InventoryProxyState ToProxyState()
		{
			var currencies = _tokensByContent
				.Values
				.Where(x => x.IsCurrency())
				.ToList();
			
			var items = _tokensByContent
				.Values
				.Except(currencies)
				.GroupBy(x => x.ContentId)
				.ToList();

			return new InventoryProxyState { 
				currencies = currencies.ToDictionary(x => x.ContentId, x => decimal.ToInt64(x.Amount)),
				items = items.ToDictionary(x => x.Key, x => x.Select(y => new ItemProxy {
					proxyId = y.Mint
				}).ToList())
			};
		}
	}

	internal record PlayerTokenInfo
	{
		public PublicKey TokenAccount { get; set; }
		public PublicKey Mint { get; set; }
		public string ContentId { get; set; }
		public decimal Amount { get; set; }

		public bool IsCurrency()
		{
			return ContentId.StartsWith("currency.", StringComparison.InvariantCultureIgnoreCase);
		}

		public IEnumerable<TransactionInstruction> GetInstructions(PublicKey ownerKey, PublicKey realmWalletKey)
		{
			if (TokenAccount is null)
				yield return AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
					realmWalletKey,
					ownerKey,
					Mint
				);

			yield return TokenProgram.MintTo(
				Mint,
				AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(ownerKey, Mint),
				(ulong)Amount,
				realmWalletKey
			);
		}
	}
}
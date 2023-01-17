using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beamable.Common.Api.Inventory;
using Beamable.Microservices.SolanaFederation.Models;
using Solnet.Rpc.Models;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation
{
    internal class PlayerTokenState
    {
        private readonly Dictionary<string, PlayerToken> _tokens;
        public PlayerTokenState(Mints mints, List<TokenAccount> tokenAccounts)
        {
            _tokens = tokenAccounts
                .Where(x => mints.ContainsMint(x.Account.Data.Parsed.Info.Mint))
                .Select(x => new PlayerToken
                {
                    TokenAccount = new PublicKey(x.PublicKey),
                    Mint = new PublicKey(x.Account.Data.Parsed.Info.Mint),
                    ContentId = mints.GetByToken(x.Account.Data.Parsed.Info.Mint)
                })
                .ToDictionary(x => x.Mint.Key, x => x);
        }

        public long GetTokenAmount(string token)
        {
            return _tokens
                .GetValueOrDefault(token)
                ?.Amount ?? 0;
        }

        public bool ContainsToken(string token) => _tokens.ContainsKey(token);

        public InventoryProxyState ToProxyState()
        {
            return new InventoryProxyState();
        }
    }

    internal class PlayerToken
    {
        public PublicKey TokenAccount { get; set; }
        public PublicKey Mint { get; set; }
        public string ContentId { get; set; }
        public long Amount { get; set; }
    }
}
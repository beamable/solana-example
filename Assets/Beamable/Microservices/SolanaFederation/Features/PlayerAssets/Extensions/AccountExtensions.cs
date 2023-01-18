using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.PlayerAssets.Extensions
{
    public static class AccountExtensions
    {
        public static Mint ToMint(this Account account, string contentId)
        {
            return new Mint
            {
                ContentId = contentId,
                PublicKey = account.PublicKey
            };
        }
    }
}
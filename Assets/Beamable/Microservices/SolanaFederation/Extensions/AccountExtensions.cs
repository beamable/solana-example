using Beamable.Microservices.SolanaFederation.Storage.Models;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Extensions
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
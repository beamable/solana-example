using System;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Solnet.Programs.Utilities;
using Solnet.Rpc;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions
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
        
        public static async Task Airdrop(this Account account, int amount)
        {
            BeamableLogger.Log("Requesting airdrop of {Amount} to {PublicKey}", amount, account.PublicKey.Key);
            var rpcClient = ClientFactory.GetClient(Configuration.SolanaCluster, null, null, null);
            try
            {
                await rpcClient.RequestAirdropAsync(account.PublicKey.Key, SolHelper.ConvertToLamports(amount));
            }
            catch (Exception ex)
            {
                BeamableLogger.LogError(ex);
            }
        }
    }
}
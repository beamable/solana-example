using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Transaction
{
    internal static class TransactionExecutor
    {
        public static async Task<string> Execute(IEnumerable<TransactionInstruction> instructions, Wallet realmWallet)
        {
            var blockHash = await SolanaRpcClient.GetLatestBlockHashAsync();
            
            var transactionBuilder = new TransactionBuilder()
                .SetFeePayer(realmWallet.Account.PublicKey)
                .SetRecentBlockHash(blockHash);
            
            instructions
                .ToList()
                .ForEach(instruction => transactionBuilder.AddInstruction(instruction));

            var transaction = transactionBuilder.Build(realmWallet.Account);
            return await SolanaRpcClient.SendTransactionAsync(transaction);
        }
    }
}
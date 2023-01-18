using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage;
using Beamable.Microservices.SolanaFederation.Features.Minting.Storage.Models;
using Beamable.Microservices.SolanaFederation.Features.PlayerAssets;
using Beamable.Microservices.SolanaFederation.Features.PlayerAssets.Extensions;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using MongoDB.Driver;
using Solnet.Metaplex;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
    public class MintingService
    {
        public static async Task<Mint> GetOrCreateMint(IMongoDatabase db, string contentId)
        {
            var maybeExistingMing = await MintCollection.Get(db, contentId);
            if (maybeExistingMing is not null)
            {
                return maybeExistingMing;
            }

            var realmWallet = await WalletService.GetRealmWallet(db);
            var mintAccount = await CreateMint(contentId, realmWallet.Account);
            var mint = mintAccount.ToMint(contentId);
            try
            {
                await MintCollection.Insert(db, mint);
                return mint;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                BeamableLogger.LogWarning("Mint for {ContentId} already created by another instance. Retrying fetch.",
                    contentId);
                return await GetOrCreateMint(db, contentId);
            }
        }

        private static async Task<Account> CreateMint(string contentId, Account owner)
        {
            var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.MintAccountDataSize
            );

            var mintAccount = new Account();

            BeamableLogger.Log("Creating mint {MintAccount} for {ContentId} with {LamportAmount} lamport balance.",
                mintAccount.PublicKey.Key, contentId, minBalanceForExemption);

            var blockHash = await SolanaRpcClient.GetLatestBlockHashAsync();

            // Calculate program derived metadata
            PublicKey.TryFindProgramAddress(
                new List<byte[]>()
                {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount.PublicKey
                },
                MetadataProgram.ProgramIdKey,
                out var metadataAddress,
                out _
            );

            var createMintTransaction = new TransactionBuilder()
                .SetFeePayer(owner.PublicKey)
                .SetRecentBlockHash(blockHash)
                .AddInstruction(
                    SystemProgram
                        .CreateAccount( // Create an account for the mint/token with lamport balance for rent exemption
                            owner.PublicKey,
                            mintAccount.PublicKey,
                            minBalanceForExemption,
                            TokenProgram.MintAccountDataSize,
                            TokenProgram.ProgramIdKey
                        )
                )
                .AddInstruction(
                    TokenProgram.InitializeMint( // Initialize mint - make it a token
                        mintAccount,
                        0,
                        owner.PublicKey,
                        owner.PublicKey
                    )
                )
                .AddInstruction(
                    MetadataProgram
                        .CreateMetadataAccount( // Create a metadata account for assigning a "name" to the token
                            metadataAddress,
                            mintAccount.PublicKey,
                            owner.PublicKey,
                            owner.PublicKey,
                            owner.PublicKey,
                            new MetadataParameters
                            {
                                name = contentId,
                                symbol = "",
                                uri = ""
                            },
                            true,
                            false
                        )
                )
                .Build(new List<Account> { owner, mintAccount });

            var response = await SolanaRpcClient.SendTransactionAsync(createMintTransaction);
            
            BeamableLogger.Log("Successfully created mint {MintAccount} for {ContentId} with transaction {Transaction}",
                mintAccount.PublicKey.Key, contentId, response);

            return mintAccount;
        }
    }
}
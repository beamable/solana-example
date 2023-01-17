using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Extensions;
using Beamable.Microservices.SolanaFederation.Storage;
using Beamable.Microservices.SolanaFederation.Storage.Models;
using MongoDB.Driver;
using Solnet.Metaplex;
using Solnet.Programs;
using Solnet.Rpc.Builders;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Services
{
    public class TokenService
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
            var minBalanceForExemptionResult = await SolanaRpc.Client.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.MintAccountDataSize,
                Commitment.Confirmed
            );

            var mintAccount = new Account();

            BeamableLogger.Log("Creating mint {MintAccount} for {ContentId} with {LamportAmount} lamport balance.",
                mintAccount.PublicKey.Key, contentId, minBalanceForExemptionResult.Result);

            var blockHashResult = await SolanaRpc.Client.GetLatestBlockHashAsync();
            var blockHash = blockHashResult.Result.Value.Blockhash;

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
                            minBalanceForExemptionResult.Result,
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

            var response = await SolanaRpc.Client.SendTransactionAsync(createMintTransaction);
            response.ThrowIfError();
            
            BeamableLogger.Log("Successfully created mint {MintAccount} for {ContentId} with transaction {Transaction}",
                mintAccount.PublicKey.Key, contentId, response.Result);

            return mintAccount;
        }
    }
}
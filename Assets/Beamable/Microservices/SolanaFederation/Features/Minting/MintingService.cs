using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction;
using Beamable.Microservices.SolanaFederation.Features.Wallets.Extensions;
using Solana.Unity.Metaplex;
using Solana.Unity.Programs;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Minting
{
	public static class MintingService
	{
		public static async ValueTask EnsureMinted(IList<string> contentIds, Wallet realmWallet)
		{
			var minBalanceForExemption = await SolanaRpcClient.GetMinimumBalanceForRentExemptionAsync(
				TokenProgram.MintAccountDataSize
			);

			foreach (var contentId in contentIds)
			{
				var mintAccount = realmWallet.GetAccount(contentId);
				var tokenMintInfo = await SolanaRpcClient.GetTokenMintInfoAsync(mintAccount.PublicKey);

				if (tokenMintInfo is null)
				{
					BeamableLogger.Log("{ContentId} is not minted. Minting token {TokenAddress}", contentId,
						mintAccount.PublicKey.Key);

					// Calculate a program derived metadata
					PublicKey.TryFindProgramAddress(
						new List<byte[]>
						{
							Encoding.UTF8.GetBytes("metadata"),
							MetadataProgram.ProgramIdKey,
							mintAccount.PublicKey
						},
						MetadataProgram.ProgramIdKey,
						out var metadataAddress,
						out _
					);

					TransactionManager.AddInstruction(SystemProgram
						.CreateAccount( // Create an account for the mint/token with lamport balance for rent exemption
							realmWallet.Account,
							mintAccount.PublicKey,
							minBalanceForExemption,
							TokenProgram.MintAccountDataSize,
							TokenProgram.ProgramIdKey
						));

					TransactionManager.AddInstruction(TokenProgram.InitializeMint( // Initialize mint - make it a token
						mintAccount.PublicKey,
						0,
						realmWallet.Account.PublicKey,
						realmWallet.Account.PublicKey
					));

					TransactionManager.AddInstruction(MetadataProgram
						.CreateMetadataAccountV3( // Create a metadata account for assigning a "name" to the token
							metadataAddress,
							mintAccount.PublicKey,
							realmWallet.Account.PublicKey,
							realmWallet.Account.PublicKey,
							realmWallet.Account.PublicKey,
							new MetadataV3
							{
								name = contentId,
								symbol = "",
								uri = ""
							},
							false,
							true
						));

					TransactionManager.AddSigner(mintAccount);
				}
			}
		}
	}
}
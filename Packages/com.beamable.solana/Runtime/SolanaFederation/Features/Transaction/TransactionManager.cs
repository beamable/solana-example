using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.SolanaRpc;
using Beamable.Microservices.SolanaFederation.Features.Transaction.Exceptions;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Transaction
{
	internal static class TransactionManager
	{
		private static readonly AsyncLocal<TransactionState> TransactionState = new();

		public static TransactionState InitTransaction()
		{
			TransactionState.Value = new TransactionState();
			return TransactionState.Value;
		}

		public static bool HasInstructions()
		{
			return TransactionState.Value?.Instructions.Any() == true;
		}

		public static void AddInstruction(TransactionInstruction transactionInstruction)
		{
			if (TransactionState.Value is null) throw new TransactionException("Transaction is not initialized");
			TransactionState.Value.Instructions.Add(transactionInstruction);
		}

		public static void AddInstructions(IEnumerable<TransactionInstruction> transactionInstructions)
		{
			if (TransactionState.Value is null) throw new TransactionException("Transaction is not initialized");
			TransactionState.Value.Instructions.AddRange(transactionInstructions);
		}

		public static void AddSigner(Account signer)
		{
			if (TransactionState.Value is null) throw new TransactionException("Transaction is not initialized");
			if (!TransactionState.Value.Signers.Contains(signer))
				TransactionState.Value.Signers.Add(signer);
		}

		public static void AddSuccessCallback(Func<string, Task> callback)
		{
			if (TransactionState.Value is null) throw new TransactionException("Transaction is not initialized");
			TransactionState.Value.SuccessCallbacks.Add(callback);
		}

		public static async Task<string> Execute(Wallet feePayer)
		{
			if (TransactionState.Value is null) throw new TransactionException("Transaction is not initialized");

			if (!TransactionState.Value.Instructions.Any())
			{
				BeamableLogger.LogWarning("No transaction instructions were generated for the request");
				return "";
			}

			var blockHash = await SolanaRpcClient.GetLatestBlockHashAsync();

			var transactionBuilder = new TransactionBuilder()
				.SetFeePayer(feePayer.Account.PublicKey)
				.SetRecentBlockHash(blockHash);

			AddSigner(feePayer.Account);

			TransactionState.Value.Instructions
				.ForEach(instruction => transactionBuilder.AddInstruction(instruction));

			var transaction = transactionBuilder.Build(TransactionState.Value.Signers.ToList());
			BeamableLogger.Log("Generated transaction: {TransactionBytes}", Convert.ToBase64String(transaction));
			var transactionId = await SolanaRpcClient.SendTransactionAsync(transaction);
			BeamableLogger.Log("Transaction {TransactionId} processed successfully", transactionId);

			foreach (var callback in TransactionState.Value.SuccessCallbacks) await callback(transactionId);

			return transactionId;
		}
	}
}
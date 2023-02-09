using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Transaction
{
	internal class TransactionState
	{
		public TransactionState()
		{
			Instructions = new List<TransactionInstruction>();
			Signers = new List<Account>();
			SuccessCallbacks = new List<Func<string, Task>>();
		}

		public List<TransactionInstruction> Instructions { get; }
		public List<Account> Signers { get; }
		public List<Func<string, Task>> SuccessCallbacks { get; }
	}
}
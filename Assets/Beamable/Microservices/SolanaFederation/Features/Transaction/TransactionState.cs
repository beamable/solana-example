using System.Collections.Generic;
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
		}

		public List<TransactionInstruction> Instructions { get; }
		public List<Account> Signers { get; }
	}
}
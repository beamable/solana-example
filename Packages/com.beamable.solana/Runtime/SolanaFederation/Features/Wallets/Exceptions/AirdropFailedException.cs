using System;

namespace Beamable.Microservices.SolanaFederation.Features.Wallets.Exceptions
{
	public class AirdropFailedException : Exception
	{
		public AirdropFailedException(string message) : base(message)
		{
		}
	}
}
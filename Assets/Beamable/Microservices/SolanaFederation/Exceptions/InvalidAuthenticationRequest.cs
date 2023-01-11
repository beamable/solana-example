using System;

namespace Assets.Beamable.Microservices.SolanaFederation.Exceptions
{
	class InvalidAuthenticationRequest : Exception
	{
		public InvalidAuthenticationRequest(string message) : base(message)
		{
		}
	}
}

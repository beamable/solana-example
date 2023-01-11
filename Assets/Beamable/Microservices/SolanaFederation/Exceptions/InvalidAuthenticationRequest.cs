using Beamable.Server;
using System.Net;

namespace Assets.Beamable.Microservices.SolanaFederation.Exceptions
{
	class InvalidAuthenticationRequest : MicroserviceException
	{
		public InvalidAuthenticationRequest(string message) : base((int)HttpStatusCode.BadRequest, "InvalidAuthenticationRequest", message)
		{
		}
	}
}

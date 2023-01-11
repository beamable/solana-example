using Beamable.Server;
using System.Net;

namespace Assets.Beamable.Microservices.SolanaFederation.Exceptions
{
	class UnauthorizedException : MicroserviceException

	{
		public UnauthorizedException() : base((int)HttpStatusCode.Unauthorized, "Unauthorized", "")
		{
		}
	}
}

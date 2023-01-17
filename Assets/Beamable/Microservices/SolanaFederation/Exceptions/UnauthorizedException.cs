using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Exceptions
{
    class UnauthorizedException : MicroserviceException

    {
        public UnauthorizedException() : base((int)HttpStatusCode.Unauthorized, "Unauthorized", "")
        {
        }
    }
}
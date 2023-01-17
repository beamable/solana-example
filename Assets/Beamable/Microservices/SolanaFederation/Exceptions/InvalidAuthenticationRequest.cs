using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Exceptions
{
    class InvalidAuthenticationRequest : MicroserviceException
    {
        public InvalidAuthenticationRequest(string message) : base((int)HttpStatusCode.BadRequest,
            "InvalidAuthenticationRequest", message)
        {
        }
    }
}
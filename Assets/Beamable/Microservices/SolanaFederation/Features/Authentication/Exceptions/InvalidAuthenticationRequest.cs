using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions
{
    class InvalidAuthenticationRequest : MicroserviceException
    {
        public InvalidAuthenticationRequest(string message) : base((int)HttpStatusCode.BadRequest,
            "InvalidAuthenticationRequest", message)
        {
        }
    }
}
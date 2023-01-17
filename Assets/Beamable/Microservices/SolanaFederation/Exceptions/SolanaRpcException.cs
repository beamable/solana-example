using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Exceptions
{
    class SolanaRpcException : MicroserviceException
    {
        public SolanaRpcException(string message) : base((int)HttpStatusCode.InternalServerError, "SolanaRpcError",
            message)
        {
        }
    }
}
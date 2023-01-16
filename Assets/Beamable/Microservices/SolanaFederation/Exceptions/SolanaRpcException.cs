using Beamable.Server;
using System.Net;

namespace Assets.Beamable.Microservices.SolanaFederation.Exceptions
{
	class SolanaRpcException : MicroserviceException
	{
		public SolanaRpcException(string message) : base((int)HttpStatusCode.InternalServerError, "SolanaRpcError", message)
		{
		}
	}
}

using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Features.SolanaRpc.Exceptions
{
	internal class SolanaRpcException : MicroserviceException
	{
		public SolanaRpcException(string message) : base((int)HttpStatusCode.InternalServerError, "SolanaRpcError",
			message)
		{
		}
	}
}
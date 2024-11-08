using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Features.Transaction.Exceptions
{
	internal class TransactionException : MicroserviceException
	{
		public TransactionException(string message) : base(
			(int)HttpStatusCode.BadRequest, "TransactionError", message)
		{
		}
	}
}
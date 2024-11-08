using Beamable.Server;
using System.Threading.Tasks;

namespace Beamable.Microservices
{
	public class Program
	{
		/// <summary>
		/// The entry point for the <see cref="SolanaFederation"/> service.
		/// </summary>
		public static async Task Main()
		{
			// inject data from the CLI.
			await MicroserviceBootstrapper.Prepare<SolanaFederation.SolanaFederation>();
			
			// run the Microservice code
			await MicroserviceBootstrapper.Start<SolanaFederation.SolanaFederation>();
		}
	}
}

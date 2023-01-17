using Beamable.Microservices.SolanaFederation.Services;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation
{
    static class Startup
    {
        public static IServiceBuilder ConfigureSolanaServices(this IServiceBuilder serviceBuilder)
        {
            serviceBuilder.AddSingleton(_ => new AuthenticationService());
            serviceBuilder.AddSingleton(_ => new WalletService());
            return serviceBuilder;
        }
    }
}
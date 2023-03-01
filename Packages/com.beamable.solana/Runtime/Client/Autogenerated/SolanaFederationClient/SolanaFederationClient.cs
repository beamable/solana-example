//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Beamable.Server.Clients
{
    using System;
    using Beamable.Platform.SDK;
    using Beamable.Server;
    
    
    /// <summary> A generated client for <see cref="Beamable.Microservices.SolanaFederation.SolanaFederation"/> </summary
    public sealed class SolanaFederationClient : MicroserviceClient, Beamable.Common.IHaveServiceName, Beamable.Common.ISupportsFederatedLogin<Beamable.Common.SolanaCloudIdentity>, Beamable.Common.ISupportsFederatedInventory<Beamable.Common.SolanaCloudIdentity>
    {
        
        public SolanaFederationClient(BeamContext context = null) : 
                base(context)
        {
        }
        
        public string ServiceName
        {
            get
            {
                return "SolanaFederation";
            }
        }
    }
    
    internal sealed class MicroserviceParametersSolanaFederationClient
    {
    }
    
    [BeamContextSystemAttribute()]
    public static class ExtensionsForSolanaFederationClient
    {
        
        [Beamable.Common.Dependencies.RegisterBeamableDependenciesAttribute()]
        public static void RegisterService(Beamable.Common.Dependencies.IDependencyBuilder builder)
        {
            builder.AddScoped<SolanaFederationClient>();
        }
        
        public static SolanaFederationClient SolanaFederation(this Beamable.Server.MicroserviceClients clients)
        {
            return clients.GetClient<SolanaFederationClient>();
        }
    }
}
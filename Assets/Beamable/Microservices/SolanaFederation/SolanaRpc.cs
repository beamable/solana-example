using Solnet.Rpc;

namespace Beamable.Microservices.SolanaFederation
{
    public static class SolanaRpc
    {
        public static readonly IRpcClient Client = ClientFactory.GetClient(Configuration.SolanaCluster, null, null, null);
    }
}
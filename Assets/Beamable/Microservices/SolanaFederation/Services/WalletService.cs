using Solana.Unity.Rpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Beamable.Microservices.SolanaFederation.Services
{
	public class WalletService
	{
		private readonly Cluster _cluster;
		private readonly IRpcClient _rpcClient;

		public WalletService(Cluster cluster)
		{
			_cluster = cluster;
			_rpcClient = ClientFactory.GetClient(Cluster.DevNet);
		}

		public void Test()
		{
			
		}

	}
}

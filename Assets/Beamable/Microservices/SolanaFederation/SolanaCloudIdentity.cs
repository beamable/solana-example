using Beamable.Common;

namespace Beamable.Microservices.SolanaFederation
{
	public class SolanaCloudIdentity : IThirdPartyCloudIdentity
	{
		public string UniqueName => "solana";
	}
}
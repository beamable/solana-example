using System;
using System.Text;
using Beamable.Server;
using Solana.Unity.Wallet;

namespace Beamable.Microservices
{
	[Microservice("SolanaAuthMS")]
	public class SolanaAuthMS : Microservice
	{
		// [ClientCallable]
		// public ExternalAuthenticationResponse Authorize(string token, string challenge, string solution)
		// {
		//
		// 	if (challenge == "ul" || solution == "ul")
		// 	{
		// 		return new ExternalAuthenticationResponse {challenge = Guid.NewGuid().ToString(), challenge_ttl = 60};
		// 	}
		//
		// 	if (Verify(token, challenge, solution))
		// 	{
		// 		return new ExternalAuthenticationResponse {user_id = token};
		// 	}
		//
		// 	return new ExternalAuthenticationResponse {challenge = Guid.NewGuid().ToString(), challenge_ttl = 60};
		// }
		
		public bool Verify(string publicKey, string challenge, string signature)
		{
			byte[] challengeBytes = Encoding.UTF8.GetBytes(challenge);
			byte[] signatureBytes = Convert.FromBase64String(signature);

			return new PublicKey(publicKey).Verify(challengeBytes, signatureBytes);
		}
	}
}

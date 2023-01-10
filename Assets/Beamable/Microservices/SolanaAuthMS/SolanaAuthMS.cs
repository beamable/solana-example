using System;
using System.Text;
using Beamable.Server;
using Beamable.Common.Api.Auth;
using Solana.Unity.Wallet;
using UnityEngine;

namespace Beamable.Microservices
{
	[Microservice("SolanaAuthMS")]
	public class SolanaAuthMS : Microservice
	{
         [ClientCallable("Solana/authenticate")]
         public ExternalAuthenticationResponse Authenticate(string token, string challenge, string solution)
         {
         	Debug.Log($"Token: {token}, challenge: {challenge}, solution: {solution}");
        
         	if (!IsValid(challenge, solution))
         	{
         		Debug.Log("Challenge or solution is empty");
         		return new ExternalAuthenticationResponse {challenge = Guid.NewGuid().ToString(), challenge_ttl = 60};
         	}
        
         	if (Verify(token, challenge, solution))
         	{
         		Debug.Log("Verified");
         		return new ExternalAuthenticationResponse { user_id = token};
         	}
        
         	Debug.Log("Not verified");
         	return new ExternalAuthenticationResponse {challenge = Guid.NewGuid().ToString(), challenge_ttl = 60};
         }

        private bool IsValid(string challenge, string solution)
		{
			return !string.IsNullOrEmpty(challenge) && !string.IsNullOrEmpty(solution);
		}

		private bool Verify(string publicKey, string challenge, string signature)
		{
			byte[] challengeBytes = Encoding.UTF8.GetBytes(challenge);
			byte[] signatureBytes = Convert.FromBase64String(signature);

			return new PublicKey(publicKey).Verify(challengeBytes, signatureBytes);
		}
	}
}

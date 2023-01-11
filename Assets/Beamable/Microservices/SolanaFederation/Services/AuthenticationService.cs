using Solana.Unity.Wallet;
using System;
using System.Text;

namespace Assets.Beamable.Microservices.SolanaFederation.Services
{
	public class AuthenticationService
	{
		public bool IsSignatureValid(string publicKey, string challenge, string signature)
		{
			byte[] challengeBytes = Encoding.UTF8.GetBytes(challenge);
			byte[] signatureBytes = Convert.FromBase64String(signature);

			return new PublicKey(publicKey).Verify(challengeBytes, signatureBytes);
		}
	}
}

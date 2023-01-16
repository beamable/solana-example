using Assets.Beamable.Microservices.SolanaFederation.Exceptions;
using Beamable.Common;
using Solnet.Wallet;
using System;
using System.Text;

namespace Assets.Beamable.Microservices.SolanaFederation.Services
{
	public class AuthenticationService
	{
		public static bool IsSignatureValid(string publicKey, string challenge, string signature)
		{
			try
			{
				byte[] challengeBytes = Encoding.UTF8.GetBytes(challenge);
				byte[] signatureBytes = Convert.FromBase64String(signature);

				return new PublicKey(publicKey).Verify(challengeBytes, signatureBytes);
			}
			catch (Exception ex)
			{
				BeamableLogger.LogError(ex);
				throw new UnauthorizedException();
			}
		}
	}
}

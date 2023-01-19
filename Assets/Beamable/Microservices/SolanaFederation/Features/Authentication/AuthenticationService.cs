using System;
using System.Text;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions;
using Solana.Unity.Wallet;

namespace Beamable.Microservices.SolanaFederation.Features.Authentication
{
    public class AuthenticationService
    {
        public static bool IsSignatureValid(string publicKey, string challenge, string signature)
        {
            try
            {
                var challengeBytes = Encoding.UTF8.GetBytes(challenge);
                var signatureBytes = Convert.FromBase64String(signature);

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
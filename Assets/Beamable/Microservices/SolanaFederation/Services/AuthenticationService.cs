using System;
using System.Text;
using Beamable.Common;
using Beamable.Microservices.SolanaFederation.Exceptions;
using Solnet.Wallet;

namespace Beamable.Microservices.SolanaFederation.Services
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
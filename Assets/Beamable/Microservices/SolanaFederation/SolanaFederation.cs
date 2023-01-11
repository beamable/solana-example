using Assets.Beamable.Microservices.SolanaFederation.Exceptions;
using Assets.Beamable.Microservices.SolanaFederation.Services;
using Beamable.Common;
using Beamable.Server;
using System;

namespace Beamable.Microservices
{
	[Microservice("SolanaFederation")]
	public class SolanaFederation : Microservice
	{
		private const int AUTHENTICATION_CHALLENGE_TTL_SEC = 600;

		private readonly AuthenticationService _authenticationService;

		public SolanaFederation(AuthenticationService authenticationService)
		{
			_authenticationService = authenticationService;
		}

		[ConfigureServices]
		public static void ConfigureSecond(IServiceBuilder serviceBuilder)
		{
			serviceBuilder.AddSingleton(_ => new AuthenticationService());
		}

		[ClientCallable("authenticate")]
		public ExternalAuthenticationResponse Authenticate(string token, string challenge, string solution)
		{
			if (string.IsNullOrEmpty(token))
			{
				BeamableLogger.LogError("We didn't receive a token (public key)");
				throw new InvalidAuthenticationRequest("Token (public key) is required");
			}

			if (!string.IsNullOrEmpty(challenge) && !string.IsNullOrEmpty(solution))
			{
				// Verify the solution
				if (_authenticationService.IsSignatureValid(token, challenge, solution))
				{
					// User identity confirmed
					return new ExternalAuthenticationResponse { user_id = token };
				}
				else
				{
					// Signature is invalid, user identity isn't confirmed
					BeamableLogger.LogWarning("Invalid signature {signature} for challenge {challenge} and wallet {wallet}", solution, challenge, token);
					return new ExternalAuthenticationResponse { user_id = null };
				}
			}
			else
			{
				// Generate a challenge
				return new ExternalAuthenticationResponse { challenge = Guid.NewGuid().ToString(), challenge_ttl = AUTHENTICATION_CHALLENGE_TTL_SEC };
			}
		}
	}
}

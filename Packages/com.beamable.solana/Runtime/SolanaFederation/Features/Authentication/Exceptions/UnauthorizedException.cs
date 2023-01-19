﻿using System.Net;
using Beamable.Server;

namespace Beamable.Microservices.SolanaFederation.Features.Authentication.Exceptions
{
	internal class UnauthorizedException : MicroserviceException

	{
		public UnauthorizedException() : base((int)HttpStatusCode.Unauthorized, "Unauthorized", "")
		{
		}
	}
}
using System;
using System.Collections.Generic;

namespace Beamable.Common.Api.Inventory
{
	[Serializable]
	public class SaveBinaryResponse
	{
		public List<BinaryReference> binary;
	}
}
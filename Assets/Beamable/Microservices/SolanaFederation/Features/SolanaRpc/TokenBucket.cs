using System;
using System.Threading;
using System.Threading.Tasks;

namespace Assets.Beamable.Microservices.SolanaFederation.Features.SolanaRpc
{
	internal class TokenBucket
	{
		private readonly int _refillInterval;
		private readonly SemaphoreSlim _semaphore;
		private readonly Timer _timer;
		private readonly int _tokenCount;

		public TokenBucket(int tokenCount, int refillInterval)
		{
			_tokenCount = tokenCount;
			_refillInterval = refillInterval;
			_semaphore = new SemaphoreSlim(_tokenCount);
			_timer = new Timer(Refill, null, TimeSpan.FromMilliseconds(refillInterval),
				TimeSpan.FromMilliseconds(refillInterval));
		}

		private void Refill(object state)
		{
			var releaseCount = _tokenCount - _semaphore.CurrentCount;
			if (releaseCount > 0)
				_semaphore.Release(releaseCount);
		}

		public async Task<bool> TryConsume()
		{
			return await _semaphore.WaitAsync(0);
		}
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface ITwitchMemeAlertsAutoService
	{
		event Action<string> OnMemesReceived;

		event Action<string> OnMemesNotReceived;

		event Action<string> OnSupporterNotFound;

		event Action<string> OnSupporterLoading;

		event Action<string> OnSupporterLoaded;

		Task<bool> CheckToken(string token, CancellationToken cancellationToken = default);

		Task RewardAllAsync(int value, CancellationToken cancellationToken = default);

		Task<int> Work(string channel, string token, string rewards, CancellationToken cancellationToken = default);
	}
}
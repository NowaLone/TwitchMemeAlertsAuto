using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface ITwitchMemeAlertsAutoService
	{
		event Action<string> OnMemesReceived;

		event Action<string> OnMemesNotReceived;

		event Action<string> OnSupporterNotFound;

		event Action OnSupporterLoading;

		event Action<int> OnSupporterLoaded;

		Task<bool> CheckToken(string token, CancellationToken cancellationToken = default);
		Task<Current> GetMemeAlertsId(HttpClient memeAlertsClient, CancellationToken cancellationToken = default);

		Task<int> Work(string channel, string token, string rewards, CancellationToken cancellationToken = default);
	}
}
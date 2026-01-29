using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface IRewardsService
	{
		Task StartAsync(IDictionary<string, int> rewards, string channel, CancellationToken cancellationToken = default);

		Task StopAsync(CancellationToken cancellationToken);
	}
}
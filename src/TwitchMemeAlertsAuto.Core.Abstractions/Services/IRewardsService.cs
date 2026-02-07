using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public interface IRewardsService
	{
		Task StartAsync(IDictionary<string, int> rewards, string channel, bool tryRewardWithWrongNickname = false, CancellationToken cancellationToken = default);

		Task StopAsync(CancellationToken cancellationToken);
	}
}
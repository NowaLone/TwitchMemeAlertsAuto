using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public interface IWebsocketHostedService
	{
		Task StartAsync(CancellationToken cancellationToken = default);

		Task StopAsync(CancellationToken cancellationToken = default);
	}
}
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public interface IMemeAlertsService
	{
		Task<bool> CheckToken(string token, CancellationToken cancellationToken = default);

		Task<List<Supporter>> GetSupportersAsync(CancellationToken cancellationToken = default);

		Task<Current> GetCurrent(CancellationToken cancellationToken = default);

		Task<bool> GiveBonusAsync(Supporter supporter, int value, CancellationToken cancellationToken = default);

		Task<List<Event>> GetEventsAsync(CancellationToken cancellationToken = default);

		Task<List<Sticker>> GetPersonalAreaCatalogueAsync(CancellationToken cancellationToken = default);

		Task<List<Sticker>> GetStreamerAreaCatalogueAsync(CancellationToken cancellationToken = default);

		Task<bool> SendMemeAsync(Sticker sticker, CancellationToken cancellationToken = default);
		Task<Supporter> GetStreamerAsSupporterAsync(CancellationToken cancellationToken = default);
	}
}
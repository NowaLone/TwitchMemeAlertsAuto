using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface ITwitchMemeAlertsAutoService
	{
		Task<bool> AddMemesAsync(Supporter supporter, int value, CancellationToken cancellationToken = default);

		Task<bool> CheckToken(string token, CancellationToken cancellationToken = default);

		Task<List<Supporter>> GetDataAsync(CancellationToken cancellationToken = default);

		Task<Current> GetMemeAlertsId(CancellationToken cancellationToken = default);
	}
}
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public interface ITwitchOAuthService : IDisposable
	{
		/// <summary>
		/// Authenticates and returns a valid access token.
		/// Loads token from settings, validates it, refreshes if needed, or starts device code flow if necessary.
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns>Valid access token</returns>
		/// <exception cref="Exception">Thrown if authentication fails</exception>
		Task<string> AuthenticateAsync(CancellationToken cancellationToken = default);
	}
}

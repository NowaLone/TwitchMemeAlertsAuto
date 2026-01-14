using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface ITwitchOAuthService : IDisposable
	{
		Task<string> AuthenticateAsync(string[] scopes = null, CancellationToken cancellationToken = default);

		Task<string> AuthenticateAsync(string refreshToken, CancellationToken cancellationToken = default);

		Task<TwitchValidationResponse> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
	}
}
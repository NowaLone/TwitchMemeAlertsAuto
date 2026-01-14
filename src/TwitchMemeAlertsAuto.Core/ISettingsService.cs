using System;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public interface ISettingsService
	{
		Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);

		Task<string> GetTwitchOAuthTokenAsync(CancellationToken cancellationToken = default);

		Task<string> GetTwitchRefreshTokenAsync(CancellationToken cancellationToken = default);

		Task<DateTimeOffset> GetTwitchExpiresInAsync(CancellationToken cancellationToken = default);

		Task<string> GetTwitchUserIdAsync(CancellationToken cancellationToken = default);

		Task<string> GetMemeAlertsTokenAsync(CancellationToken cancellationToken = default);

		Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default);

		Task SetTwitchOAuthTokenAsync(string oauthToken, CancellationToken cancellationToken = default);

		Task SetTwitchRefreshTokenAsync(string oauthToken, CancellationToken cancellationToken = default);

		Task SetTwitchExpiresInAsync(DateTimeOffset expiresIn, CancellationToken cancellationToken = default);

		Task SetTwitchUserIdAsync(string userId, CancellationToken cancellationToken = default);

		Task SetMemeAlertsTokenAsync(string token, CancellationToken cancellationToken = default);
	}
}
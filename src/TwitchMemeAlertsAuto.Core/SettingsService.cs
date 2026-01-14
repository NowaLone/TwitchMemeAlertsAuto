using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core
{
	public class SettingsService : ISettingsService
	{
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
		private readonly ILogger<SettingsService> logger;

		public SettingsService(IDbContextFactory<TmaaDbContext> dbContextFactory, ILogger<SettingsService> logger)
		{
			this.dbContextFactory = dbContextFactory;
			this.logger = logger;
		}

		public async Task<string> GetTwitchOAuthTokenAsync(CancellationToken cancellationToken = default)
		{
			return await GetSettingAsync("Twitch:OAuthToken", string.Empty, cancellationToken);
		}

		public async Task SetTwitchOAuthTokenAsync(string oauthToken, CancellationToken cancellationToken = default)
		{
			await SetSettingAsync("Twitch:OAuthToken", oauthToken ?? string.Empty, cancellationToken);
		}

		public Task<string> GetTwitchRefreshTokenAsync(CancellationToken cancellationToken = default)
		{
			return GetSettingAsync("Twitch:RefreshToken", string.Empty, cancellationToken);
		}

		public Task SetTwitchRefreshTokenAsync(string oauthToken, CancellationToken cancellationToken = default)
		{
			return SetSettingAsync("Twitch:RefreshToken", oauthToken ?? string.Empty, cancellationToken);
		}

		public async Task<DateTimeOffset> GetTwitchExpiresInAsync(CancellationToken cancellationToken = default)
		{
			return await GetSettingAsync("Twitch:ExpiresIn", DateTimeOffset.MinValue, cancellationToken).ConfigureAwait(false);
		}

		public Task SetTwitchExpiresInAsync(DateTimeOffset expiresIn, CancellationToken cancellationToken = default)
		{
			return SetSettingAsync("Twitch:ExpiresIn", expiresIn, cancellationToken);
		}

		public Task<string> GetTwitchUserIdAsync(CancellationToken cancellationToken = default)
		{
			return GetSettingAsync("Twitch:UserId", string.Empty, cancellationToken);
		}

		public Task SetTwitchUserIdAsync(string userId, CancellationToken cancellationToken = default)
		{
			return SetSettingAsync("Twitch:UserId", userId ?? string.Empty, cancellationToken);
		}

		public Task<string> GetMemeAlertsTokenAsync(CancellationToken cancellationToken = default)
		{
			return GetSettingAsync("MemeAlerts:Token", string.Empty, cancellationToken);
		}

		public Task SetMemeAlertsTokenAsync(string token, CancellationToken cancellationToken = default)
		{
			return SetSettingAsync("MemeAlerts:Token", token ?? string.Empty, cancellationToken);
		}

		public async Task<T> GetSettingAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
		{
			using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var setting = await dbContext.Settings
				.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

			if (setting == null)
			{
				logger.LogDebug("Setting '{Key}' not found, creating with default value: {DefaultValue}", key, defaultValue);

				// Create the setting with default value
				var newSetting = new Setting
				{
					Key = key,
					Value = Convert.ToString(defaultValue, CultureInfo.InvariantCulture),
				};
				dbContext.Settings.Add(newSetting);
				await dbContext.SaveChangesAsync(cancellationToken);

				return defaultValue;
			}

			try
			{
				return (T)Convert.ChangeType(setting.Value, typeof(T), CultureInfo.InvariantCulture);
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to convert setting '{Key}' value '{Value}' to type {Type}, returning default value: {DefaultValue}",
					key, setting.Value, typeof(T).Name, defaultValue);
				return defaultValue;
			}
		}

		public async Task SetSettingAsync<T>(string key, T value, CancellationToken cancellationToken = default)
		{
			using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var setting = await dbContext.Settings
				.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

			var stringValue = Convert.ToString(value, CultureInfo.InvariantCulture);

			if (setting == null)
			{
				setting = new Setting
				{
					Key = key,
					Value = stringValue,
				};
				dbContext.Settings.Add(setting);
				logger.LogDebug("Created new setting '{Key}' with value '{Value}'", key, stringValue);
			}
			else
			{
				setting.Value = stringValue;
				logger.LogDebug("Updated setting '{Key}' to value '{Value}'", key, stringValue);
			}

			await dbContext.SaveChangesAsync(cancellationToken);
		}
	}
}
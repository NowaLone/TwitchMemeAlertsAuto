using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public class TwitchTokenRefreshHostedService : BackgroundService
	{
		private static readonly TimeSpan MaxCheckInterval = TimeSpan.FromMinutes(5);
		private static readonly TimeSpan NoRefreshTokenRetryInterval = TimeSpan.FromMinutes(5);
		private static readonly TimeSpan RefreshFailureRetryInterval = TimeSpan.FromMinutes(1);

		private readonly IServiceScopeFactory serviceScopeFactory;
		private readonly ILogger<TwitchTokenRefreshHostedService> logger;

		public TwitchTokenRefreshHostedService(IServiceScopeFactory serviceScopeFactory, ILogger<TwitchTokenRefreshHostedService> logger)
		{
			this.serviceScopeFactory = serviceScopeFactory;
			this.logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var delay = await RunRefreshCycleAsync(stoppingToken).ConfigureAwait(false);
					if (delay > TimeSpan.Zero)
					{
						await Task.Delay(delay, stoppingToken).ConfigureAwait(false);
					}
				}
				catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
				{
					break;
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Unexpected error in Twitch token refresh loop");
					await Task.Delay(RefreshFailureRetryInterval, stoppingToken).ConfigureAwait(false);
				}
			}
		}

		private async Task<TimeSpan> RunRefreshCycleAsync(CancellationToken stoppingToken)
		{
			using var scope = serviceScopeFactory.CreateScope();
			var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
			var twitchOAuthService = scope.ServiceProvider.GetRequiredService<ITwitchOAuthService>();

			var refreshToken = await settingsService.GetTwitchRefreshTokenAsync(stoppingToken).ConfigureAwait(false);
			if (string.IsNullOrWhiteSpace(refreshToken))
			{
				return NoRefreshTokenRetryInterval;
			}

			var expiryTime = await settingsService.GetTwitchExpiresInAsync(stoppingToken).ConfigureAwait(false);
			var refreshAt = expiryTime.AddSeconds(-TwitchOAuthService.TokenExpiryBufferSeconds);
			var delayUntilRefresh = refreshAt - DateTimeOffset.UtcNow;

			if (delayUntilRefresh > TimeSpan.Zero)
			{
				return delayUntilRefresh > MaxCheckInterval ? MaxCheckInterval : delayUntilRefresh;
			}

			var previousToken = await settingsService.GetTwitchOAuthTokenAsync(stoppingToken).ConfigureAwait(false);
			var refreshedToken = await twitchOAuthService.TryRefreshTokenAsync(stoppingToken).ConfigureAwait(false);

			if (string.IsNullOrWhiteSpace(refreshedToken))
			{
				logger.LogWarning("Background Twitch token refresh failed");
				return RefreshFailureRetryInterval;
			}

			if (!string.Equals(previousToken, refreshedToken, StringComparison.Ordinal))
			{
				var userId = await settingsService.GetTwitchUserIdAsync(stoppingToken).ConfigureAwait(false);
				logger.LogInformation("Background Twitch token refresh succeeded, notifying subscribers");
				WeakReferenceMessenger.Default.Send(new TwitchTokenRefreshedMessage(refreshedToken, userId));
			}

			var nextExpiry = await settingsService.GetTwitchExpiresInAsync(stoppingToken).ConfigureAwait(false);
			var nextDelay = nextExpiry.AddSeconds(-TwitchOAuthService.TokenExpiryBufferSeconds) - DateTimeOffset.UtcNow;
			if (nextDelay <= TimeSpan.Zero)
			{
				return RefreshFailureRetryInterval;
			}

			return nextDelay > MaxCheckInterval ? MaxCheckInterval : nextDelay;
		}
	}
}

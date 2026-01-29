using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Interfaces;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class ConnectionViewModel : ObservableRecipient, IRecipient<RewardChangedMessage>
	{
		private readonly ISettingsService settingsService;
		private readonly IRewardsService rewardsService;
		private readonly ITwitchOAuthService twitchOAuthService;
		private readonly ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService;
		private readonly IDispatcherService dispatcherService;
		private readonly IServiceProvider serviceProvider;
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
		private readonly ILogger logger;

		private CancellationTokenSource cancellationTokenSource;

		public ConnectionViewModel()
		{
		}

		public ConnectionViewModel(ISettingsService settingsService, IRewardsService rewardsService, ITwitchOAuthService twitchOAuthService, ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService, IDispatcherService dispatcherService, IServiceProvider serviceProvider, IDbContextFactory<TmaaDbContext> dbContextFactory, ILogger<ConnectionViewModel> logger) : this()
		{
			this.settingsService = settingsService;
			this.rewardsService = rewardsService;
			this.twitchOAuthService = twitchOAuthService;
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.dispatcherService = dispatcherService;
			this.serviceProvider = serviceProvider;
			this.dbContextFactory = dbContextFactory;
			this.logger = logger;
		}

		[ObservableProperty]
		private bool isTwitchConnected;

		[ObservableProperty]
		private bool isMemeAlertsConnected;

		[NotifyCanExecuteChangedFor(nameof(ConnectTwitchCommand))]
		[ObservableProperty]
		private bool isCheckingTwitch;

		[NotifyCanExecuteChangedFor(nameof(ConnectMemeAlertsCommand))]
		[ObservableProperty]
		private bool isCheckingMemeAlerts;

		public async void Receive(RewardChangedMessage message)
		{
			if (IsTwitchConnected && IsMemeAlertsConnected)
			{
				await StartWork().ConfigureAwait(false);
			}
		}

		protected override async void OnActivated()
		{
			var Ttoken = await settingsService.GetTwitchOAuthTokenAsync().ConfigureAwait(false);

			if (!string.IsNullOrWhiteSpace(Ttoken))
			{
				ConnectTwitchCommand.Execute(default);
			}

			var Mtoken = await settingsService.GetMemeAlertsTokenAsync().ConfigureAwait(false);

			if (!string.IsNullOrWhiteSpace(Mtoken))
			{
				ConnectMemeAlertsCommand.Execute(default);
			}

			base.OnActivated();
		}

		protected override void OnDeactivated()
		{
			base.OnDeactivated();
		}

		[RelayCommand(CanExecute = nameof(CanConnectTwitch))]
		private async Task ConnectTwitch(CancellationToken cancellationToken)
		{
			IsCheckingTwitch = true;

			try
			{
				var oauthToken = await twitchOAuthService.AuthenticateAsync(cancellationToken);
				if (!string.IsNullOrWhiteSpace(oauthToken))
				{
					var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken);
					dispatcherService.CallWithDispatcher(() => IsTwitchConnected = true);
					Messenger.Send(new TwitchConnectedMessage(oauthToken, userId));
					await StartWork(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					dispatcherService.CallWithDispatcher(() => IsTwitchConnected = false);
				}
			}
			catch (Exception ex)
			{
				dispatcherService.CallWithDispatcher(() => IsTwitchConnected = false);
				logger.LogError(ex, "Error in ConnectTwitch");
			}
			finally
			{
				dispatcherService.CallWithDispatcher(() => IsCheckingTwitch = false);
			}
		}

		private bool CanConnectTwitch()
		{
			return !IsCheckingTwitch;
		}

		[RelayCommand(CanExecute = nameof(CanConnectMemeAlerts))]
		private async Task ConnectMemeAlerts(CancellationToken cancellationToken)
		{
			IsCheckingMemeAlerts = true;

			try
			{
				var maToken = await settingsService.GetMemeAlertsTokenAsync(cancellationToken);

				if (!string.IsNullOrWhiteSpace(maToken) && await twitchMemeAlertsAutoService.CheckToken(maToken, cancellationToken))
				{
					dispatcherService.CallWithDispatcher(() => IsMemeAlertsConnected = true);
					await StartWork(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					maToken = await dispatcherService.CallMemeAlertsAsync().ConfigureAwait(false);

					if (await twitchMemeAlertsAutoService.CheckToken(maToken, cancellationToken).ConfigureAwait(false))
					{
						await settingsService.SetMemeAlertsTokenAsync(maToken, cancellationToken).ConfigureAwait(false);

						dispatcherService.CallWithDispatcher(() => IsMemeAlertsConnected = true);
						await StartWork(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						dispatcherService.CallWithDispatcher(() => IsMemeAlertsConnected = false);
					}
				}
			}
			catch (Exception ex)
			{
				dispatcherService.CallWithDispatcher(() => IsMemeAlertsConnected = false);
				logger.LogError(ex, "Error in ConnectMemeAlerts");
			}
			finally
			{
				dispatcherService.CallWithDispatcher(() => IsCheckingMemeAlerts = false);
			}
		}

		private bool CanConnectMemeAlerts()
		{
			return !IsCheckingMemeAlerts;
		}

		private async Task StartWork(CancellationToken cancellationToken = default)
		{
			if (!IsTwitchConnected || !IsMemeAlertsConnected)
			{
				return;
			}

			if (cancellationTokenSource != null)
			{
				await rewardsService.StopAsync(cancellationToken).ConfigureAwait(false);
				cancellationTokenSource.Cancel();
			}

			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			var maToken = await settingsService.GetMemeAlertsTokenAsync(cancellationToken).ConfigureAwait(false);
			string broadcasterLogin = null;
			IDictionary<string, int> rewards = null;

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				var channelInformationResponse = await twitchAPI.Helix.Channels.GetChannelInformationAsync(userId);
				broadcasterLogin = channelInformationResponse.Data.First().BroadcasterLogin;
			}

			using (var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
			{
				rewards = context.Settings.Where(s => s.Key.StartsWith("Reward:")).Select(r => r.Key.Replace("Reward:", string.Empty) + ":" + r.Value).ToDictionary(d => d.Split(':')[0], d => int.Parse(d.Split(":")[1]));
			}

			cancellationTokenSource = new CancellationTokenSource();

			await rewardsService.StartAsync(rewards, broadcasterLogin, cancellationTokenSource.Token).ConfigureAwait(false);
		}
	}
}
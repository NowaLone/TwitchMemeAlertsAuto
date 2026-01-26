using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Interfaces;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class ConnectionViewModel : ObservableRecipient
	{
		private readonly ISettingsService settingsService;
		private readonly ITwitchOAuthService twitchOAuthService;
		private readonly ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService;
		private readonly IDispatcherService dispatcherService;
		private readonly IServiceProvider serviceProvider;
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
		private readonly ILogger<ConnectionViewModel> logger;

		private CancellationTokenSource cancellationTokenSource;

		public ConnectionViewModel()
		{
		}

		public ConnectionViewModel(ISettingsService settingsService, ITwitchOAuthService twitchOAuthService, ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService, IDispatcherService dispatcherService, IServiceProvider serviceProvider, IDbContextFactory<TmaaDbContext> dbContextFactory, ILogger<ConnectionViewModel> logger) : this()
		{
			this.settingsService = settingsService;
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

		protected override async void OnActivated()
		{
			this.twitchMemeAlertsAutoService.OnMemesReceived += OnMemesReceived;
			this.twitchMemeAlertsAutoService.OnMemesNotReceived += OnMemesNotReceived;
			this.twitchMemeAlertsAutoService.OnSupporterNotFound += OnSupporterNotFound;
			this.twitchMemeAlertsAutoService.OnSupporterLoading += OnSupporterLoading;
			this.twitchMemeAlertsAutoService.OnSupporterLoaded += OnSupporterLoaded;

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
			this.twitchMemeAlertsAutoService.OnMemesReceived -= OnMemesReceived;
			this.twitchMemeAlertsAutoService.OnMemesNotReceived -= OnMemesNotReceived;
			this.twitchMemeAlertsAutoService.OnSupporterNotFound -= OnSupporterNotFound;
			this.twitchMemeAlertsAutoService.OnSupporterLoading -= OnSupporterLoading;
			this.twitchMemeAlertsAutoService.OnSupporterLoaded -= OnSupporterLoaded;

			base.OnDeactivated();
		}

		private async void OnMemesReceived(string message)
		{
			var history = new History() { Username = message, Timestamp = DateTimeOffset.Now };
			Messenger.Send(new MemeAlertsLogMessage(history));
			await SaveHistory(history).ConfigureAwait(false);
		}

		private async void OnMemesNotReceived(string message)
		{
			var history = new History() { Username = message, Timestamp = DateTimeOffset.Now };
			Messenger.Send(new MemeAlertsLogMessage(history));
			await SaveHistory(history).ConfigureAwait(false);
		}

		private async void OnSupporterNotFound(string message)
		{
			var history = new History() { Username = message, Timestamp = DateTimeOffset.Now };
			Messenger.Send(new MemeAlertsLogMessage(history));
			await SaveHistory(history).ConfigureAwait(false);
		}

		private async void OnSupporterLoading(string message)
		{
			var history = new History() { Username = message, Timestamp = DateTimeOffset.Now };
			Messenger.Send(new MemeAlertsLogMessage(history));
		}

		private async void OnSupporterLoaded(string message)
		{
			var history = new History() { Username = message, Timestamp = DateTimeOffset.Now };
			Messenger.Send(new MemeAlertsLogMessage(history));
		}

		private async Task SaveHistory(History history, CancellationToken cancellationToken = default)
		{
			using (var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
			{
				context.Histories.Add(history);
				await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
			}
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
					IsTwitchConnected = true;
					Messenger.Send(new TwitchConnectedMessage(oauthToken, userId));
				}
				else
				{
					IsTwitchConnected = false;
				}
			}
			catch (Exception ex)
			{
				IsTwitchConnected = false;
				logger.LogError(ex, "Error in ConnectTwitch");
			}
			finally
			{
				IsCheckingTwitch = false;
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
					IsMemeAlertsConnected = true;
					await StartWork(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					maToken = await dispatcherService.CallMemeAlertsAsync().ConfigureAwait(false);

					if (await twitchMemeAlertsAutoService.CheckToken(maToken, cancellationToken).ConfigureAwait(false))
					{
						await settingsService.SetMemeAlertsTokenAsync(maToken, cancellationToken).ConfigureAwait(false);

						IsMemeAlertsConnected = true;
						await StartWork(cancellationToken).ConfigureAwait(false);
					}
					else
					{
						IsMemeAlertsConnected = false;
					}
				}
			}
			catch (Exception ex)
			{
				IsMemeAlertsConnected = false;
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
			cancellationTokenSource?.Cancel();

			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			var maToken = await settingsService.GetMemeAlertsTokenAsync(cancellationToken).ConfigureAwait(false);
			string broadcasterLogin, rewards = null;

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				var channelInformationResponse = await twitchAPI.Helix.Channels.GetChannelInformationAsync(userId);
				broadcasterLogin = channelInformationResponse.Data.First().BroadcasterLogin;
			}

			using (var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
			{
				rewards = string.Join(",", context.Settings.Where(s => s.Key.StartsWith("Reward:")).Select(r => r.Key.Replace("Reward:", string.Empty) + ":" + r.Value));
			}

			cancellationTokenSource = new CancellationTokenSource();

			twitchMemeAlertsAutoService.Work(broadcasterLogin, maToken, rewards, cancellationTokenSource.Token);
		}
	}
}
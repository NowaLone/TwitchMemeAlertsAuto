using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Securify.ShellLink;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Interfaces;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class MainMenuViewModel : ObservableRecipient
	{
		private readonly IDispatcherService dispatcherService;
		private readonly ISettingsService settingsService;
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger logger;
		private readonly string startupFullPath;

		[ObservableProperty]
		private ConnectionViewModel connectionViewModel;

		[ObservableProperty]
		private bool isStartup;

		[ObservableProperty]
		private bool tryRewardWithWrongNickname;

		[ObservableProperty]
		private bool showMemer;

		[ObservableProperty]
		private bool sendRandomMeme;

		[ObservableProperty]
		private bool showMemerWithMemeInfo;

		public MainMenuViewModel()
		{
		}

		public MainMenuViewModel(IDispatcherService dispatcherService, ISettingsService settingsService, IServiceProvider serviceProvider, ConnectionViewModel connectionViewModel, ILogger<MainMenuViewModel> logger) : this()
		{
			this.dispatcherService = dispatcherService;
			this.settingsService = settingsService;
			this.serviceProvider = serviceProvider;
			this.connectionViewModel = connectionViewModel;
			this.logger = logger;
			this.startupFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ".lnk"));
		}

		protected override async void OnActivated()
		{
			ConnectionViewModel.IsActive = true;

			IsStartup = File.Exists(startupFullPath);
			TryRewardWithWrongNickname = await settingsService.GetTryRewardWithWrongNicknameOptionAsync().ConfigureAwait(false);
			ShowMemer = !string.IsNullOrWhiteSpace(await settingsService.GetShowMemerRewardIdAsync().ConfigureAwait(false));
			SendRandomMeme = !string.IsNullOrWhiteSpace(await settingsService.GetSendRandomMemeRewardIdAsync().ConfigureAwait(false));
			ShowMemerWithMemeInfo = await settingsService.GetShowMemerWithMemeInfoAsync().ConfigureAwait(false);
			base.OnActivated();
		}

		protected override void OnDeactivated()
		{
			ConnectionViewModel.IsActive = false;

			base.OnDeactivated();
		}

		[RelayCommand]
		private void Exit()
		{
			dispatcherService.Shutdown();
		}

		[RelayCommand]
		private void About()
		{
			dispatcherService.ShowMessage($"TwitchMemeAlertsAuto v {FileVersionInfo.GetVersionInfo(Environment.ProcessPath).FileVersion?.ToString()} by NowaruAlone 😎");
		}

		[RelayCommand]
		private void CheckForUpdates()
		{
			dispatcherService.CheckForUpdates();
		}

		[RelayCommand]
		private void AddRemoveWinStartup()
		{
			try
			{
				RemoveAutorunShortcut();

				if (IsStartup)
				{
					Shortcut.CreateShortcut(Environment.ProcessPath, "--silent", Environment.ProcessPath, 0).WriteToFile(startupFullPath);
				}
			}
			catch (Exception e)
			{
				logger.LogError(e, "Error while adding startup lnk");
			}
		}

		private void RemoveAutorunShortcut()
		{
			try
			{
				File.Delete(startupFullPath);
			}
			catch (Exception e)
			{
				logger.LogError(e, "Error while removing startup lnk");
			}
		}

		[RelayCommand]
		private async Task SetTryRewardWithWrongNickname(CancellationToken cancellationToken = default)
		{
			await settingsService.SetTryRewardWithWrongNicknameOptionAsync(TryRewardWithWrongNickname, cancellationToken).ConfigureAwait(false);
			Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetTryRewardWithWrongNicknameOptionAsync)));
		}

		[RelayCommand(CanExecute = nameof(CanAddShowMemer))]
		private async Task AddShowMemerAsync(CancellationToken cancellationToken = default)
		{
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				var response = await twitchAPI.Helix.ChannelPoints.CreateCustomRewardsAsync(userId, new TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward.CreateCustomRewardsRequest
				{
					Cost = 100,
					Title = Properties.Resources.ShowLastMemer,
					IsEnabled = true,
				}).ConfigureAwait(false);
				await settingsService.SetShowMemerRewardIdAsync(response.Data[0].Id, cancellationToken).ConfigureAwait(false);
			}

			Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetShowMemerRewardIdAsync)));
			ShowMemer = false;

			dispatcherService.ShowMessage(Properties.Resources.ShowMemerRewardSuccessfullyCreated);
		}

		private bool CanAddShowMemer()
		{
			return !ShowMemer;
		}

		[RelayCommand(CanExecute = nameof(CanAddSendRandomMeme))]
		private async Task AddSendRandomMemeAsync(CancellationToken cancellationToken = default)
		{
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				var response = await twitchAPI.Helix.ChannelPoints.CreateCustomRewardsAsync(userId, new TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward.CreateCustomRewardsRequest
				{
					Cost = 100,
					Title = Properties.Resources.SendRandomMeme,
					IsEnabled = true,
				}).ConfigureAwait(false);
				await settingsService.SetSendRandomMemeRewardIdAsync(response.Data[0].Id, cancellationToken).ConfigureAwait(false);
			}

			Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetSendRandomMemeRewardIdAsync)));
			SendRandomMeme = false;

			dispatcherService.ShowMessage(Properties.Resources.SendRandomMemeRewardSuccessfullyCreated);
		}

		private bool CanAddSendRandomMeme()
		{
			return !SendRandomMeme;
		}

		[RelayCommand(CanExecute = nameof(CanRemoveSendRandomMeme))]
		private async Task RemoveSendRandomMemeAsync(CancellationToken cancellationToken = default)
		{
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			var rewardId = await settingsService.GetSendRandomMemeRewardIdAsync(cancellationToken).ConfigureAwait(false);

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				await twitchAPI.Helix.ChannelPoints.DeleteCustomRewardAsync(userId, rewardId).ConfigureAwait(false);
			}

			await settingsService.SetSendRandomMemeRewardIdAsync(string.Empty, cancellationToken).ConfigureAwait(false);

			Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetSendRandomMemeRewardIdAsync)));
			SendRandomMeme = true;
		}

		private bool CanRemoveSendRandomMeme()
		{
			return SendRandomMeme;
		}

		[RelayCommand(CanExecute = nameof(CanRemoveShowMemer))]
		private async Task RemoveShowMemerAsync(CancellationToken cancellationToken = default)
		{
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			var rewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				await twitchAPI.Helix.ChannelPoints.DeleteCustomRewardAsync(userId, rewardId).ConfigureAwait(false);
			}

			await settingsService.SetShowMemerRewardIdAsync(string.Empty, cancellationToken).ConfigureAwait(false);

			Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetShowMemerRewardIdAsync)));
			ShowMemer = true;
		}

		private bool CanRemoveShowMemer()
		{
			return ShowMemer;
		}

		[RelayCommand(CanExecute = nameof(CanShowMemerWithMemeInfo))]
		private async Task ShowMemerWithMemeInfoAsync(bool option, CancellationToken cancellationToken = default)
		{
			if (option)
			{
				dispatcherService.ShowMessage(Properties.Resources.ProfanityInfo);
			}
			await settingsService.SetShowMemerWithMemeInfoAsync(option, cancellationToken).ConfigureAwait(false);
		}

		private bool CanShowMemerWithMemeInfo()
		{
			return ShowMemer;
		}
	}
}
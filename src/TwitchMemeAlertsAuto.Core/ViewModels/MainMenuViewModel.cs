using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
		private readonly ITwitchAPI twitchAPI;
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

		public MainMenuViewModel()
		{
		}

		public MainMenuViewModel(IDispatcherService dispatcherService, ISettingsService settingsService, ITwitchAPI twitchAPI, ConnectionViewModel connectionViewModel, ILogger<MainMenuViewModel> logger) : this()
		{
			this.dispatcherService = dispatcherService;
			this.settingsService = settingsService;
			this.twitchAPI = twitchAPI;
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

		[RelayCommand]
		private async Task AddShowMemerAsync(CancellationToken cancellationToken = default)
		{
			var userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			
			if (ShowMemer)
			{
				var response = await twitchAPI.Helix.ChannelPoints.CreateCustomRewardsAsync(userId, new TwitchLib.Api.Helix.Models.ChannelPoints.CreateCustomReward.CreateCustomRewardsRequest
				{
					Cost = 100,
					Title = Properties.Resources.ShowLastMemer,
					IsEnabled = true,					
				}).ConfigureAwait(false);
				await settingsService.SetShowMemerRewardIdAsync(response.Data[0].Id, cancellationToken).ConfigureAwait(false);
				Messenger.Send(new SettingsChangedMessage(nameof(settingsService.GetShowMemerRewardIdAsync)));
				dispatcherService.ShowMessage(Properties.Resources.ShowMemerRewardSuccessfullyCreated);
			}
			else
			{
				var rewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);
				await twitchAPI.Helix.ChannelPoints.DeleteCustomRewardAsync(userId, rewardId).ConfigureAwait(false);
				await settingsService.SetShowMemerRewardIdAsync(string.Empty, cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
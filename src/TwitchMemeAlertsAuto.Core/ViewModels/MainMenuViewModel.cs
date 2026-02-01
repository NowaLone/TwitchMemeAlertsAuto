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
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class MainMenuViewModel : ObservableRecipient
	{
		private readonly IDispatcherService dispatcherService;
		private readonly ISettingsService settingsService;
		private readonly ILogger logger;
		private readonly string startupFullPath;

		[ObservableProperty]
		private bool isStartup;

		[ObservableProperty]
		private bool tryRewardWithWrongNickname;

		public MainMenuViewModel()
		{
		}

		public MainMenuViewModel(IDispatcherService dispatcherService, ISettingsService settingsService, ILogger<MainMenuViewModel> logger) : this()
		{
			this.dispatcherService = dispatcherService;
			this.settingsService = settingsService;
			this.logger = logger;
			this.startupFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ".lnk"));
		}

		protected override async void OnActivated()
		{
			IsStartup = File.Exists(startupFullPath);
			TryRewardWithWrongNickname = await settingsService.GetTryRewardWithWrongNicknameOptionAsync().ConfigureAwait(false);
			base.OnActivated();
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
	}
}
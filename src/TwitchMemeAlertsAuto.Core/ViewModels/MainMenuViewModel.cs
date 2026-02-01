using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Securify.ShellLink;
using System;
using System.Diagnostics;
using System.IO;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class MainMenuViewModel : ObservableRecipient
	{
		private readonly IDispatcherService dispatcherService;
		private readonly ILogger logger;
		private readonly string startupFullPath;

		[ObservableProperty]
		private bool isStartup;

		public MainMenuViewModel()
		{
		}

		public MainMenuViewModel(IDispatcherService dispatcherService, ILogger<MainMenuViewModel> logger) : this()
		{
			this.dispatcherService = dispatcherService;
			this.logger = logger;
			this.startupFullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), Path.ChangeExtension(Path.GetFileName(Environment.ProcessPath), ".lnk"));
		}

		protected override void OnActivated()
		{
			IsStartup = File.Exists(startupFullPath);
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
	}
}
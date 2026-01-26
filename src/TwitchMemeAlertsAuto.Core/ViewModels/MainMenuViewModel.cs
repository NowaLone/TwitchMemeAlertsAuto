using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class MainMenuViewModel : ObservableRecipient
	{
		private readonly IDispatcherService dispatcherService;

		public MainMenuViewModel()
		{
		}

		public MainMenuViewModel(IDispatcherService dispatcherService) : this()
		{
			this.dispatcherService = dispatcherService;
		}

		[RelayCommand]
		private void Exit()
		{
			dispatcherService.Shutdown();
		}

		[RelayCommand]
		private void About()
		{
			// Optionally show about dialog or message
		}

		[RelayCommand]
		private void CheckForUpdates()
		{
			Process.Start(new ProcessStartInfo("https://github.com/NowaLone/TwitchMemeAlertsAuto/releases") { UseShellExecute = true});
		}
	}
}
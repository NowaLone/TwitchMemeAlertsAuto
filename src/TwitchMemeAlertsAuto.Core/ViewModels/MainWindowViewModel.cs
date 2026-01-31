using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class MainWindowViewModel : ObservableRecipient
	{
		private readonly ILogger logger;

		[ObservableProperty]
		private LogViewModel logViewModel;

		[ObservableProperty]
		private ConnectionViewModel connectionViewModel;

		[ObservableProperty]
		private RewardsViewModel rewardsViewModel;

		[ObservableProperty]
		private MainMenuViewModel mainMenuViewModel;

		[ObservableProperty]
		private AllRewardViewModel allRewardViewModel;

		[ObservableProperty]
		private SupportersViewModel supportersViewModel;

		public MainWindowViewModel()
		{
		}

		public MainWindowViewModel(LogViewModel logViewModel, ConnectionViewModel connectionViewModel, RewardsViewModel rewardsViewModel, MainMenuViewModel mainMenuViewModel, AllRewardViewModel allRewardViewModel, SupportersViewModel supportersViewModel, ILogger<MainWindowViewModel> logger) : this()
		{
			this.logViewModel = logViewModel;
			this.connectionViewModel = connectionViewModel;
			this.rewardsViewModel = rewardsViewModel;
			this.mainMenuViewModel = mainMenuViewModel;
			this.allRewardViewModel = allRewardViewModel;
			this.supportersViewModel = supportersViewModel;
			this.logger = logger;
		}

		protected override void OnActivated()
		{
			LogViewModel.IsActive = true;
			ConnectionViewModel.IsActive = true;
			RewardsViewModel.IsActive = true;
			MainMenuViewModel.IsActive = true;
			AllRewardViewModel.IsActive = true;
			SupportersViewModel.IsActive = true;

			base.OnActivated();
		}

		protected override void OnDeactivated()
		{
			LogViewModel.IsActive = false;
			ConnectionViewModel.IsActive = false;
			RewardsViewModel.IsActive = false;
			MainMenuViewModel.IsActive = false;
			AllRewardViewModel.IsActive = false;
			SupportersViewModel.IsActive = false;

			base.OnDeactivated();
		}
	}
}
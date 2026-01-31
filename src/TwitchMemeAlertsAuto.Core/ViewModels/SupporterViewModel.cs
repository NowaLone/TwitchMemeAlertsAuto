using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class SupporterViewModel : ObservableRecipientWithQuantity
	{
		private readonly ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService;
		private readonly ILogger logger;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RewardCommand))]
		private Supporter supporter;

		public SupporterViewModel() : base()
		{
		}

		public SupporterViewModel(ITwitchMemeAlertsAutoService twitchMemeAlertsAutoService, ILogger<SupporterViewModel> logger) : this()
		{
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.logger = logger;
		}

		[RelayCommand(CanExecute = nameof(CanReward))]
		private async Task Reward(string parameter, CancellationToken cancellationToken = default)
		{
			var value = int.Parse(parameter);

			if (await twitchMemeAlertsAutoService.AddMemesAsync(Supporter, value, cancellationToken).ConfigureAwait(false))
			{
				logger.LogInformation(EventIds.Rewarded, "Мемы для {username} успешно выданы в кол-ве {value} шт.", Supporter.SupporterName, value);
			}
			else
			{
				logger.LogError(EventIds.NotRewarded, "Мемы для {username} не выданы", Supporter.SupporterName);
			}

			Quantity = "0";
		}

		private bool CanReward(string parameter)
		{
			return !HasErrors && Supporter != null;
		}
	}
}
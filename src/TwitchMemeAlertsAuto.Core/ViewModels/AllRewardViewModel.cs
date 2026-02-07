using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class AllRewardViewModel : ObservableRecipientWithQuantity, IRecipient<MemealertsConnectedMessage>
	{
		private readonly IMemeAlertsService twitchMemeAlertsAutoService;
		private readonly IDispatcherService dispatcherService;
		private readonly ILogger logger;

		[ObservableProperty]
		private bool isConnected;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RewardWhoSentCommand))]
		private string whoSentCount;

		[ObservableProperty]
		[NotifyCanExecuteChangedFor(nameof(RewardWhoReceivedCommand))]
		private string whoReceivedCount;

		public AllRewardViewModel() : base()
		{
			WhoSentCount = "10";
			WhoReceivedCount = "10";
		}

		public AllRewardViewModel(IMemeAlertsService twitchMemeAlertsAutoService, IDispatcherService dispatcherService, ILogger<AllRewardViewModel> logger) : this()
		{
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.dispatcherService = dispatcherService;
			this.logger = logger;
		}

		public void Receive(MemealertsConnectedMessage message)
		{
			IsConnected = true;
		}

		protected override void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Quantity))
			{
				RewardAllCommand.NotifyCanExecuteChanged();
				RewardWhoSentCommand.NotifyCanExecuteChanged();
				RewardWhoReceivedCommand.NotifyCanExecuteChanged();
			}

			base.OnPropertyChanged(e);
		}

		[RelayCommand(CanExecute = nameof(CanRewardAll))]
		private async Task RewardAllAsync(string parameter, CancellationToken cancellationToken = default)
		{
			var supporters = await twitchMemeAlertsAutoService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);
			var value = int.Parse(Quantity);

			foreach (var supporter in supporters)
			{
				if (await twitchMemeAlertsAutoService.GiveBonusAsync(supporter, value, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation(EventIds.Rewarded, "Мемы для {username} успешно выданы в кол-ве {value} шт.", supporter.SupporterName, value);
				}
				else
				{
					logger.LogError(EventIds.NotRewarded, "Мемы для {username} не выданы", supporter.SupporterName);
				}

				await Task.Delay(500, cancellationToken).ConfigureAwait(false);
			}

			dispatcherService.CallWithDispatcher(() => Quantity = "0");
		}

		private bool CanRewardAll(string parameter)
		{
			return !HasErrors;
		}

		[RelayCommand(CanExecute = nameof(CanRewardWhoSent))]
		private async Task RewardWhoSentAsync(string parameter, CancellationToken cancellationToken = default)
		{
			var events = await twitchMemeAlertsAutoService.GetEventsAsync(cancellationToken).ConfigureAwait(false);
			var value = int.Parse(parameter);
			var qty = int.Parse(Quantity);

			foreach (var supporter in events.DistinctBy(e => e.UserId).OrderByDescending(e => e.Timestamp).Take(value).Select(e => new Supporter { SupporterId = e.UserId, SupporterName = e.UserName }))
			{
				if (await twitchMemeAlertsAutoService.GiveBonusAsync(supporter, qty, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation(EventIds.Rewarded, "Мемы для {username} успешно выданы в кол-ве {qty} шт.", supporter.SupporterName, qty);
				}
				else
				{
					logger.LogError(EventIds.NotRewarded, "Мемы для {username} не выданы", supporter.SupporterName);
				}

				await Task.Delay(500, cancellationToken).ConfigureAwait(false);
			}

			dispatcherService.CallWithDispatcher(() => Quantity = "0");
		}

		private bool CanRewardWhoSent(string parameter)
		{
			return !HasErrors && int.TryParse(parameter, out var result) && result > 0;
		}

		[RelayCommand(CanExecute = nameof(CanRewardWhoReceived))]
		private async Task RewardWhoReceivedAsync(string parameter, CancellationToken cancellationToken = default)
		{
			var supporters = await twitchMemeAlertsAutoService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);
			var value = int.Parse(parameter);
			var qty = int.Parse(Quantity);

			foreach (var supporter in supporters.OrderByDescending(s => s.LastSupport).Take(value))
			{
				if (await twitchMemeAlertsAutoService.GiveBonusAsync(supporter, qty, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation(EventIds.Rewarded, "Мемы для {username} успешно выданы в кол-ве {qty} шт.", supporter.SupporterName, qty);
				}
				else
				{
					logger.LogError(EventIds.NotRewarded, "Мемы для {username} не выданы", supporter.SupporterName);
				}

				await Task.Delay(500, cancellationToken).ConfigureAwait(false);
			}

			dispatcherService.CallWithDispatcher(() => Quantity = "0");
		}

		private bool CanRewardWhoReceived(string parameter)
		{
			return !HasErrors && int.TryParse(parameter, out var result) && result > 0;
		}
	}
}
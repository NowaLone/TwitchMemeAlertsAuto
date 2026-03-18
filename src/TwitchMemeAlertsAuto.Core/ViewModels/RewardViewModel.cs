using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class RewardViewModel : ObservableRecipientWithQuantity
	{
		private readonly ISettingsService settingsService;

		[ObservableProperty]
		private CustomReward reward;

		public RewardViewModel() : base()
		{
		}

		public RewardViewModel(ISettingsService settingsService) : this()
		{
			this.settingsService = settingsService;
		}

		protected override async void OnActivated()
		{
			await LoadCount().ConfigureAwait(false);
			base.OnActivated();
		}

		[RelayCommand(CanExecute = nameof(CanSave))]
		private async Task Save(string parameter, CancellationToken cancellationToken = default)
		{
			await settingsService.SetSettingAsync($"Reward:{Reward.Id}", parameter, cancellationToken).ConfigureAwait(false);
			Messenger.Send(new SettingsChangedMessage(Reward.Id));
		}

		private bool CanSave(string parameter)
		{
			return !HasErrors;
		}

		private async Task LoadCount(CancellationToken cancellationToken = default)
		{
			var value = await settingsService.GetSettingAsync($"Reward:{Reward.Id}", default(string), cancellationToken).ConfigureAwait(false);
			if (int.TryParse(value, out var count))
			{
				Quantity = count.ToString();
			}
			else
			{
				Quantity = "0";
			}
		}
	}
}
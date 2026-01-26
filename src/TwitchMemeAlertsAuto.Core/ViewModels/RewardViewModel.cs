using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.ChannelPoints;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class RewardViewModel : ObservableRecipient, INotifyDataErrorInfo
	{
		private readonly IDispatcherService dispatcherService;
		private readonly ISettingsService settingsService;
		private readonly Dictionary<string, List<string>> errors;

		[ObservableProperty]
		private CustomReward reward;

		[ObservableProperty]
		private string count;

		public RewardViewModel()
		{
			this.errors = new Dictionary<string, List<string>>();
		}

		public RewardViewModel(IDispatcherService dispatcherService, ISettingsService settingsService) : this()
		{
			this.dispatcherService = dispatcherService;
			this.settingsService = settingsService;
		}

		public bool HasErrors => errors.Any();

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		public IEnumerable GetErrors(string propertyName)
		{
			return errors.TryGetValue(propertyName, out var strings) ? strings : new List<string>();
		}

		private void AddError(string property, string error)
		{
			if (!errors.ContainsKey(property)) errors[property] = new List<string>();
			errors[property].Add(error);
			ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
		}

		private void ClearErrors(string property)
		{
			if (errors.Remove(property))
				ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(property));
		}

		partial void OnCountChanged(string oldValue, string newValue)
		{
			ClearErrors(nameof(Count));

			if (!int.TryParse(newValue, out var result) || result <= 0 || result > 1000)
			{
				AddError(nameof(Count), "Count must be a positive integer and less then 1000.");
			}
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

			// TODO: remove this requirement
			dispatcherService.ShowMessage("Требуется перезапуск!");
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
				Count = count.ToString();
			}
			else
			{
				Count = "0";
			}
		}
	}
}
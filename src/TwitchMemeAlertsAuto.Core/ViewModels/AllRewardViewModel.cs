using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class AllRewardViewModel : ObservableRecipient, INotifyDataErrorInfo
	{
		private readonly IMemeAlertsService twitchMemeAlertsAutoService;
		private readonly ILogger logger;
		private readonly Dictionary<string, List<string>> errors;

		[ObservableProperty]
		private string count;

		public AllRewardViewModel()
		{
			this.errors = new Dictionary<string, List<string>>();
			Count = "0";
		}

		public AllRewardViewModel(IMemeAlertsService twitchMemeAlertsAutoService, ILogger<AllRewardViewModel> logger) : this()
		{
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.logger = logger;
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

		[RelayCommand(CanExecute = nameof(CanReward))]
		private async Task Reward(string parameter, CancellationToken cancellationToken = default)
		{
			var current = await twitchMemeAlertsAutoService.GetCurrent(cancellationToken).ConfigureAwait(false);
			var supporters = await twitchMemeAlertsAutoService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);
			var value = int.Parse(parameter);

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

			Count = "0";
		}

		private bool CanReward(string parameter)
		{
			return !HasErrors;
		}
	}
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Interfaces;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class RewardsViewModel : ObservableRecipient, IRecipient<TwitchConnectedMessage>
	{
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger<RewardsViewModel> logger;

		[ObservableProperty]
		private ObservableCollection<RewardViewModel> rewards;

		[ObservableProperty]
		private bool isLoading;

		public RewardsViewModel()
		{
			rewards = new ObservableCollection<RewardViewModel>();
		}

		public RewardsViewModel(IServiceProvider serviceProvider, ILogger<RewardsViewModel> logger) : this()
		{
			this.serviceProvider = serviceProvider;
			this.logger = logger;
		}

		public async void Receive(TwitchConnectedMessage message)
		{
			await LoadRewardsAsync(message.Value.Token, message.Value.UserId).ConfigureAwait(false);
		}

		[RelayCommand(CanExecute = nameof(CanRefresh))]
		private Task RefreshAsync(object parameter, CancellationToken cancellationToken = default)
		{
			string token, userId = null;
			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var settingsService = scope.ServiceProvider.GetRequiredService<ISettingsService>();
				token = settingsService.GetTwitchOAuthTokenAsync(cancellationToken).GetAwaiter().GetResult();
				userId = settingsService.GetTwitchUserIdAsync(cancellationToken).GetAwaiter().GetResult();
			}

			return LoadRewardsAsync(token, userId);
		}

		private bool CanRefresh(object parameter)
		{
			return !IsLoading;
		}

		private async Task LoadRewardsAsync(string token, string userId)
		{
			IsLoading = true;
			try
			{
				using (var scope = serviceProvider.CreateAsyncScope())
				{
					var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
					twitchAPI.Settings.AccessToken = token;
					var response = await twitchAPI.Helix.ChannelPoints.GetCustomRewardAsync(userId);
					if (response?.Data != null && response.Data.Any())
					{
						Rewards.Clear();
						foreach (var r in response.Data)
						{
							var item = serviceProvider.GetRequiredService<RewardViewModel>();
							item.Reward = r;
							item.IsActive = true;
							Rewards.Add(item);
						}
					}
				}

				OnPropertyChanged(nameof(Rewards));
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error fetching rewards");
			}
			finally
			{
				IsLoading = false;
			}
		}
	}
}
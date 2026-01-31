using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchLib.Api.Interfaces;
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
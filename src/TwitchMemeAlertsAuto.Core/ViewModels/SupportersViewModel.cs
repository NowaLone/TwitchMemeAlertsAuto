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
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class SupportersViewModel : ObservableRecipient, IRecipient<MemealertsConnectedMessage>
	{
		private readonly IServiceProvider serviceProvider;
		private readonly IDispatcherService dispatcherService;
		private readonly IMemeAlertsService twitchMemeAlertsAutoService;
		private readonly ILogger logger;

		[ObservableProperty]
		private ObservableCollection<SupporterViewModel> supporters;

		[ObservableProperty]
		private bool isLoading;

		public SupportersViewModel()
		{
			supporters = new ObservableCollection<SupporterViewModel>();
		}

		public SupportersViewModel(IServiceProvider serviceProvider, IDispatcherService dispatcherService, IMemeAlertsService twitchMemeAlertsAutoService, ILogger<SupportersViewModel> logger) : this()
		{
			this.serviceProvider = serviceProvider;
			this.dispatcherService = dispatcherService;
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.logger = logger;
		}

		public async void Receive(MemealertsConnectedMessage message)
		{
			await LoadSupportersAsync().ConfigureAwait(false);
		}

		[RelayCommand(CanExecute = nameof(CanRefresh))]
		private Task RefreshAsync(object parameter, CancellationToken cancellationToken = default)
		{
			return LoadSupportersAsync();
		}

		private bool CanRefresh(object parameter)
		{
			return !IsLoading;
		}

		private async Task LoadSupportersAsync()
		{
			IsLoading = true;
			try
			{
				var data = await twitchMemeAlertsAutoService.GetSupportersAsync().ConfigureAwait(false);

				using (var scope = serviceProvider.CreateAsyncScope())
				{
					if (data != null && data.Any())
					{
						dispatcherService.CallWithDispatcher(Supporters.Clear);
						foreach (var s in data)
						{
							var item = serviceProvider.GetRequiredService<SupporterViewModel>();
							item.Supporter = s;
							item.IsActive = true;
							dispatcherService.CallWithDispatcher(() => Supporters.Add(item));
						}
					}
				}

				OnPropertyChanged(nameof(Supporters));
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
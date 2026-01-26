using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class LogViewModel : ObservableRecipient, IRecipient<MemeAlertsLogMessage>
	{
		private readonly IDispatcherService dispatcherService;
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
		private readonly ILogger logger;

		[ObservableProperty]
		private ObservableCollection<History> log;

		public LogViewModel()
		{
			log = new ObservableCollection<History>();
		}

		public LogViewModel(IDispatcherService dispatcherService, IDbContextFactory<TmaaDbContext> dbContextFactory, ILogger<LogViewModel> logger) : this()
		{
			this.dispatcherService = dispatcherService;
			this.dbContextFactory = dbContextFactory;
			this.logger = logger;
		}

		protected override void OnActivated()
		{
			using (var ctx = dbContextFactory.CreateDbContext())
			{
				foreach (var item in ctx.Histories.OrderBy(h => h.Timestamp))
				{
					Log.Add(item);
				}
			}

			base.OnActivated();
		}

		public async void Receive(MemeAlertsLogMessage message)
		{
			await dispatcherService.CallWithDispatcherAsync(async () => Log.Add(message.Value)).ConfigureAwait(false);
		}
	}
}
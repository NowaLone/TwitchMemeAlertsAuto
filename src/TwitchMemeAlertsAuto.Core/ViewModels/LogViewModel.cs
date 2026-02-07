using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels
{
	public partial class LogViewModel : ObservableRecipient, IRecipient<LogMessage>
	{
		private readonly IDispatcherService dispatcherService;
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
		private readonly ILogger logger;

		private readonly IEnumerable<int> ignoredEventIds = EventIds.Events.Where(e => e.Id == EventIds.Loading.Id || e.Id == EventIds.Loaded.Id).Select(e => e.Id);

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

			OnPropertyChanged(nameof(Log));

			base.OnActivated();
		}

		public async void Receive(LogMessage message)
		{
			await dispatcherService.CallWithDispatcherAsync(async () => Log.Add(message.Value)).ConfigureAwait(false);

			if (Log.Count == 1)
			{
				OnPropertyChanged(nameof(Log));
			}

			if (!ignoredEventIds.Contains(message.Value.EventId))
			{
				await SaveHistory(message.Value).ConfigureAwait(false);
			}
		}

		private async Task SaveHistory(History history, CancellationToken cancellationToken = default)
		{
			using (var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
			{
				context.Histories.Add(history);
				await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
			}
		}
	}
}
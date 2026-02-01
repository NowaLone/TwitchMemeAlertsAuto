using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchChat.EventsArgs;

namespace TwitchMemeAlertsAuto.Core
{
	public class RewardsService : IRewardsService
	{
		private readonly IMemeAlertsService twitchMemeAlertsAutoService;
		private readonly ITwitchClient twitchClient;
		private readonly ILogger logger;

		private CancellationToken cancellationToken;
		private IDictionary<string, int> rewards;
		private string channel;
		private bool tryRewardWithWrongNickname;
		private List<Supporter> data;

		public RewardsService(IMemeAlertsService twitchMemeAlertsAutoService, ITwitchClient twitchClient, ILogger<RewardsService> logger)
		{
			this.twitchMemeAlertsAutoService = twitchMemeAlertsAutoService;
			this.twitchClient = twitchClient;
			this.logger = logger;
		}

		public async Task StartAsync(IDictionary<string, int> rewards, string channel, bool tryRewardWithWrongNickname = false, CancellationToken cancellationToken = default)
		{
			this.cancellationToken = cancellationToken;
			this.rewards = rewards;
			this.channel = channel;
			this.tryRewardWithWrongNickname = tryRewardWithWrongNickname;

			data = await twitchMemeAlertsAutoService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);

			twitchClient.JoinChannel(channel);
			twitchClient.OnMessageReceived += TwitchClient_OnMessageReceived;

			await twitchClient.ConnectAsync(cancellationToken).ConfigureAwait(false);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			twitchClient.OnMessageReceived -= TwitchClient_OnMessageReceived;
			
			try
			{
				return twitchClient.DisconnectAsync(cancellationToken);
			}
			catch (InvalidOperationException)
			{
				return Task.CompletedTask;
			}
		}

		private async void TwitchClient_OnMessageReceived(object sender, MessageReceivedEventArgs e)
		{
			if (e.Message is IrcV3Message ircV3Message && ircV3Message.Command == IrcCommand.PRIVMSG && ircV3Message.Parameters.ElementAt(0) == $"#{channel}" && ircV3Message.Tags.TryGetValue("custom-reward-id", out string customRewardId))
			{
				if (rewards.TryGetValue(customRewardId, out var value))
				{
					var username = ircV3Message.Parameters.ElementAt(1).TrimStart(':');

					var dataItem = data.FirstOrDefault(d => d.SupporterName.Equals(username, StringComparison.OrdinalIgnoreCase));

					if (dataItem == default)
					{
						data = await twitchMemeAlertsAutoService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);
						dataItem = data.FirstOrDefault(d => d.SupporterName.Equals(username, StringComparison.OrdinalIgnoreCase));

						if(dataItem == default && tryRewardWithWrongNickname)
						{
							logger.LogWarning(EventIds.NotFound, "Саппортёр {username} не найден, попытка наградить по нику с твича", username);
							username = ircV3Message.Prefix.Nick;
							dataItem = data.FirstOrDefault(d => d.SupporterName.Equals(username, StringComparison.OrdinalIgnoreCase));
						}
					}

					if (dataItem != default)
					{
						if (await twitchMemeAlertsAutoService.GiveBonusAsync(dataItem, value, cancellationToken).ConfigureAwait(false))
						{
							logger.LogInformation(EventIds.Rewarded, "Мемы для {username} успешно выданы в кол-ве {value} шт.", username, value);
						}
						else
						{
							logger.LogError(EventIds.NotRewarded, "Мемы для {username} не выданы", username);
						}
					}
					else
					{
						logger.LogWarning(EventIds.NotFound, "Саппортёр {username} не найден", username);
					}
				}
				else
				{
					logger.LogTrace("У сообщения не найден тег custom-reward-id");
				}
			}
		}
	}
}
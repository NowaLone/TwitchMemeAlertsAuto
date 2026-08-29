using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProfanityFilter.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public class WebsocketHostedService : IWebsocketHostedService
	{
		private readonly EventSubWebsocketClient eventSubWebsocketClient;
		private readonly ITwitchClient twitchClient;
		private readonly IIrcParser<IrcV3Message> ircParser;
		private readonly ISettingsService settingsService;
		private readonly IMemeAlertsService memeAlertsService;
		private readonly IProfanityFilter profanityFilter;
		private readonly IServiceProvider serviceProvider;
		private readonly ILogger<WebsocketHostedService> logger;

		private string showMemerRewardId;
		private string sendRandomMemeRewardId;
		private string sendMemeWithTextId;
		private bool tryRewardWithWrongNickname;
		private Dictionary<string, int> rewards;
		private List<Supporter> supporters;
		private string userId;
		private string eventSubId;
		private CancellationToken serviceCancellationToken;

		private IEnumerable<Sticker> randomStrickers;
		private readonly SemaphoreSlim rewardProcessingSemaphore = new SemaphoreSlim(1, 1);

		public WebsocketHostedService(EventSubWebsocketClient eventSubWebsocketClient, ITwitchClient twitchClient, IIrcParser<IrcV3Message> ircParser, ISettingsService settingsService, IMemeAlertsService memeAlertsService, IProfanityFilter profanityFilter, IServiceProvider serviceProvider, ILogger<WebsocketHostedService> logger)
		{
			this.eventSubWebsocketClient = eventSubWebsocketClient;
			this.twitchClient = twitchClient;
			this.ircParser = ircParser;
			this.settingsService = settingsService;
			this.memeAlertsService = memeAlertsService;
			this.profanityFilter = profanityFilter;
			this.serviceProvider = serviceProvider;
			this.logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			serviceCancellationToken = cancellationToken;
			this.eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;

			userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			showMemerRewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);
			sendRandomMemeRewardId = await settingsService.GetSendRandomMemeRewardIdAsync(cancellationToken).ConfigureAwait(false);
			sendMemeWithTextId = await settingsService.GetSendMemeWithTextIdAsync(cancellationToken).ConfigureAwait(false);
			tryRewardWithWrongNickname = await settingsService.GetTryRewardWithWrongNicknameOptionAsync(cancellationToken).ConfigureAwait(false);
			rewards = await settingsService.GetRewardsAsync(cancellationToken).ConfigureAwait(false);

			supporters = await memeAlertsService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);

			if (!string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
			{
				var personalStickers = await memeAlertsService.GetPersonalAreaCatalogueAsync(cancellationToken).ConfigureAwait(false);
				var streamerStickers = await memeAlertsService.GetStreamerAreaCatalogueAsync(cancellationToken).ConfigureAwait(false);
				randomStrickers = personalStickers.Concat(streamerStickers);
			}
			else
			{
				randomStrickers = Enumerable.Empty<Sticker>();
			}

			var channel = await settingsService.GetTwitchUsernameAsync(cancellationToken).ConfigureAwait(false);

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();

				if (!string.IsNullOrWhiteSpace(showMemerRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendMemeWithTextId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendMemeWithTextId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				}

				await eventSubWebsocketClient.ConnectAsync();
			}
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			this.eventSubWebsocketClient.WebsocketConnected -= OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected -= OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected -= OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred -= OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd -= EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;

			await eventSubWebsocketClient.DisconnectAsync();

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				var response = await twitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(eventSubId);

				if (response)
				{
					logger.LogWarning("Unable to delete event subscription {eventSubId}", eventSubId);
				}

				if (!string.IsNullOrWhiteSpace(showMemerRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendMemeWithTextId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendMemeWithTextId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				}
			}
		}

		private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
		{
			logger.LogInformation("Websocket {sessionId} connected!", eventSubWebsocketClient.SessionId);

			if (!e.IsRequestedReconnect)
			{
				using (var scope = serviceProvider.CreateAsyncScope())
				{
					var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
					var response2 = await twitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync(new GetEventSubSubscriptionsRequest());
					foreach (var item in response2.Subscriptions)
					{
						await twitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(item.Id);
					}

					if (!string.IsNullOrWhiteSpace(showMemerRewardId) || !string.IsNullOrWhiteSpace(sendRandomMemeRewardId) || !string.IsNullOrWhiteSpace(sendMemeWithTextId) || rewards.Count > 0)
					{
						var condition = new Dictionary<string, string> { { "broadcaster_user_id", userId } };
						var response = await twitchAPI.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", condition, EventSubTransportMethod.Websocket, eventSubWebsocketClient.SessionId);
						if (response?.Subscriptions?.FirstOrDefault() != null)
						{
							eventSubId = response.Subscriptions.FirstOrDefault().Id;
						}
					}
				}
			}
		}

		private async Task OnWebsocketDisconnected(object sender, WebsocketDisconnectedArgs e)
		{
			logger.LogError("Websocket {sessionId} disconnected!", eventSubWebsocketClient.SessionId);

			var attempt = 0;

			while (!serviceCancellationToken.IsCancellationRequested)
			{
				if (await eventSubWebsocketClient.ReconnectAsync().ConfigureAwait(false))
				{
					return;
				}

				attempt++;
				var delay = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 60));
				logger.LogError("Websocket reconnect failed! Next attempt in {delay}", delay);

				try
				{
					await Task.Delay(delay, serviceCancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					return;
				}
			}
		}

		private Task OnWebsocketReconnected(object sender, WebsocketReconnectedArgs e)
		{
			logger.LogWarning("Websocket {sessionId} reconnected", eventSubWebsocketClient.SessionId);
			return Task.CompletedTask;
		}

		private Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
		{
			logger.LogError("Websocket {sessionId} - Error occurred!", eventSubWebsocketClient.SessionId);
			return Task.CompletedTask;
		}

		private async Task EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
		{
			var rewardId = e?.Payload?.Event?.Reward?.Id;
			var userName = e?.Payload?.Event?.UserName;

			if (string.IsNullOrWhiteSpace(rewardId))
			{
				return;
			}

			if (rewardId == showMemerRewardId)
			{
				logger.LogInformation(EventIds.ShowMemer, "{userName} активировал награду \"{title}\"", userName, e.Payload.Event.Reward.Title);

				var events = await memeAlertsService.GetEventsAsync().ConfigureAwait(false);
				var showMemeInfo = await settingsService.GetShowMemerWithMemeInfoAsync().ConfigureAwait(false);
				if (events.Count != 0)
				{
					var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
					var msg = ircParser.BuildMessage(new IrcV3Message
					{
						Command = IrcCommand.PRIVMSG,
						Parameters = new List<string>
						{
							$"#{e.Payload.Event.BroadcasterUserLogin}",
							string.Format(":@" + userName + " " + Properties.Resources.LastMemeSentBy, showMemeInfo ? $"\"{Censor(lastEvent.StickerName)}\"": string.Empty, lastEvent.UserName)
						},
					});

					await twitchClient.SendMessageAsync(msg).ConfigureAwait(false);
				}

				return;
			}
			else if (rewardId == sendRandomMemeRewardId)
			{
				logger.LogInformation(EventIds.RandomMeme, "{userName} активировал награду \"{title}\"", userName, e.Payload.Event.Reward.Title);

				try
				{
					if (randomStrickers.Any())
					{
						var random = Random.Shared.Next(0, randomStrickers.Count());
						var sticker = randomStrickers.ElementAt(random);
						var supporter = await memeAlertsService.GetStreamerAsSupporterAsync().ConfigureAwait(false);

						if (supporter.Balance == 0)
						{
							await memeAlertsService.GiveBonusAsync(supporter, 1).ConfigureAwait(false);
						}

						await memeAlertsService.SendMemeAsync(sticker, userName).ConfigureAwait(false);
					}
					else
					{
						logger.LogWarning("No stickers available to send for SendRandomMeme reward");
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error while handling SendRandomMeme reward redemption");
				}

				return;
			}
			else if (rewardId == sendMemeWithTextId)
			{
				logger.LogInformation(EventIds.MemeWithText, "{userName} активировал награду \"{title}\"", userName, e.Payload.Event.Reward.Title);

				try
				{
					var stickers = await memeAlertsService.GetPersonalAreaSearchAsync(e.Payload.Event.UserInput).ConfigureAwait(false);

					if (stickers.Any())
					{
						await memeAlertsService.SendMemeAsync(stickers.FirstOrDefault(), userName).ConfigureAwait(false);
					}
					else
					{
						logger.LogWarning("No stickers available to send for SendMemeWithText reward");
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error while handling SendMemeWithText reward redemption");
				}

				return;
			}
			else if (rewards.TryGetValue(rewardId, out var value))
			{
				logger.LogInformation(EventIds.MemeWithText, "{userName} активировал награду \"{title}\"", userName, e.Payload.Event.Reward.Title);

				try
				{
					var usernameInMemeAlerts = e?.Payload?.Event?.UserInput;

					await rewardProcessingSemaphore.WaitAsync(serviceCancellationToken).ConfigureAwait(false);
					var dataItem = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));

					if (dataItem == null)
					{
						supporters = await memeAlertsService.GetSupportersAsync(serviceCancellationToken).ConfigureAwait(false);
						rewardProcessingSemaphore.Release();

						dataItem = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));

						if (dataItem == null && tryRewardWithWrongNickname && !string.IsNullOrWhiteSpace(userName))
						{
							logger.LogWarning(EventIds.NotFound, "Саппортёр {usernameInMemeAlerts} не найден, попытка наградить по нику с твича", usernameInMemeAlerts);
							usernameInMemeAlerts = userName;
							dataItem = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));
						}
					}

					if (dataItem != null)
					{
						if (await memeAlertsService.GiveBonusAsync(dataItem, value, serviceCancellationToken).ConfigureAwait(false))
						{
							logger.LogInformation(EventIds.Rewarded, "Мемы для {usernameInMemeAlerts} успешно выданы в кол-ве {value} шт.", usernameInMemeAlerts, value);
						}
						else
						{
							logger.LogError(EventIds.NotRewarded, "Мемы для {usernameInMemeAlerts} не выданы", usernameInMemeAlerts);
						}
					}
					else
					{
						logger.LogWarning(EventIds.NotFound, "Саппортёр {usernameInMemeAlerts} не найден", usernameInMemeAlerts);
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Error while handling GiveBonus reward redemption");
				}

				return;
			}
		}

		// hack from https://github.com/stephenhaunts/ProfanityDetector/issues/34#issuecomment-2789168413
		private string Censor(string stickerName)
		{
			var message = profanityFilter.CensorString(stickerName, '*', true);

			if (profanityFilter.ContainsProfanity(message))
			{
				var sb = new StringBuilder(message);
				for (int i = 0; i < sb.Length; i++)
				{
					if (sb[i] != ' ')
					{
						sb[i] = '*';
					}
				}
				message = sb.ToString();
			}

			return message;
		}
	}
}
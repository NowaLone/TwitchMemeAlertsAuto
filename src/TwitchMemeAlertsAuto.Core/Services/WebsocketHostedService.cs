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
		private string userId;
		private string eventSubId;

		private IEnumerable<Sticker> randomStrickers;

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
			this.eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;

			userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
			showMemerRewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);
			sendRandomMemeRewardId = await settingsService.GetSendRandomMemeRewardIdAsync(cancellationToken).ConfigureAwait(false);

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

			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
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
				await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward.UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
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

					if (!string.IsNullOrWhiteSpace(showMemerRewardId) || !string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
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

			// Don't do this in production. You should implement a better reconnect strategy with exponential backoff
			while (!await eventSubWebsocketClient.ReconnectAsync())
			{
				logger.LogError("Websocket reconnect failed!");
				await Task.Delay(1000);
			}
		}

		private async Task OnWebsocketReconnected(object sender, WebsocketReconnectedArgs e)
		{
			logger.LogWarning("Websocket {sessionId} reconnected", eventSubWebsocketClient.SessionId);
		}

		private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
		{
			logger.LogError("Websocket {sessionId} - Error occurred!", eventSubWebsocketClient.SessionId);
		}

		private async Task EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
		{
			var rewardId = e?.Payload?.Event?.Reward?.Id;

			if (rewardId == showMemerRewardId)
			{
				logger.LogInformation(EventIds.RandomMeme, "{userName} активировал награду \"{title}\"", e.Payload.Event.UserName, e.Payload.Event.Reward.Title);

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
							string.Format(":@" + e.Payload.Event.UserName + " " + Properties.Resources.LastMemeSentBy, showMemeInfo ? $"\"{Censor(lastEvent.StickerName)}\"": string.Empty, lastEvent.UserName)
						},
					});

					await twitchClient.SendMessageAsync(msg).ConfigureAwait(false);
				}

				return;
			}
			else if (rewardId == sendRandomMemeRewardId)
			{
				logger.LogInformation(EventIds.RandomMeme, "{userName} активировал награду \"{title}\"", e.Payload.Event.UserName, e.Payload.Event.Reward.Title);

				try
				{
					if (randomStrickers.Any())
					{
						var random = Random.Shared.Next(0, randomStrickers.Count());
						var sticker = randomStrickers.ElementAt(random);
						await memeAlertsService.SendMemeAsync(sticker).ConfigureAwait(false);
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
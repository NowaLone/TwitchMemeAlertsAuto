using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
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
		private readonly ITwitchAPI twitchApi;
		private readonly EventSubWebsocketClient eventSubWebsocketClient;
		private readonly ITwitchClient twitchClient;
		private readonly IIrcParser<IrcV3Message> ircParser;
		private readonly ISettingsService settingsService;
		private readonly IMemeAlertsService memeAlertsService;
		private readonly ILogger<WebsocketHostedService> logger;

		private string showMemerRewardId;
		private string userId;
		private string eventSubId;

		public WebsocketHostedService(ITwitchAPI twitchAPI, EventSubWebsocketClient eventSubWebsocketClient, ITwitchClient twitchClient, IIrcParser<IrcV3Message> ircParser, ISettingsService settingsService, IMemeAlertsService memeAlertsService, ILogger<WebsocketHostedService> logger)
		{
			this.twitchApi = twitchAPI;
			this.eventSubWebsocketClient = eventSubWebsocketClient;
			this.twitchClient = twitchClient;
			this.ircParser = ircParser;
			this.settingsService = settingsService;
			this.memeAlertsService = memeAlertsService;
			this.logger = logger;
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			this.eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;

			await eventSubWebsocketClient.ConnectAsync();

			showMemerRewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);
			userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			this.eventSubWebsocketClient.WebsocketConnected -= OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected -= OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected -= OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred -= OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd -= EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;

			await eventSubWebsocketClient.DisconnectAsync();

			var response = await twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(eventSubId);

			if (response)
			{
				logger.LogWarning("Unable to delete event subscription {eventSubId}", eventSubId);
			}
		}

		private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
		{
			logger.LogInformation("Websocket {sessionId} connected!", eventSubWebsocketClient.SessionId);

			if (!e.IsRequestedReconnect)
			{
				var condition = new Dictionary<string, string> { { "broadcaster_user_id", userId }, { "reward_id", showMemerRewardId } };
				var response2 = await twitchApi.Helix.EventSub.GetEventSubSubscriptionsAsync(new GetEventSubSubscriptionsRequest());
				foreach (var item in response2.Subscriptions)
				{
					await twitchApi.Helix.EventSub.DeleteEventSubSubscriptionAsync(item.Id);
				}
				var response = await twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", condition, EventSubTransportMethod.Websocket, eventSubWebsocketClient.SessionId);
				eventSubId = response.Subscriptions.FirstOrDefault().Id;
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
			//logger.LogWarning("Websocket {sessionId} reconnected", eventSubWebsocketClient.SessionId);
		}

		private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
		{
			//logger.LogError("Websocket {sessionId} - Error occurred!", eventSubWebsocketClient.SessionId);
		}

		private async Task EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
		{
			var events = await memeAlertsService.GetEventsAsync().ConfigureAwait(false);
			if (events.Count != 0)
			{
				var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
				var msg = ircParser.BuildMessage(new IrcV3Message
				{
					Command = IrcCommand.PRIVMSG,
					Parameters = new List<string>
					{
						$"#{e.Payload.Event.BroadcasterUserLogin}",
						string.Format(":@" + e.Payload.Event.UserName + " " + Properties.Resources.LastMemeSentBy, "Неизвестно", lastEvent.UserName)
					},
				});
				await twitchClient.SendMessageAsync(msg).ConfigureAwait(false);
			}
		}
	}
}
using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Interfaces;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchLib.EventSub.Websockets.Core.EventArgs;

namespace TwitchMemeAlertsAuto.Core.Services
{
	public class WebsocketHostedService : IHostedService
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

		public WebsocketHostedService(ITwitchAPI twitchAPI, EventSubWebsocketClient eventSubWebsocketClient, ITwitchClient twitchClient, IIrcParser<IrcV3Message> ircParser, ISettingsService settingsService, IMemeAlertsService memeAlertsService, ILogger<WebsocketHostedService> logger)
		{
			this.twitchApi = twitchAPI;
			this.eventSubWebsocketClient = eventSubWebsocketClient;
			this.twitchClient = twitchClient;
			this.ircParser = ircParser;
			this.settingsService = settingsService;
			this.memeAlertsService = memeAlertsService;
			this.logger = logger;

			this.eventSubWebsocketClient.WebsocketConnected += OnWebsocketConnected;
			this.eventSubWebsocketClient.WebsocketDisconnected += OnWebsocketDisconnected;
			this.eventSubWebsocketClient.WebsocketReconnected += OnWebsocketReconnected;
			this.eventSubWebsocketClient.ErrorOccurred += OnErrorOccurred;

			this.eventSubWebsocketClient.ChannelPointsCustomRewardRedemptionAdd += EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd;
		}

		private async Task EventSubWebsocketClient_ChannelPointsCustomRewardRedemptionAdd(object sender, ChannelPointsCustomRewardRedemptionArgs e)
		{
			var events = await memeAlertsService.GetEventsAsync().ConfigureAwait(false);
			if (events.Count != 0)
			{
				var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
				var msg = ircParser.BuildMessage(new IrcV3Message { Command = IrcCommand.PRIVMSG, Parameters = new List<string> { $"#{e.Payload.Event.BroadcasterUserLogin}", string.Format(":@" + e.Payload.Event.UserName + " " + Properties.Resources.LastMemeSentBy, "Неизвестно", lastEvent.UserName) } });
				await twitchClient.SendMessageAsync(msg).ConfigureAwait(false);
			}
		}

		public async Task StartAsync(CancellationToken cancellationToken = default)
		{
			await eventSubWebsocketClient.ConnectAsync();

			showMemerRewardId = await settingsService.GetShowMemerRewardIdAsync(cancellationToken).ConfigureAwait(false);
			userId = await settingsService.GetTwitchUserIdAsync(cancellationToken).ConfigureAwait(false);
		}

		public async Task StopAsync(CancellationToken cancellationToken = default)
		{
			await eventSubWebsocketClient.DisconnectAsync();
		}

		private async Task OnWebsocketConnected(object sender, WebsocketConnectedArgs e)
		{
			logger.LogInformation($"Websocket {eventSubWebsocketClient.SessionId} connected!");

			if (!e.IsRequestedReconnect)
			{
				// subscribe to topics
				// create condition Dictionary
				// You need BOTH broadcaster and moderator values or EventSub returns an Error!
				var condition = new Dictionary<string, string> { { "broadcaster_user_id", userId }, { "reward_id", showMemerRewardId } };
				// Create and send EventSubscription
				await twitchApi.Helix.EventSub.CreateEventSubSubscriptionAsync("channel.channel_points_custom_reward_redemption.add", "1", condition, EventSubTransportMethod.Websocket, eventSubWebsocketClient.SessionId);
				// If you want to get Events for special Events you need to additionally add the AccessToken of the ChannelOwner to the request.
				// https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
			}
		}

		private async Task OnWebsocketDisconnected(object sender, WebsocketDisconnectedArgs e)
		{
			logger.LogError($"Websocket {eventSubWebsocketClient.SessionId} disconnected!");

			// Don't do this in production. You should implement a better reconnect strategy with exponential backoff
			while (!await eventSubWebsocketClient.ReconnectAsync())
			{
				logger.LogError("Websocket reconnect failed!");
				await Task.Delay(1000);
			}
		}

		private async Task OnWebsocketReconnected(object sender, WebsocketReconnectedArgs e)
		{
			logger.LogWarning($"Websocket {eventSubWebsocketClient.SessionId} reconnected");
		}

		private async Task OnErrorOccurred(object sender, ErrorOccuredArgs e)
		{
			logger.LogError($"Websocket {eventSubWebsocketClient.SessionId} - Error occurred!");
		}
	}
}
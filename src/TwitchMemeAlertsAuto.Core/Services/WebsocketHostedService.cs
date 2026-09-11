using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.EntityFrameworkCore;
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
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomReward;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
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
		private readonly IDbContextFactory<TmaaDbContext> dbContextFactory;
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

		public WebsocketHostedService(EventSubWebsocketClient eventSubWebsocketClient, ITwitchClient twitchClient, IIrcParser<IrcV3Message> ircParser, ISettingsService settingsService, IMemeAlertsService memeAlertsService, IProfanityFilter profanityFilter, IServiceProvider serviceProvider, IDbContextFactory<TmaaDbContext> dbContextFactory, ILogger<WebsocketHostedService> logger)
		{
			this.eventSubWebsocketClient = eventSubWebsocketClient;
			this.twitchClient = twitchClient;
			this.ircParser = ircParser;
			this.settingsService = settingsService;
			this.memeAlertsService = memeAlertsService;
			this.profanityFilter = profanityFilter;
			this.serviceProvider = serviceProvider;
			this.dbContextFactory = dbContextFactory;
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
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendMemeWithTextId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendMemeWithTextId, new UpdateCustomRewardRequest { IsPaused = false }).ConfigureAwait(false);
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
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, showMemerRewardId, new UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendRandomMemeRewardId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendRandomMemeRewardId, new UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
				}

				if (!string.IsNullOrWhiteSpace(sendMemeWithTextId))
				{
					await twitchAPI.Helix.ChannelPoints.UpdateCustomRewardAsync(userId, sendMemeWithTextId, new UpdateCustomRewardRequest { IsPaused = true }).ConfigureAwait(false);
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
			var broadcasterUserLogin = e.Payload.Event.BroadcasterUserLogin;

			if (string.IsNullOrWhiteSpace(rewardId))
			{
				return;
			}

			try
			{
				if (rewardId == showMemerRewardId)
				{
					logger.LogInformation(EventIds.Info, Properties.Resources.RewardRedeemed, userName, e.Payload.Event.Reward.Title);
					await ShowMemerWork(e, userName, serviceCancellationToken).ConfigureAwait(false);
				}
				else if (rewardId == sendRandomMemeRewardId)
				{
					logger.LogInformation(EventIds.Info, Properties.Resources.RewardRedeemed, userName, e.Payload.Event.Reward.Title);
					await SendRandomMemeWork(e, userName, serviceCancellationToken).ConfigureAwait(false);
				}
				else if (rewardId == sendMemeWithTextId)
				{
					logger.LogInformation(EventIds.Info, Properties.Resources.RewardRedeemed, userName, e.Payload.Event.Reward.Title);
					await SendMemeWithTextWork(e, userName, serviceCancellationToken).ConfigureAwait(false);
				}
				else if (rewards.TryGetValue(rewardId, out var value))
				{
					logger.LogInformation(EventIds.Info, Properties.Resources.RewardRedeemed, userName, e.Payload.Event.Reward.Title);
					await RewardWork(e, userName, value, serviceCancellationToken).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException ex)
			{
				logger.LogTrace(ex, default);
			}
			catch (Exception ex)
			{
				await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

				var logText = Properties.Resources.ErrorWhileHandleRedeem + ", " + Properties.Resources.PointsReturned;
				logger.LogError(EventIds.Error, ex, logText, e.Payload.Event.Reward.Title);
				await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}").ConfigureAwait(false);
			}
		}

		private async Task ShowMemerWork(ChannelPointsCustomRewardRedemptionArgs e, string userName, CancellationToken cancellationToken = default)
		{
			var events = await memeAlertsService.GetEventsAsync(cancellationToken).ConfigureAwait(false);
			var showMemeInfo = await settingsService.GetShowMemerWithMemeInfoAsync(cancellationToken).ConfigureAwait(false);
			var broadcasterUserLogin = e.Payload.Event.BroadcasterUserLogin;

			if (events.Count != 0)
			{
				var lastEvent = events.OrderByDescending(e => e.Timestamp).FirstOrDefault();
				await LogIntoChat(broadcasterUserLogin, string.Format($":@{userName} {Properties.Resources.LastMemeSentBy}", showMemeInfo ? $"\"{Censor(lastEvent.StickerName)}\"" : string.Empty, lastEvent.UserName), cancellationToken).ConfigureAwait(false);
			}
			else
			{
				await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

				var logText = Properties.Resources.EventsNotFound + ", " + Properties.Resources.PointsReturned;
				logger.LogWarning(EventIds.Warning, logText);
				await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}", cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task SendRandomMemeWork(ChannelPointsCustomRewardRedemptionArgs e, string userName, CancellationToken cancellationToken = default)
		{
			var broadcasterUserLogin = e.Payload.Event.BroadcasterUserLogin;

			if (randomStrickers.Any())
			{
				var sticker = randomStrickers.ElementAt(Random.Shared.Next(0, randomStrickers.Count()));
				await GiveBounsYourself(cancellationToken).ConfigureAwait(false);

				if (await memeAlertsService.SendMemeAsync(sticker, userName, cancellationToken).ConfigureAwait(false))
				{
					using (var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
					{
						await ctx.MemeHistories.AddAsync(new MemeHistory
						{
							Sticker = await ctx.Stickers.FirstOrDefaultAsync(s => s.StickerId == sticker.Id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new StickerInfo
							{
								StickerId = sticker.Id,
								Name = sticker.Name,
							},
							Type = MemeHistoryType.Random,
							BroadcasterUserId = e.Payload.Event.BroadcasterUserId,
							UserName = e.Payload.Event.UserName,
							UserId = e.Payload.Event.UserId,
							UserInput = e.Payload.Event.UserInput,
							Cost = e.Payload.Event.Reward.Cost,
							RewardId = e.Payload.Event.Reward.Id,
							Timestamp = DateTimeOffset.UtcNow,
						}, cancellationToken).ConfigureAwait(false);

						await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
					}
				}
			}
			else
			{
				await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

				var logText = Properties.Resources.StickersNotFound + ", " + Properties.Resources.PointsReturned;
				logger.LogWarning(EventIds.Warning, logText);
				await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}", cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task SendMemeWithTextWork(ChannelPointsCustomRewardRedemptionArgs e, string userName, CancellationToken cancellationToken = default)
		{
			var stickers = await memeAlertsService.GetPersonalAreaSearchAsync(e.Payload.Event.UserInput, cancellationToken).ConfigureAwait(false);
			var broadcasterUserLogin = e.Payload.Event.BroadcasterUserLogin;

			if (stickers.Any())
			{
				var sticker = stickers.FindBest(e.Payload.Event.UserInput);
				await GiveBounsYourself(cancellationToken).ConfigureAwait(false);

				if (await memeAlertsService.SendMemeAsync(sticker, userName, cancellationToken).ConfigureAwait(false))
				{
					using (var ctx = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false))
					{
						await ctx.MemeHistories.AddAsync(new MemeHistory
						{
							Sticker = await ctx.Stickers.FirstOrDefaultAsync(s => s.StickerId == sticker.Id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? new StickerInfo
							{
								StickerId = sticker.Id,
								Name = sticker.Name,
							},
							Type = MemeHistoryType.Text,
							BroadcasterUserId = e.Payload.Event.BroadcasterUserId,
							UserName = e.Payload.Event.UserName,
							UserId = e.Payload.Event.UserId,
							UserInput = e.Payload.Event.UserInput,
							Cost = e.Payload.Event.Reward.Cost,
							RewardId = e.Payload.Event.Reward.Id,
							Timestamp = DateTimeOffset.UtcNow,
						}, cancellationToken).ConfigureAwait(false);

						await ctx.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
					}
				}
			}
			else
			{
				await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

				var logText = string.Format(Properties.Resources.StickersNotFoundByRequest, e.Payload.Event.UserInput) + ", " + Properties.Resources.PointsReturned;
				logger.LogWarning(EventIds.Warning, logText);
				await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}", cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task RewardWork(ChannelPointsCustomRewardRedemptionArgs e, string userName, int value, CancellationToken cancellationToken = default)
		{
			var broadcasterUserLogin = e.Payload.Event.BroadcasterUserLogin;
			var usernameInMemeAlerts = e?.Payload?.Event?.UserInput;

			await rewardProcessingSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			var supporter = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));

			if (supporter == null)
			{
				supporters = await memeAlertsService.GetSupportersAsync(cancellationToken).ConfigureAwait(false);
				rewardProcessingSemaphore.Release();

				supporter = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));

				if (supporter == null && tryRewardWithWrongNickname && !string.IsNullOrWhiteSpace(userName))
				{
					logger.LogWarning(EventIds.NotFound, Properties.Resources.SupporterNotFound + ", " + Properties.Resources.RetryWithTwitchNickname, usernameInMemeAlerts);
					usernameInMemeAlerts = userName;
					supporter = supporters.FirstOrDefault(d => string.Equals(d.SupporterName, usernameInMemeAlerts, StringComparison.OrdinalIgnoreCase));
				}
			}
			else
			{
				rewardProcessingSemaphore.Release();
			}

			if (supporter != null)
			{
				if (await memeAlertsService.GiveBonusAsync(supporter, value, cancellationToken).ConfigureAwait(false))
				{
					logger.LogInformation(EventIds.Rewarded, Properties.Resources.SupporterSuccessfullyRewarded, usernameInMemeAlerts, value);
				}
				else
				{
					await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

					var logText = Properties.Resources.ErrorWhileGiveBonus + ", " + Properties.Resources.SupporterNotRewarded.ToLower() + ", " + Properties.Resources.PointsReturned;
					logger.LogError(EventIds.NotRewarded, logText, usernameInMemeAlerts);
					await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}", cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				await CancelReward(e.Payload.Event.BroadcasterUserId, e.Payload.Event.Reward.Id, e.Payload.Event.Id).ConfigureAwait(false);

				var logText = Properties.Resources.SupporterNotFound + ", " + Properties.Resources.PointsReturned;
				logger.LogWarning(EventIds.Warning, logText, usernameInMemeAlerts);
				await LogIntoChat(broadcasterUserLogin, $":@{userName} {logText}", cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task LogIntoChat(string broadcasterUserLogin, string text, CancellationToken cancellationToken = default)
		{
			var msg = ircParser.BuildMessage(new IrcV3Message
			{
				Command = IrcCommand.PRIVMSG,
				Parameters = new List<string>
						{
							$"#{broadcasterUserLogin}",
							text
						},
			});

			await twitchClient.SendMessageAsync(msg, cancellationToken).ConfigureAwait(false);
		}

		private async Task GiveBounsYourself(CancellationToken cancellationToken = default)
		{
			var supporter = await memeAlertsService.GetStreamerAsSupporterAsync(cancellationToken).ConfigureAwait(false);

			if (supporter.Balance == 0)
			{
				await memeAlertsService.GiveBonusAsync(supporter, 1, cancellationToken).ConfigureAwait(false);
			}
		}

		private async Task CancelReward(string broadcasterId, string rewardId, string redemptionId)
		{
			using (var scope = serviceProvider.CreateAsyncScope())
			{
				var twitchAPI = scope.ServiceProvider.GetRequiredService<ITwitchAPI>();
				await twitchAPI.Helix.ChannelPoints.UpdateRedemptionStatusAsync(broadcasterId, rewardId, new List<string> { redemptionId }, new UpdateCustomRewardRedemptionStatusRequest { Status = CustomRewardRedemptionStatus.CANCELED }).ConfigureAwait(false);
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
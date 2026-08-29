using IrcNet;
using IrcNet.Parser.V3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProfanityFilter.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using TwitchChat.Client;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.Models;
using TwitchLib.EventSub.Core.Models.ChannelPoints;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;
using TwitchLib.EventSub.Websockets;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Services;

namespace TwitchMemeAlertsAuto.Core.Tests.Services;

[TestClass]
public class WebsocketHostedServiceTests
{
	private const BindingFlags NonPublicInstance = BindingFlags.Instance | BindingFlags.NonPublic;

	private readonly Mock<IMemeAlertsService> memeAlertsMock = new();
	private readonly Mock<ISettingsService> settingsServiceMock = new();
	private readonly Mock<ITwitchClient> twitchClientMock = new();

	private WebsocketHostedService CreateService()
	{
		return new WebsocketHostedService(
			new EventSubWebsocketClient(),
			twitchClientMock.Object,
			Mock.Of<IIrcParser<IrcV3Message>>(),
			settingsServiceMock.Object,
			memeAlertsMock.Object,
			Mock.Of<IProfanityFilter>(),
			Mock.Of<IServiceProvider>(),
			Mock.Of<ILogger<WebsocketHostedService>>());
	}

	private static ChannelPointsCustomRewardRedemptionArgs CreateRewardArgs(string rewardId, string userInput = null, string userLogin = null)
		=> new()
		{
			Payload = new EventSubNotificationPayload<ChannelPointsCustomRewardRedemption>
			{
				Event = new ChannelPointsCustomRewardRedemption
				{
					UserInput = userInput,
					UserLogin = userLogin,
					UserName = userInput,
					BroadcasterUserLogin = "testchannel",
					Reward = new RedemptionReward { Id = rewardId, Title = "Test Reward" },
				},
			},
		};

	private static void SetRewards(WebsocketHostedService service, IDictionary<string, int> rewards)
		=> typeof(WebsocketHostedService).GetField("rewards", NonPublicInstance)!.SetValue(service, rewards);

	private static void SetSupporters(WebsocketHostedService service, List<Supporter> supporters)
		=> typeof(WebsocketHostedService).GetField("supporters", NonPublicInstance)!.SetValue(service, supporters);

	private static void SetTryRewardWithWrongNickname(WebsocketHostedService service, bool value)
		=> typeof(WebsocketHostedService).GetField("tryRewardWithWrongNickname", NonPublicInstance)!.SetValue(service, value);

	private static Task HandleGenericRewardAsync(WebsocketHostedService service, string rewardId, ChannelPointsCustomRewardRedemptionArgs e)
	{
		var method = typeof(WebsocketHostedService).GetMethod("HandleGenericRewardAsync", NonPublicInstance)
			?? throw new MissingMethodException(nameof(WebsocketHostedService), "HandleGenericRewardAsync");
		return (Task)method.Invoke(service, new object[] { rewardId, e });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(WebsocketHostedService))]
	[TestCategory(nameof(WebsocketHostedService))]
	public void Constructor_WithDependencies_CreatesService()
	{
		// Act
		var service = CreateService();

		// Assert
		Assert.IsNotNull(service);
	}

	#endregion

	// __HANDLE_GENERIC_REWARD_REGION__
}

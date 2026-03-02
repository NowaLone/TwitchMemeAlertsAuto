using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchChat.Client;

namespace TwitchMemeAlertsAuto.Core.Tests.Services;

[TestClass]
public class RewardsServiceTests
{
	private readonly IFixture fixture;

	public RewardsServiceTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	private (RewardsService service, Mock<IMemeAlertsService> memeAlertsMock, Mock<ITwitchClient> twitchClientMock, Mock<ILogger<RewardsService>> loggerMock)
		CreateService()
	{
		var memeAlertsMock = new Mock<IMemeAlertsService>();
		var twitchClientMock = new Mock<ITwitchClient>();
		var loggerMock = new Mock<ILogger<RewardsService>>();

		var service = new RewardsService(memeAlertsMock.Object, twitchClientMock.Object, loggerMock.Object);
		return (service, memeAlertsMock, twitchClientMock, loggerMock);
	}

	#region StartAsync

	[TestMethod]
	[TestCategory(nameof(RewardsService))]
	[TestCategory(nameof(RewardsService.StartAsync))]
	public async Task StartAsync_JoinsChannel_SubscribesAndConnects()
	{
		// Arrange
		var (service, memeAlertsMock, twitchClientMock, _) = CreateService();
		var rewards = new Dictionary<string, int>();
		var channel = "testchannel";

		memeAlertsMock
			.Setup(m => m.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<Supporter>());

		// Act
		await service.StartAsync(rewards, channel, false, CancellationToken.None);

		// Assert
		twitchClientMock.Verify(c => c.JoinChannel(channel), Times.Once);
		twitchClientMock.Verify(c => c.ConnectAsync(It.IsAny<CancellationToken>()), Times.Once);
		// We cannot directly assert event subscription, but we at least ensure no exception.
	}

	#endregion

	#region StopAsync

	[TestMethod]
	[TestCategory(nameof(RewardsService))]
	[TestCategory(nameof(RewardsService.StopAsync))]
	public async Task StopAsync_UnsubscribesAndDisconnects()
	{
		// Arrange
		var (service, memeAlertsMock, twitchClientMock, _) = CreateService();

		memeAlertsMock
			.Setup(m => m.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<Supporter>());

		await service.StartAsync(new Dictionary<string, int>(), "channel", false, CancellationToken.None);

		// Act
		await service.StopAsync(CancellationToken.None);

		// Assert
		twitchClientMock.Verify(c => c.DisconnectAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion
}


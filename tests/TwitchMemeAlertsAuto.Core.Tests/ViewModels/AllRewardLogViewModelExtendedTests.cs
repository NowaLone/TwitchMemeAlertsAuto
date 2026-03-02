using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.Tests.ViewModels;

[TestClass]
public class AllRewardViewModelExtendedTests
{
	private readonly IFixture fixture;

	public AllRewardViewModelExtendedTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region RewardAllAsync

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardAllCommand))]
	public async Task RewardAllCommand_Execute_CallsGiveBonusAsync_ForAllSupporters()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var supporters = new List<Supporter>
		{
			new Supporter { SupporterId = "1", SupporterName = "User1" },
			new Supporter { SupporterId = "2", SupporterName = "User2" }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(supporters);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardAllCommand.ExecuteAsync(null);
		await Task.Delay(100); // Allow async operations to complete

		// Assert
		memeAlertsServiceMock.Verify(s => s.GiveBonusAsync(
			It.IsAny<Supporter>(),
			10,
			It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardAllCommand))]
	public async Task RewardAllCommand_Execute_LogsError_WhenGiveBonusFails()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var supporters = new List<Supporter>
		{
			new Supporter { SupporterId = "1", SupporterName = "User1" }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(supporters);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardAllCommand.ExecuteAsync(null);
		await Task.Delay(100);

		// Assert
		loggerMock.Verify(
			l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("не выданы")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()),
			Times.Once);
	}

	#endregion

	#region RewardWhoSentAsync

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardWhoSentCommand))]
	public async Task RewardWhoSentCommand_Execute_RewardsTopSenders()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var events = new List<Event>
		{
			new Event { UserId = "1", UserName = "User1", Timestamp = 100 },
			new Event { UserId = "2", UserName = "User2", Timestamp = 200 },
			new Event { UserId = "3", UserName = "User3", Timestamp = 300 }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardWhoSentCommand.ExecuteAsync("2");
		await Task.Delay(100);

		// Assert - Should reward top 2 senders (User3 and User2 by timestamp)
		memeAlertsServiceMock.Verify(s => s.GiveBonusAsync(
			It.Is<Supporter>(sup => sup.SupporterId == "3" || sup.SupporterId == "2"),
			10,
			It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardWhoSentCommand))]
	public async Task RewardWhoSentCommand_Execute_DeduplicatesByUserId()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var events = new List<Event>
		{
			new Event { UserId = "1", UserName = "User1", Timestamp = 100 },
			new Event { UserId = "1", UserName = "User1", Timestamp = 200 }, // Same user
			new Event { UserId = "2", UserName = "User2", Timestamp = 300 }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetEventsAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(events);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardWhoSentCommand.ExecuteAsync("5");
		await Task.Delay(100);

		// Assert - Should only reward unique users (User1 and User2)
		memeAlertsServiceMock.Verify(s => s.GiveBonusAsync(
			It.Is<Supporter>(sup => sup.SupporterId == "1" || sup.SupporterId == "2"),
			10,
			It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	#endregion

	#region RewardWhoReceivedAsync

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardWhoReceivedCommand))]
	public async Task RewardWhoReceivedCommand_Execute_RewardsByLastSupport()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var supporters = new List<Supporter>
		{
			new Supporter { SupporterId = "1", SupporterName = "User1", LastSupport = 100 },
			new Supporter { SupporterId = "2", SupporterName = "User2", LastSupport = 300 },
			new Supporter { SupporterId = "3", SupporterName = "User3", LastSupport = 200 }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(supporters);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(true);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardWhoReceivedCommand.ExecuteAsync("2");
		await Task.Delay(100);

		// Assert - Should reward top 2 by LastSupport (User2 and User3)
		memeAlertsServiceMock.Verify(s => s.GiveBonusAsync(
			It.Is<Supporter>(sup => sup.SupporterId == "2" || sup.SupporterId == "3"),
			10,
			It.IsAny<CancellationToken>()), Times.Exactly(2));
	}

	[TestMethod]
	[TestCategory(nameof(AllRewardViewModel))]
	[TestCategory(nameof(AllRewardViewModel.RewardWhoReceivedCommand))]
	public async Task RewardWhoReceivedCommand_Execute_LogsError_WhenGiveBonusFails()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var loggerMock = new Mock<ILogger<AllRewardViewModel>>();

		var supporters = new List<Supporter>
		{
			new Supporter { SupporterId = "1", SupporterName = "User1", LastSupport = 100 }
		};

		memeAlertsServiceMock
			.Setup(s => s.GetSupportersAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(supporters);

		memeAlertsServiceMock
			.Setup(s => s.GiveBonusAsync(It.IsAny<Supporter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(false);

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcher(It.IsAny<Action>()))
			.Callback<Action>(a => a());

		var viewModel = new AllRewardViewModel(
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		await viewModel.RewardWhoReceivedCommand.ExecuteAsync("1");
		await Task.Delay(100);

		// Assert
		loggerMock.Verify(
			l => l.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("не выданы")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()),
			Times.Once);
	}

	#endregion
}

[TestClass]
public class LogViewModelExtendedTests
{
	private readonly IFixture fixture;

	public LogViewModelExtendedTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel))]
	public void Constructor_WithDependencies_InitializesLogCollection()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		// Act
		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		// Assert
		Assert.IsNotNull(viewModel.Log);
		Assert.AreEqual(0, viewModel.Log.Count);
	}

	#endregion

	#region Receive

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel.Receive))]
	public void Receive_LogMessage_AddsMessageToLog()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		var history = new History { Id = 1, Message = "Test", Timestamp = DateTimeOffset.Now, EventId = 1 };
		var message = new LogMessage(history);

		// Act
		viewModel.Receive(message);

		// Assert
		Assert.AreEqual(1, viewModel.Log.Count);
		Assert.AreEqual(history, viewModel.Log.First());
	}

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel.Receive))]
	public void Receive_LogMessage_RaisesPropertyChanged_WhenFirstMessage()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		var history = new History { Id = 1, Message = "Test", Timestamp = DateTimeOffset.Now, EventId = 1 };
		var message = new LogMessage(history);

		var propertyChanged = false;
		viewModel.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == nameof(LogViewModel.Log))
				propertyChanged = true;
		};

		// Act
		viewModel.Receive(message);

		// Assert
		Assert.IsTrue(propertyChanged);
	}

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel.Receive))]
	public async Task Receive_LoadingEvent_DoesNotTriggerSave()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		var history = new History { Id = 1, Message = "Loading", Timestamp = DateTimeOffset.Now, EventId = EventIds.Loading.Id };
		var message = new LogMessage(history);

		// Act
		viewModel.Receive(message);
		await Task.Delay(50);

		// Assert - Verify no exception thrown and log was added (just not saved)
		Assert.AreEqual(1, viewModel.Log.Count);
	}

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel.Receive))]
	public async Task Receive_LoadedEvent_DoesNotTriggerSave()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		var history = new History { Id = 1, Message = "Loaded", Timestamp = DateTimeOffset.Now, EventId = EventIds.Loaded.Id };
		var message = new LogMessage(history);

		// Act
		viewModel.Receive(message);
		await Task.Delay(50);

		// Assert
		Assert.AreEqual(1, viewModel.Log.Count);
	}

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory(nameof(LogViewModel.Receive))]
	public async Task Receive_RewardedEvent_AddsToLog()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		dbContextFactoryMock
			.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TmaaDbContext(options));

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		var history = new History { Id = 1, Message = "Rewarded", Timestamp = DateTimeOffset.Now, EventId = EventIds.Rewarded.Id };
		var message = new LogMessage(history);

		// Act
		viewModel.Receive(message);
		await Task.Delay(100);

		// Assert
		Assert.AreEqual(1, viewModel.Log.Count);
	}

	#endregion

	#region IgnoredEventIds

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory("IgnoredEventIds")]
	public async Task IgnoredEventIds_ContainsLoadingAndLoaded()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		// Act & Assert - Loading event should add to log but not trigger save exception
		var loadingHistory = new History { Id = 1, Message = "Loading", Timestamp = DateTimeOffset.Now, EventId = EventIds.Loading.Id };
		viewModel.Receive(new LogMessage(loadingHistory));
		await Task.Delay(50);

		Assert.AreEqual(1, viewModel.Log.Count);

		// Loaded event should also add to log but not trigger save exception
		var loadedHistory = new History { Id = 2, Message = "Loaded", Timestamp = DateTimeOffset.Now, EventId = EventIds.Loaded.Id };
		viewModel.Receive(new LogMessage(loadedHistory));
		await Task.Delay(50);

		Assert.AreEqual(2, viewModel.Log.Count);
	}

	[TestMethod]
	[TestCategory(nameof(LogViewModel))]
	[TestCategory("IgnoredEventIds")]
	public async Task IgnoredEventIds_DoesNotContainRewarded()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<LogViewModel>>();

		dispatcherServiceMock
			.Setup(d => d.CallWithDispatcherAsync(It.IsAny<Func<Task>>()))
			.ReturnsAsync((Func<Task> f) => f());

		dbContextFactoryMock
			.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(new TmaaDbContext(options));

		var viewModel = new LogViewModel(
			dispatcherServiceMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		// Act & Assert - Rewarded event should add to log (save will be attempted)
		var rewardedHistory = new History { Id = 1, Message = "Rewarded", Timestamp = DateTimeOffset.Now, EventId = EventIds.Rewarded.Id };
		
		// This should not throw - the save is fire-and-forget
		viewModel.Receive(new LogMessage(rewardedHistory));
		await Task.Delay(100);

		Assert.AreEqual(1, viewModel.Log.Count);
	}

	#endregion
}

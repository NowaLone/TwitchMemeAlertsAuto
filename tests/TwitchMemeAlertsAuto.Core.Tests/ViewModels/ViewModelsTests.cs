using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.Tests.ViewModels;

[TestClass]
public class RewardsViewModelTests
{
	private readonly IFixture fixture;

	public RewardsViewModelTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(RewardsViewModel))]
	[TestCategory(nameof(RewardsViewModel))]
	public void Constructor_WithDependencies_CreatesViewModel()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var loggerMock = new Mock<ILogger<RewardsViewModel>>();

		// Act
		var viewModel = new RewardsViewModel(
			serviceProviderMock.Object,
			loggerMock.Object);

		// Assert
		Assert.IsNotNull(viewModel);
		Assert.IsNotNull(viewModel.Rewards);
		Assert.AreEqual(0, viewModel.Rewards.Count);
		Assert.IsFalse(viewModel.IsLoading);
	}

	#endregion

	#region RefreshCommand

	[TestMethod]
	[TestCategory(nameof(RewardsViewModel))]
	[TestCategory(nameof(RewardsViewModel.RefreshCommand))]
	public void RefreshCommand_CanExecute_ReturnsTrue_WhenNotLoading()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var loggerMock = new Mock<ILogger<RewardsViewModel>>();

		var viewModel = new RewardsViewModel(
			serviceProviderMock.Object,
			loggerMock.Object);

		// Act
		var canExecute = viewModel.RefreshCommand.CanExecute(null);

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(RewardsViewModel))]
	[TestCategory(nameof(RewardsViewModel.RefreshCommand))]
	public void RefreshCommand_CanExecute_ReturnsFalse_WhenLoading()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var loggerMock = new Mock<ILogger<RewardsViewModel>>();

		var viewModel = new RewardsViewModel(
			serviceProviderMock.Object,
			loggerMock.Object);

		viewModel.IsLoading = true;

		// Act
		var canExecute = viewModel.RefreshCommand.CanExecute(null);

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion
}

[TestClass]
public class SupportersViewModelTests
{
	private readonly IFixture fixture;

	public SupportersViewModelTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(SupportersViewModel))]
	[TestCategory(nameof(SupportersViewModel))]
	public void Constructor_WithDependencies_CreatesViewModel()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupportersViewModel>>();

		// Act
		var viewModel = new SupportersViewModel(
			serviceProviderMock.Object,
			dispatcherServiceMock.Object,
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Assert
		Assert.IsNotNull(viewModel);
		Assert.IsNotNull(viewModel.Supporters);
		Assert.AreEqual(0, viewModel.Supporters.Count);
		Assert.IsFalse(viewModel.IsLoading);
	}

	#endregion

	#region RefreshCommand

	[TestMethod]
	[TestCategory(nameof(SupportersViewModel))]
	[TestCategory(nameof(SupportersViewModel.RefreshCommand))]
	public void RefreshCommand_CanExecute_ReturnsTrue_WhenNotLoading()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupportersViewModel>>();

		var viewModel = new SupportersViewModel(
			serviceProviderMock.Object,
			dispatcherServiceMock.Object,
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Act
		var canExecute = viewModel.RefreshCommand.CanExecute(null);

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(SupportersViewModel))]
	[TestCategory(nameof(SupportersViewModel.RefreshCommand))]
	public void RefreshCommand_CanExecute_ReturnsFalse_WhenLoading()
	{
		// Arrange
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupportersViewModel>>();

		var viewModel = new SupportersViewModel(
			serviceProviderMock.Object,
			dispatcherServiceMock.Object,
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		viewModel.IsLoading = true;

		// Act
		var canExecute = viewModel.RefreshCommand.CanExecute(null);

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion
}

[TestClass]
public class MainMenuViewModelTests
{
	private readonly IFixture fixture;

	public MainMenuViewModelTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(MainMenuViewModel))]
	[TestCategory(nameof(MainMenuViewModel))]
	public void Constructor_WithDependencies_CreatesViewModel()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();
		var connectionViewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			Mock.Of<IRewardsService>(),
			Mock.Of<ITwitchOAuthService>(),
			Mock.Of<IMemeAlertsService>(),
			dispatcherServiceMock.Object,
			Mock.Of<IServiceProvider>(),
			Mock.Of<IDbContextFactory<TmaaDbContext>>(),
			Mock.Of<ILogger<ConnectionViewModel>>());
		var loggerMock = new Mock<ILogger<MainMenuViewModel>>();

		// Act
		var viewModel = new MainMenuViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object,
			connectionViewModel,
			loggerMock.Object);

		// Assert
		Assert.IsNotNull(viewModel);
		Assert.IsNotNull(viewModel.ConnectionViewModel);
	}

	#endregion

	#region Commands

	[TestMethod]
	[TestCategory(nameof(MainMenuViewModel))]
	[TestCategory(nameof(MainMenuViewModel.ExitCommand))]
	public void ExitCommand_CallsDispatcherShutdown()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();
		var connectionViewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			Mock.Of<IRewardsService>(),
			Mock.Of<ITwitchOAuthService>(),
			Mock.Of<IMemeAlertsService>(),
			dispatcherServiceMock.Object,
			Mock.Of<IServiceProvider>(),
			Mock.Of<IDbContextFactory<TmaaDbContext>>(),
			Mock.Of<ILogger<ConnectionViewModel>>());
		var loggerMock = new Mock<ILogger<MainMenuViewModel>>();

		var viewModel = new MainMenuViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object,
			connectionViewModel,
			loggerMock.Object);

		// Act
		viewModel.ExitCommand.Execute(null);

		// Assert
		dispatcherServiceMock.Verify(d => d.Shutdown(), Times.Once);
	}

	[TestMethod]
	[TestCategory(nameof(MainMenuViewModel))]
	[TestCategory(nameof(MainMenuViewModel.AboutCommand))]
	public void AboutCommand_CallsShowMessage()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();
		var connectionViewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			Mock.Of<IRewardsService>(),
			Mock.Of<ITwitchOAuthService>(),
			Mock.Of<IMemeAlertsService>(),
			dispatcherServiceMock.Object,
			Mock.Of<IServiceProvider>(),
			Mock.Of<IDbContextFactory<TmaaDbContext>>(),
			Mock.Of<ILogger<ConnectionViewModel>>());
		var loggerMock = new Mock<ILogger<MainMenuViewModel>>();

		var viewModel = new MainMenuViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object,
			connectionViewModel,
			loggerMock.Object);

		// Act
		viewModel.AboutCommand.Execute(null);

		// Assert
		dispatcherServiceMock.Verify(d => d.ShowMessage(It.IsAny<string>()), Times.Once);
	}

	[TestMethod]
	[TestCategory(nameof(MainMenuViewModel))]
	[TestCategory(nameof(MainMenuViewModel.CheckForUpdatesCommand))]
	public void CheckForUpdatesCommand_CallsCheckForUpdates()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();
		var connectionViewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			Mock.Of<IRewardsService>(),
			Mock.Of<ITwitchOAuthService>(),
			Mock.Of<IMemeAlertsService>(),
			dispatcherServiceMock.Object,
			Mock.Of<IServiceProvider>(),
			Mock.Of<IDbContextFactory<TmaaDbContext>>(),
			Mock.Of<ILogger<ConnectionViewModel>>());
		var loggerMock = new Mock<ILogger<MainMenuViewModel>>();

		var viewModel = new MainMenuViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object,
			connectionViewModel,
			loggerMock.Object);

		// Act
		viewModel.CheckForUpdatesCommand.Execute(null);

		// Assert
		dispatcherServiceMock.Verify(d => d.CheckForUpdates(), Times.Once);
	}

	#endregion

	#region SetTryRewardWithWrongNickname

	[TestMethod]
	[TestCategory(nameof(MainMenuViewModel))]
	[TestCategory(nameof(MainMenuViewModel.SetTryRewardWithWrongNicknameCommand))]
	public async Task SetTryRewardWithWrongNicknameCommand_SavesSetting()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();
		var connectionViewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			Mock.Of<IRewardsService>(),
			Mock.Of<ITwitchOAuthService>(),
			Mock.Of<IMemeAlertsService>(),
			dispatcherServiceMock.Object,
			Mock.Of<IServiceProvider>(),
			Mock.Of<IDbContextFactory<TmaaDbContext>>(),
			Mock.Of<ILogger<ConnectionViewModel>>());
		var loggerMock = new Mock<ILogger<MainMenuViewModel>>();

		var viewModel = new MainMenuViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object,
			connectionViewModel,
			loggerMock.Object);

		// Act
		viewModel.TryRewardWithWrongNickname = true;
		await viewModel.SetTryRewardWithWrongNicknameCommand.ExecuteAsync(CancellationToken.None);

		// Assert
		settingsServiceMock.Verify(s => s.SetTryRewardWithWrongNicknameOptionAsync(true, It.IsAny<CancellationToken>()), Times.Once);
	}

	#endregion
}

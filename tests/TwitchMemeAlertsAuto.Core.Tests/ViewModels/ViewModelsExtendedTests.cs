using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
public class ConnectionViewModelExtendedTests
{
	private readonly IFixture fixture;

	public ConnectionViewModelExtendedTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Receive

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory(nameof(ConnectionViewModel.Receive))]
	public void Receive_SettingsChangedMessage_DoesNotCallStartWork_WhenTwitchNotConnected()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsTwitchConnected = false;
		viewModel.IsMemeAlertsConnected = true;

		var message = new SettingsChangedMessage("TestSetting");

		// Act
		viewModel.Receive(message);

		// Assert
		rewardsServiceMock.Verify(r => r.StartAsync(It.IsAny<IDictionary<string, int>>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory(nameof(ConnectionViewModel.Receive))]
	public void Receive_SettingsChangedMessage_DoesNotCallStartWork_WhenMemeAlertsNotConnected()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsTwitchConnected = true;
		viewModel.IsMemeAlertsConnected = false;

		var message = new SettingsChangedMessage("TestSetting");

		// Act
		viewModel.Receive(message);

		// Assert
		rewardsServiceMock.Verify(r => r.StartAsync(It.IsAny<IDictionary<string, int>>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	#endregion

	#region CanConnectTwitch

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory("CanExecute")]
	public void CanConnectTwitch_ReturnsTrue_WhenNotChecking()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsCheckingTwitch = false;

		// Act
		var canExecute = viewModel.ConnectTwitchCommand.CanExecute(null);

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory("CanExecute")]
	public void CanConnectTwitch_ReturnsFalse_WhenChecking()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsCheckingTwitch = true;

		// Act
		var canExecute = viewModel.ConnectTwitchCommand.CanExecute(null);

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion

	#region CanConnectMemeAlerts

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory("CanExecute")]
	public void CanConnectMemeAlerts_ReturnsTrue_WhenNotChecking()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsCheckingMemeAlerts = false;

		// Act
		var canExecute = viewModel.ConnectMemeAlertsCommand.CanExecute(null);

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(ConnectionViewModel))]
	[TestCategory("CanExecute")]
	public void CanConnectMemeAlerts_ReturnsFalse_WhenChecking()
	{
		// Arrange
		var settingsServiceMock = new Mock<ISettingsService>();
		var rewardsServiceMock = new Mock<IRewardsService>();
		var twitchOAuthServiceMock = new Mock<ITwitchOAuthService>();
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var serviceProviderMock = new Mock<IServiceProvider>();
		var dbContextFactoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		var loggerMock = new Mock<ILogger<ConnectionViewModel>>();

		var viewModel = new ConnectionViewModel(
			settingsServiceMock.Object,
			rewardsServiceMock.Object,
			twitchOAuthServiceMock.Object,
			memeAlertsServiceMock.Object,
			dispatcherServiceMock.Object,
			serviceProviderMock.Object,
			dbContextFactoryMock.Object,
			loggerMock.Object);

		viewModel.IsCheckingMemeAlerts = true;

		// Act
		var canExecute = viewModel.ConnectMemeAlertsCommand.CanExecute(null);

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion
}

[TestClass]
public class ObservableRecipientWithQuantityTests
{
	private readonly IFixture fixture;

	public ObservableRecipientWithQuantityTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region GetErrors

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory(nameof(ObservableRecipientWithQuantity.GetErrors))]
	public void GetErrors_ReturnsEmptyList_WhenNoErrors()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();
		viewModel.Quantity = "100"; // Set valid value to clear default "0" error

		// Act
		var errors = viewModel.GetErrors("Quantity");

		// Assert
		Assert.IsNotNull(errors);
		Assert.AreEqual(0, errors.Cast<string>().Count());
	}

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory(nameof(ObservableRecipientWithQuantity.GetErrors))]
	public void GetErrors_ReturnsErrors_WhenErrorsExist()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();
		viewModel.Quantity = "0"; // This triggers validation error

		// Act
		var errors = viewModel.GetErrors("Quantity");

		// Assert
		Assert.IsNotNull(errors);
		Assert.IsTrue(errors.Cast<string>().Any());
	}

	#endregion

	#region HasErrors

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory(nameof(ObservableRecipientWithQuantity.HasErrors))]
	public void HasErrors_ReturnsFalse_WhenNoErrors()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();
		viewModel.Quantity = "100"; // Valid value

		// Act & Assert
		Assert.IsFalse(viewModel.HasErrors);
	}

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory(nameof(ObservableRecipientWithQuantity.HasErrors))]
	public void HasErrors_ReturnsTrue_WhenHasErrors()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();
		viewModel.Quantity = "0"; // Invalid value

		// Act & Assert
		Assert.IsTrue(viewModel.HasErrors);
	}

	#endregion

	#region Quantity Validation

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory("Validation")]
	public void Quantity_SetNegativeValue_AddsError()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();

		// Act
		viewModel.Quantity = "-5";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(viewModel.Quantity)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory("Validation")]
	public void Quantity_SetValueOver1000_AddsError()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();

		// Act
		viewModel.Quantity = "1001";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(viewModel.Quantity)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory("Validation")]
	public void Quantity_SetInvalidString_AddsError()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();

		// Act
		viewModel.Quantity = "invalid";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(viewModel.Quantity)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(ObservableRecipientWithQuantity))]
	[TestCategory("Validation")]
	public void Quantity_SetValidValue_ClearsErrors()
	{
		// Arrange
		var viewModel = new TestObservableRecipientWithQuantity();
		viewModel.Quantity = "0"; // First set invalid

		// Act
		viewModel.Quantity = "100"; // Then set valid

		// Assert
		Assert.IsFalse(viewModel.HasErrors);
	}

	#endregion
}

// Test class to expose protected members
public class TestObservableRecipientWithQuantity : ObservableRecipientWithQuantity
{
}

// Test subclass for ConnectionViewModel to access protected methods
public class TestConnectionViewModel : ConnectionViewModel
{
	public TestConnectionViewModel(
		ISettingsService settingsService,
		IRewardsService rewardsService,
		ITwitchOAuthService twitchOAuthService,
		IMemeAlertsService twitchMemeAlertsAutoService,
		IDispatcherService dispatcherService,
		IServiceProvider serviceProvider,
		IDbContextFactory<TmaaDbContext> dbContextFactory,
		ILogger<ConnectionViewModel> logger)
		: base(settingsService, rewardsService, twitchOAuthService, twitchMemeAlertsAutoService, dispatcherService, serviceProvider, dbContextFactory, logger)
	{
	}

	public void TestOnActivated() => OnActivated();
	public void TestOnDeactivated() => OnDeactivated();
}

// Test subclass for MainWindowViewModel to access protected methods
public class TestMainWindowViewModel : MainWindowViewModel
{
	public TestMainWindowViewModel(
		ISettingsService settingsService,
		LogViewModel logViewModel,
		RewardsViewModel rewardsViewModel,
		MainMenuViewModel mainMenuViewModel,
		AllRewardViewModel allRewardViewModel,
		SupportersViewModel supportersViewModel,
		ILogger<MainWindowViewModel> logger)
		: base(logViewModel, rewardsViewModel, mainMenuViewModel, allRewardViewModel, supportersViewModel, logger)
	{
	}

	public void TestOnActivated() => OnActivated();
	public void TestOnDeactivated() => OnDeactivated();
}

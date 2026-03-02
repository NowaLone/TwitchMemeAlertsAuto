using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchMemeAlertsAuto.Core.Services;
using TwitchMemeAlertsAuto.Core.ViewModels;

namespace TwitchMemeAlertsAuto.Core.Tests.ViewModels;

[TestClass]
public class RewardViewModelTests
{
	private readonly IFixture fixture;

	public RewardViewModelTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory(nameof(RewardViewModel))]
	public void Constructor_WithDependencies_CreatesViewModel()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		// Act
		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Assert
		Assert.IsNotNull(viewModel);
		Assert.IsFalse(viewModel.HasErrors);
	}

	#endregion

	#region Count Validation

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory("Validation")]
	public void Count_SetValidValue_NoErrors()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Act
		viewModel.Count = "100";

		// Assert
		Assert.IsFalse(viewModel.HasErrors);
		Assert.AreEqual("100", viewModel.Count);
	}

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory("Validation")]
	public void Count_SetZero_AddsError()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Act
		viewModel.Count = "0";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(RewardViewModel.Count)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory("Validation")]
	public void Count_SetNegativeValue_AddsError()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Act
		viewModel.Count = "-5";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(RewardViewModel.Count)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory("Validation")]
	public void Count_SetValueOver1000_AddsError()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Act
		viewModel.Count = "1001";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(RewardViewModel.Count)).Cast<string>().Any());
	}

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory("Validation")]
	public void Count_SetInvalidString_AddsError()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		// Act
		viewModel.Count = "invalid";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
		Assert.IsTrue(viewModel.GetErrors(nameof(RewardViewModel.Count)).Cast<string>().Any());
	}

	#endregion

	#region SaveCommand

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory(nameof(RewardViewModel.SaveCommand))]
	public void SaveCommand_CanExecute_ReturnsTrue_WhenNoErrors()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		viewModel.Count = "100";

		// Act
		var canExecute = viewModel.SaveCommand.CanExecute("100");

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(RewardViewModel))]
	[TestCategory(nameof(RewardViewModel.SaveCommand))]
	public void SaveCommand_CanExecute_ReturnsFalse_WhenHasErrors()
	{
		// Arrange
		var dispatcherServiceMock = new Mock<IDispatcherService>();
		var settingsServiceMock = new Mock<ISettingsService>();

		var viewModel = new RewardViewModel(
			dispatcherServiceMock.Object,
			settingsServiceMock.Object);

		viewModel.Count = "0"; // Invalid value

		// Act
		var canExecute = viewModel.SaveCommand.CanExecute("0");

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion
}

[TestClass]
public class SupporterViewModelTests
{
	private readonly IFixture fixture;

	public SupporterViewModelTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	#region Constructor

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory(nameof(SupporterViewModel))]
	public void Constructor_WithDependencies_CreatesViewModel()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		// Act
		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Assert
		Assert.IsNotNull(viewModel);
		Assert.AreEqual("0", viewModel.Quantity);
		// Note: Quantity "0" triggers validation error by design
		Assert.IsTrue(viewModel.HasErrors);
	}

	#endregion

	#region Quantity Validation

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory("Validation")]
	public void Quantity_SetValidValue_NoErrors()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Act
		viewModel.Quantity = "100";

		// Assert
		Assert.IsFalse(viewModel.HasErrors);
		Assert.AreEqual("100", viewModel.Quantity);
	}

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory("Validation")]
	public void Quantity_SetZero_AddsError()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Act
		viewModel.Quantity = "0";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
	}

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory("Validation")]
	public void Quantity_SetValueOver1000_AddsError()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		// Act
		viewModel.Quantity = "1001";

		// Assert
		Assert.IsTrue(viewModel.HasErrors);
	}

	#endregion

	#region RewardCommand

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory(nameof(SupporterViewModel.RewardCommand))]
	public void RewardCommand_CanExecute_ReturnsTrue_WhenValidSupporterAndNoErrors()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		viewModel.Supporter = new Supporter { SupporterId = "123", SupporterName = "TestUser" };
		viewModel.Quantity = "10";

		// Act
		var canExecute = viewModel.RewardCommand.CanExecute("10");

		// Assert
		Assert.IsTrue(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory(nameof(SupporterViewModel.RewardCommand))]
	public void RewardCommand_CanExecute_ReturnsFalse_WhenHasErrors()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		viewModel.Supporter = new Supporter { SupporterId = "123", SupporterName = "TestUser" };
		viewModel.Quantity = "0"; // Invalid

		// Act
		var canExecute = viewModel.RewardCommand.CanExecute("10");

		// Assert
		Assert.IsFalse(canExecute);
	}

	[TestMethod]
	[TestCategory(nameof(SupporterViewModel))]
	[TestCategory(nameof(SupporterViewModel.RewardCommand))]
	public void RewardCommand_CanExecute_ReturnsFalse_WhenSupporterIsNull()
	{
		// Arrange
		var memeAlertsServiceMock = new Mock<IMemeAlertsService>();
		var loggerMock = new Mock<ILogger<SupporterViewModel>>();

		var viewModel = new SupporterViewModel(
			memeAlertsServiceMock.Object,
			loggerMock.Object);

		viewModel.Quantity = "10";

		// Act
		var canExecute = viewModel.RewardCommand.CanExecute("10");

		// Assert
		Assert.IsFalse(canExecute);
	}

	#endregion
}

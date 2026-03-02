using AutoFixture;
using AutoFixture.AutoMoq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Services;

namespace TwitchMemeAlertsAuto.Core.Tests.Services;

[TestClass]
public class SettingsServiceTests
{
	private readonly IFixture fixture;

	public SettingsServiceTests()
	{
		fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
	}

	private (SettingsService service, DbContextOptions<TmaaDbContext> options, Mock<ILogger<SettingsService>> loggerMock) CreateService()
	{
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
			.Options;

		var factoryMock = new Mock<IDbContextFactory<TmaaDbContext>>();
		factoryMock
			.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => new TmaaDbContext(options));

		var loggerMock = new Mock<ILogger<SettingsService>>();

		var service = new SettingsService(factoryMock.Object, loggerMock.Object);
		return (service, options, loggerMock);
	}

	#region GetSettingAsync

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.GetSettingAsync))]
	public async Task GetSettingAsync_CreatesSetting_WhenMissingAndDefaultNotNull()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var key = fixture.Create<string>();
		var defaultValue = fixture.Create<int>();

		// Act
		var result = await service.GetSettingAsync(key, defaultValue, CancellationToken.None);

		// Assert
		Assert.AreEqual(defaultValue, result);
		await using var context = new TmaaDbContext(options);
		var setting = await context.Settings.SingleAsync(s => s.Key == key);
		Assert.AreEqual(Convert.ToString(defaultValue, System.Globalization.CultureInfo.InvariantCulture), setting.Value);
	}

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.GetSettingAsync))]
	public async Task GetSettingAsync_DoesNotCreateSetting_WhenMissingAndDefaultNull()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var key = fixture.Create<string>();

		// Act
		var result = await service.GetSettingAsync<string?>(key, null, CancellationToken.None);

		// Assert
		Assert.IsNull(result);
		await using var context = new TmaaDbContext(options);
		Assert.IsFalse(await context.Settings.AnyAsync(s => s.Key == key));
	}

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.GetSettingAsync))]
	public async Task GetSettingAsync_ReturnsConvertedValue_WhenSettingExists()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var key = fixture.Create<string>();
		var value = fixture.Create<int>();

		await using (var arrangeContext = new TmaaDbContext(options))
		{
			arrangeContext.Settings.Add(new Setting
			{
				Key = key,
				Value = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
			});
			await arrangeContext.SaveChangesAsync();
		}

		// Act
		var result = await service.GetSettingAsync(key, 0, CancellationToken.None);

		// Assert
		Assert.AreEqual(value, result);
	}

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.GetSettingAsync))]
	public async Task GetSettingAsync_ReturnsDefaultAndLogsWarning_OnConversionFailure()
	{
		// Arrange
		var (service, options, loggerMock) = CreateService();
		var key = fixture.Create<string>();
		const int defaultValue = 42;

		await using (var arrangeContext = new TmaaDbContext(options))
		{
			arrangeContext.Settings.Add(new Setting
			{
				Key = key,
				Value = "not-an-int"
			});
			await arrangeContext.SaveChangesAsync();
		}

		// Act
		var result = await service.GetSettingAsync(key, defaultValue, CancellationToken.None);

		// Assert
		Assert.AreEqual(defaultValue, result);

		loggerMock.Verify(
			l => l.Log(
				LogLevel.Warning,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to convert setting")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception, string>>()),
			Times.Once);
	}

	#endregion

	#region SetSettingAsync

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.SetSettingAsync))]
	public async Task SetSettingAsync_InsertsNewSetting_WhenKeyMissing()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var key = fixture.Create<string>();
		var value = fixture.Create<string>();

		// Act
		await service.SetSettingAsync(key, value, CancellationToken.None);

		// Assert
		await using var context = new TmaaDbContext(options);
		var setting = await context.Settings.SingleAsync(s => s.Key == key);
		Assert.AreEqual(value, setting.Value);
	}

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.SetSettingAsync))]
	public async Task SetSettingAsync_UpdatesExistingSetting_WhenKeyExists()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var key = fixture.Create<string>();

		await using (var arrangeContext = new TmaaDbContext(options))
		{
			arrangeContext.Settings.Add(new Setting
			{
				Key = key,
				Value = "old"
			});
			await arrangeContext.SaveChangesAsync();
		}

		var newValue = fixture.Create<string>();

		// Act
		await service.SetSettingAsync(key, newValue, CancellationToken.None);

		// Assert
		await using var context = new TmaaDbContext(options);
		var setting = await context.Settings.SingleAsync(s => s.Key == key);
		Assert.AreEqual(newValue, setting.Value);
	}

	#endregion

	#region ConvenienceMethods

	[TestMethod]
	[TestCategory(nameof(SettingsService))]
	[TestCategory(nameof(SettingsService.SetTwitchOAuthTokenAsync))]
	[TestCategory(nameof(SettingsService.GetTwitchOAuthTokenAsync))]
	public async Task ConvenienceMethods_UseExpectedKeys_ForTwitchOAuthToken()
	{
		// Arrange
		var (service, options, _) = CreateService();
		var token = fixture.Create<string>();

		// Act
		await service.SetTwitchOAuthTokenAsync(token, CancellationToken.None);
		var loaded = await service.GetTwitchOAuthTokenAsync(CancellationToken.None);

		// Assert
		Assert.AreEqual(token, loaded);
		await using var context = new TmaaDbContext(options);
		var setting = await context.Settings.SingleAsync(s => s.Key == "Twitch:OAuthToken");
		Assert.AreEqual(token, setting.Value);
	}

	#endregion
}


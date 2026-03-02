using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.Core.Migrations.Sqlite.Tests;

[TestClass]
public class TmaaDbContextTests
{
	#region Constructor

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory(nameof(TmaaDbContext))]
	public void Constructor_WithOptions_CreatesContext()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		// Act
		var context = new TmaaDbContext(options);

		// Assert
		Assert.IsNotNull(context);
	}

	#endregion

	#region DbSets

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("DbSets")]
	public async Task DbSet_Settings_IsConfigured()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var settings = context.Settings;

		// Assert
		Assert.IsNotNull(settings);
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("DbSets")]
	public async Task DbSet_Histories_IsConfigured()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var histories = context.Histories;

		// Assert
		Assert.IsNotNull(histories);
	}

	#endregion

	#region ModelConfiguration

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Model")]
	public async Task Model_HistoriesEntity_HasExpectedProperties()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var entityType = context.Model.FindEntityType(typeof(History));

		// Assert
		Assert.IsNotNull(entityType);
		Assert.IsNotNull(entityType!.FindProperty("Id"));
		Assert.IsNotNull(entityType.FindProperty("Message"));
		Assert.IsNotNull(entityType.FindProperty("Timestamp"));
		Assert.IsNotNull(entityType.FindProperty("EventId"));
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Model")]
	public async Task Model_SettingsEntity_HasExpectedProperties()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var entityType = context.Model.FindEntityType(typeof(Setting));

		// Assert
		Assert.IsNotNull(entityType);
		Assert.IsNotNull(entityType!.FindProperty("Id"));
		Assert.IsNotNull(entityType.FindProperty("Key"));
		Assert.IsNotNull(entityType.FindProperty("Value"));
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Model")]
	public async Task Model_HistoriesEntity_HasPrimaryKey()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var entityType = context.Model.FindEntityType(typeof(History));
		var primaryKey = entityType!.FindPrimaryKey();

		// Assert
		Assert.IsNotNull(primaryKey);
		Assert.AreEqual(1, primaryKey.Properties.Count);
		Assert.AreEqual("Id", primaryKey.Properties[0].Name);
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Model")]
	public async Task Model_SettingsEntity_HasPrimaryKey()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act
		var entityType = context.Model.FindEntityType(typeof(Setting));
		var primaryKey = entityType!.FindPrimaryKey();

		// Assert
		Assert.IsNotNull(primaryKey);
		Assert.AreEqual(1, primaryKey.Properties.Count);
		Assert.AreEqual("Id", primaryKey.Properties[0].Name);
	}

	#endregion

	#region DatabaseOperations

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("DatabaseOperations")]
	public async Task Database_CanExecuteCrudOperations_OnSettings()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act - Create
		var setting = new Setting { Key = "TestKey", Value = "TestValue" };
		context.Settings.Add(setting);
		await context.SaveChangesAsync();

		// Act - Read
		var loaded = await context.Settings.SingleAsync(s => s.Key == "TestKey");

		// Act - Update
		loaded.Value = "UpdatedValue";
		await context.SaveChangesAsync();

		// Act - Delete
		context.Settings.Remove(loaded);
		await context.SaveChangesAsync();

		// Assert
		Assert.AreEqual(setting.Id, loaded.Id);
		Assert.AreEqual("UpdatedValue", loaded.Value);
		Assert.IsFalse(await context.Settings.AnyAsync(s => s.Key == "TestKey"));
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("DatabaseOperations")]
	public async Task Database_CanExecuteCrudOperations_OnHistories()
	{
		// Arrange
		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		await using var context = new TmaaDbContext(options);

		// Act - Create
		var history = new History { Message = "TestMessage", Timestamp = DateTimeOffset.FromUnixTimeSeconds(1234567890), EventId = 1 };
		context.Histories.Add(history);
		await context.SaveChangesAsync();

		// Act - Read
		var loaded = await context.Histories.SingleAsync(h => h.Message == "TestMessage");

		// Act - Update
		loaded.Message = "UpdatedMessage";
		await context.SaveChangesAsync();

		// Act - Delete
		context.Histories.Remove(loaded);
		await context.SaveChangesAsync();

		// Assert
		Assert.AreEqual(history.Id, loaded.Id);
		Assert.AreEqual("UpdatedMessage", loaded.Message);
		Assert.IsFalse(await context.Histories.AnyAsync(h => h.Message == "UpdatedMessage"));
	}

	#endregion

	#region SqliteIntegration

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Sqlite")]
	public async Task Sqlite_CanConnect_AndCreateTables()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection)
			.Options;

		try
		{
			// Act
			await using var context = new TmaaDbContext(options);
			await context.Database.EnsureCreatedAsync();

			// Assert
			Assert.IsTrue(await context.Database.CanConnectAsync());
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	[TestMethod]
	[TestCategory(nameof(TmaaDbContext))]
	[TestCategory("Sqlite")]
	public async Task Sqlite_CanPersistAndReadData()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection)
			.Options;

		try
		{
			await using var context = new TmaaDbContext(options);
			await context.Database.EnsureCreatedAsync();

			// Act - Insert
			context.Settings.Add(new Setting { Key = "Key1", Value = "Value1" });
			context.Histories.Add(new History { Message = "Msg1", Timestamp = DateTimeOffset.FromUnixTimeSeconds(111), EventId = 1 });
			await context.SaveChangesAsync();

			// Assert - Read
			var setting = await context.Settings.SingleAsync(s => s.Key == "Key1");
			Assert.AreEqual("Value1", setting.Value);

			var history = await context.Histories.SingleAsync(h => h.Message == "Msg1");
			Assert.AreEqual(111, history.Timestamp.ToUnixTimeSeconds());
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	#endregion
}

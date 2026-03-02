using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchMemeAlertsAuto.Core;
using TwitchMemeAlertsAuto.Core.Migrations.Sqlite.Migrations;

namespace TwitchMemeAlertsAuto.Core.Migrations.Sqlite.Tests;

[TestClass]
public class InitialCreateMigrationTests
{
	#region MigrationMetadata

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Metadata")]
	public void Migration_HasExpectedName()
	{
		// Arrange
		var migration = new InitialCreate();

		// Act
		var attribute = migration.GetType().GetCustomAttributes(typeof(MigrationAttribute), false).FirstOrDefault() as MigrationAttribute;

		// Assert
		Assert.IsNotNull(attribute);
		// MigrationAttribute stores the migration ID internally, verify via the type name
		Assert.AreEqual("InitialCreate", migration.GetType().Name);
	}

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Metadata")]
	public void Migration_TargetsTmaaDbContext()
	{
		// Arrange
		var migration = new InitialCreate();

		// Act
		var attribute = migration.GetType().GetCustomAttributes(typeof(DbContextAttribute), false).FirstOrDefault() as DbContextAttribute;

		// Assert
		Assert.IsNotNull(attribute);
		Assert.AreEqual(typeof(TmaaDbContext), attribute!.ContextType);
	}

	#endregion

	#region MigrationIntegration

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Integration")]
	public async Task Migration_CanApplyToDatabase_AndCreateTables()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection, b => b.MigrationsAssembly(typeof(InitialCreate).Assembly.FullName))
			.Options;

		try
		{
			// Act
			await using var context = new TmaaDbContext(options);
			await context.Database.MigrateAsync();

			// Assert
			Assert.IsTrue(await context.Database.CanConnectAsync());

			// Verify tables exist
			var tables = await GetSqliteTablesAsync(connection);
			Assert.IsTrue(tables.Contains("Histories"));
			Assert.IsTrue(tables.Contains("Settings"));
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Integration")]
	public async Task Migration_CanInsertAndReadData_FromBothTables()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection, b => b.MigrationsAssembly(typeof(InitialCreate).Assembly.FullName))
			.Options;

		try
		{
			await using var context = new TmaaDbContext(options);
			await context.Database.MigrateAsync();

			// Act - Insert data
			context.Settings.Add(new Setting { Key = "TestKey", Value = "TestValue" });
			context.Histories.Add(new History { Message = "TestMessage", Timestamp = DateTimeOffset.FromUnixTimeSeconds(1234567890), EventId = 1 });
			await context.SaveChangesAsync();

			// Assert - Read data
			var setting = await context.Settings.SingleAsync(s => s.Key == "TestKey");
			Assert.AreEqual("TestValue", setting.Value);

			var history = await context.Histories.SingleAsync(h => h.Message == "TestMessage");
			Assert.AreEqual(1234567890, history.Timestamp.ToUnixTimeSeconds());
			Assert.AreEqual(1, history.EventId);
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Integration")]
	public async Task Migration_HistoriesTable_HasExpectedColumns()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection, b => b.MigrationsAssembly(typeof(InitialCreate).Assembly.FullName))
			.Options;

		try
		{
			await using var context = new TmaaDbContext(options);
			await context.Database.MigrateAsync();

			// Act
			var columnInfo = await GetTableColumnsAsync(connection, "Histories");

			// Assert
			Assert.IsTrue(columnInfo.Any(c => c.name == "Id" && c.type == "INTEGER"));
			Assert.IsTrue(columnInfo.Any(c => c.name == "Message" && c.type == "TEXT"));
			Assert.IsTrue(columnInfo.Any(c => c.name == "Timestamp" && c.type == "INTEGER"));
			Assert.IsTrue(columnInfo.Any(c => c.name == "EventId" && c.type == "INTEGER"));
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Integration")]
	public async Task Migration_SettingsTable_HasExpectedColumns()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection, b => b.MigrationsAssembly(typeof(InitialCreate).Assembly.FullName))
			.Options;

		try
		{
			await using var context = new TmaaDbContext(options);
			await context.Database.MigrateAsync();

			// Act
			var columnInfo = await GetTableColumnsAsync(connection, "Settings");

			// Assert
			Assert.IsTrue(columnInfo.Any(c => c.name == "Id" && c.type == "INTEGER"));
			Assert.IsTrue(columnInfo.Any(c => c.name == "Key" && c.type == "TEXT"));
			Assert.IsTrue(columnInfo.Any(c => c.name == "Value" && c.type == "TEXT"));
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	#endregion

	#region Rollback

	[TestMethod]
	[TestCategory(nameof(InitialCreate))]
	[TestCategory("Rollback")]
	public async Task Migration_CanRollback_RemovesTables()
	{
		// Arrange
		var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<TmaaDbContext>()
			.UseSqlite(connection, b => b.MigrationsAssembly(typeof(InitialCreate).Assembly.FullName))
			.Options;

		try
		{
			await using (var context = new TmaaDbContext(options))
			{
				await context.Database.MigrateAsync();
			}

			// Act - Rollback
			await using (var rollbackContext = new TmaaDbContext(options))
			{
				var migrator = rollbackContext.Database.GetService<IMigrator>();
				await migrator.MigrateAsync(Migration.InitialDatabase);
			}

			// Assert
			var tables = await GetSqliteTablesAsync(connection);
			Assert.IsFalse(tables.Contains("Histories"));
			Assert.IsFalse(tables.Contains("Settings"));
		}
		finally
		{
			await connection.CloseAsync();
		}
	}

	#endregion

	private static async Task<List<string>> GetSqliteTablesAsync(Microsoft.Data.Sqlite.SqliteConnection connection)
	{
		var tables = new List<string>();
		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
		await using var reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			tables.Add(reader.GetString(0));
		}
		return tables;
	}

	private static async Task<List<(string name, string type)>> GetTableColumnsAsync(Microsoft.Data.Sqlite.SqliteConnection connection, string tableName)
	{
		var columns = new List<(string name, string type)>();
		await using var command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info({tableName});";
		await using var reader = await command.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			columns.Add((reader.GetString(1), reader.GetString(2)));
		}
		return columns;
	}
}

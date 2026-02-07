using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using TwitchMemeAlertsAuto.Core.ViewModels.Messages;

namespace TwitchMemeAlertsAuto.Core.Logging
{
	public class WpfLoggerProvider : ILoggerProvider
	{
		private readonly Dictionary<string, WpfLogger> loggers = new();
		private readonly LogLevel minimumLogLevel;
		private bool disposed;

		public WpfLoggerProvider(LogLevel minimumLogLevel = LogLevel.Information)
		{
			this.minimumLogLevel = minimumLogLevel;
		}

		public ILogger CreateLogger(string categoryName)
		{
			lock (loggers)
			{
				if (!loggers.TryGetValue(categoryName, out var logger))
				{
					logger = new WpfLogger(categoryName, minimumLogLevel);
					loggers[categoryName] = logger;
				}

				return logger;
			}
		}

		public void Dispose()
		{
			if (!disposed)
			{
				lock (loggers)
				{
					loggers.Clear();
				}

				disposed = true;
			}
		}
	}

	public class WpfLogger : ILogger
	{
		private readonly string categoryName;
		private readonly LogLevel minimumLogLevel;

		public WpfLogger(string categoryName, LogLevel minimumLogLevel)
		{
			this.categoryName = categoryName;
			this.minimumLogLevel = minimumLogLevel;
		}

		public IDisposable BeginScope<TState>(TState state) where TState : notnull => new NoOpScope();

		public bool IsEnabled(LogLevel logLevel) => logLevel >= minimumLogLevel;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
				return;
			
			if (!EventIds.Events.Contains(eventId))
			{
				return;
			}

			var message = formatter(state, exception);
			if (string.IsNullOrWhiteSpace(message) && exception == null)
				return;

			if (exception != null)
				message = $"{message}{Environment.NewLine}{exception}";

			var history = new History
			{
				Message = message,
				Timestamp = DateTimeOffset.Now,
				EventId = eventId.Id,
			};

			// Send the log message through MVVM Toolkit messaging
			WeakReferenceMessenger.Default.Send(new LogMessage(history));
		}

		private sealed class NoOpScope : IDisposable
		{
			public void Dispose()
			{ }
		}
	}
}
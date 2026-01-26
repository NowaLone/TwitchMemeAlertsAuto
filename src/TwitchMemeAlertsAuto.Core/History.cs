using System;

namespace TwitchMemeAlertsAuto.Core
{
	public class History
	{
		public int Id { get; set; }
		public string Username { get; set; }
		public int Value { get; set; }
		public DateTimeOffset Timestamp { get; set; }

		// Convenience read-only properties for UI grouping and display
		public DateTime Date => Timestamp.Date;

		// Backwards-compatible: Username currently stores the message text in some places.
		public string Message => $"{Timestamp:HH:mm} {Username}";
	}
}
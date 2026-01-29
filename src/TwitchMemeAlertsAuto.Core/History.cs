using System;

namespace TwitchMemeAlertsAuto.Core
{
	public class History
	{
		public int Id { get; set; }
		public string Message { get; set; }
		public DateTimeOffset Timestamp { get; set; }
		public int EventId { get; set; }
	}
}
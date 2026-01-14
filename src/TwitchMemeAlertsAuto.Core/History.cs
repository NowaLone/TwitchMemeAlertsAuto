using System;

namespace TwitchMemeAlertsAuto.Core
{
	public class History
	{
		public int Id { get; set; }
		public string Username { get; set; }
		public int Value { get; set; }
		public DateTimeOffset Timestamp { get; set; }
	}
}
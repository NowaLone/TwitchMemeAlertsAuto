using System.Collections.Generic;

namespace TwitchMemeAlertsAuto.Core
{
	public class Events
	{
		public IEnumerable<Event> Data { get; set; }
		public int Total { get; set; }
	}
}
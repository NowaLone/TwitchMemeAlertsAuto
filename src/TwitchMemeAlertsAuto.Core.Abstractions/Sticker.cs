using System.Collections.Generic;

namespace TwitchMemeAlertsAuto.Core
{
	public class Sticker
	{
		public string Id { get; set; }

		public string Name { get; set; }

		public string Description { get; set; }
		public string Music { get; set; }

		public List<Tag> Tags { get; set; }
		public string StreamerName { get; set; }
	}
}
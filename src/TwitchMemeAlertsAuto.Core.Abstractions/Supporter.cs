using System;

namespace TwitchMemeAlertsAuto.Core
{
	public class Supporter
	{
		public int Balance { get; set; }
		public string SupporterId { get; set; }
		public string SupporterName { get; set; }
		public Uri SupporterAvatar { get; set; }
		public long? LastSupport { get; set; }
	}
}
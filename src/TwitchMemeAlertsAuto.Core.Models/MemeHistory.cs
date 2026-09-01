using System;

namespace TwitchMemeAlertsAuto.Core
{
	public class MemeHistory
	{
		public int Id { get; set; }
		public StickerInfo Sticker { get; set; }
		public MemeHistoryType Type { get; set; }
		public string BroadcasterUserId { get; set; }
		public string UserId { get; set; }
		public string UserInput { get; set; }
		public int Cost { get; set; }
		public string RewardId { get; set; }
		public string UserName { get; set; }
		public DateTimeOffset Timestamp { get; set; }
	}
}
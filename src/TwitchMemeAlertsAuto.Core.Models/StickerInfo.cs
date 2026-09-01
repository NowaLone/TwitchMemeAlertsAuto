using Microsoft.EntityFrameworkCore;

namespace TwitchMemeAlertsAuto.Core
{
	[Index(nameof(StickerId), IsUnique = true)]
	public class StickerInfo
	{
		public int Id { get; set; }
		public string StickerId { get; set; }
		public string Name { get; set; }
	}
}
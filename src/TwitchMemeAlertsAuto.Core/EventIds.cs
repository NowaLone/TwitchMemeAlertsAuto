using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace TwitchMemeAlertsAuto.Core
{
	public static class EventIds
	{
		public static EventId Rewarded => new EventId(19980901, nameof(Rewarded));
		public static EventId NotRewarded => new EventId(19980902, nameof(NotRewarded));
		public static EventId NotFound => new EventId(19980903, nameof(NotFound));
		public static EventId Loading => new EventId(19980904, nameof(Loading));
		public static EventId Loaded => new EventId(19980905, nameof(Loaded));

		public static List<EventId> Events => new List<EventId>
		{
			Rewarded,
			NotRewarded,
			NotFound,
			Loading,
			Loaded,
		};
	}
}
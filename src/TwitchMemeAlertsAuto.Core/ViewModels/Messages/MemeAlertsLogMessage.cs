using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class MemeAlertsLogMessage : ValueChangedMessage<History>
	{
		public MemeAlertsLogMessage(History value) : base(value)
		{
		}
	}
}

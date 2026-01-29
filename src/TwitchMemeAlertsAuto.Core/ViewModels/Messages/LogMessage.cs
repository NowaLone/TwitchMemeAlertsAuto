using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class LogMessage : ValueChangedMessage<History>
	{
		public LogMessage(History value) : base(value)
		{
		}
	}
}

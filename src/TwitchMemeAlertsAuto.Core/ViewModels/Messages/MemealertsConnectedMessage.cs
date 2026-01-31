using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class MemealertsConnectedMessage : ValueChangedMessage<string>
	{
		public MemealertsConnectedMessage(string value) : base(value)
		{
		}
	}
}
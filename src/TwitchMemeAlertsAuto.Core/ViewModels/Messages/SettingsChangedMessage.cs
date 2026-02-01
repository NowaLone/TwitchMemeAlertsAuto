using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class SettingsChangedMessage : ValueChangedMessage<string>
	{
		public SettingsChangedMessage(string value) : base(value)
		{
		}
	}
}
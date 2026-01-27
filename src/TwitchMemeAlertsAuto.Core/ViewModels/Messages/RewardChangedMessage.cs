using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class RewardChangedMessage : ValueChangedMessage<string>
	{
		public RewardChangedMessage(string value) : base(value)
		{
		}
	}
}
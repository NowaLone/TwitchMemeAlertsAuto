using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class TwitchConnectedMessage : ValueChangedMessage<(string Token, string UserId)>
	{
		public TwitchConnectedMessage(string token, string userId) : base((token, userId))
		{
		}
	}
}
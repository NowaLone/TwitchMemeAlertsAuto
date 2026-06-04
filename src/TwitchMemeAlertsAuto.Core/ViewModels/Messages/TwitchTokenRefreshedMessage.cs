using CommunityToolkit.Mvvm.Messaging.Messages;

namespace TwitchMemeAlertsAuto.Core.ViewModels.Messages
{
	public class TwitchTokenRefreshedMessage : ValueChangedMessage<(string Token, string UserId)>
	{
		public TwitchTokenRefreshedMessage(string token, string userId) : base((token, userId))
		{
		}
	}
}

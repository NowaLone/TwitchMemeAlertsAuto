using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using TwitchLib.Api.Helix.Models.ChannelPoints;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.WPF
{
	/// <summary>
	/// Interaction logic for RewardControl.xaml
	/// </summary>
	public partial class RewardControl : UserControl
	{
		private readonly ISettingsService settingsService;

		public RewardControl()
		{
			this.settingsService = App.Host.Services.GetRequiredService<ISettingsService>();

			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(CountTextBox.Text.Trim(), out var count))
			{
				await settingsService.SetSettingAsync($"Reward:{(DataContext as CustomReward).Id}", count);
			}
		}

		private async void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			CountTextBox.Text = await settingsService.GetSettingAsync($"Reward:{(DataContext as CustomReward).Id}", "0");
		}
	}
}
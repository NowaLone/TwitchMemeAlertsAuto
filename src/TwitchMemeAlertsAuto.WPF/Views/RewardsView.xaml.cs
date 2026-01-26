using System.Windows.Controls;
using System.Windows.Data;
using TwitchMemeAlertsAuto.Core.ViewModels;

namespace TwitchMemeAlertsAuto.WPF.Views
{
	/// <summary>
	/// Interaction logic for RewardsView.xaml
	/// </summary>
	public partial class RewardsView : UserControl
	{
		public RewardsView()
		{
			InitializeComponent();
		}

		private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
		{
			if (e.Item is RewardViewModel rewardViewModel && (rewardViewModel.Count.Contains(FilterTextBox.Text, System.StringComparison.OrdinalIgnoreCase) || rewardViewModel.Reward.Title.Contains(FilterTextBox.Text, System.StringComparison.OrdinalIgnoreCase)))
			{
				e.Accepted = true;
			}
			else
			{
				e.Accepted = false;
			}
		}

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			CollectionViewSource.GetDefaultView(RewardsItemsControl.ItemsSource).Refresh();
			e.Handled = true;
		}
	}
}
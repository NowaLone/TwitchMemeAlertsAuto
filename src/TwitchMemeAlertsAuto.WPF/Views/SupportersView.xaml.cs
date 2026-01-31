using System;
using System.Windows.Controls;
using System.Windows.Data;
using TwitchMemeAlertsAuto.Core.ViewModels;

namespace TwitchMemeAlertsAuto.WPF.Views
{
	/// <summary>
	/// Interaction logic for SupportersView.xaml
	/// </summary>
	public partial class SupportersView : UserControl
	{
		public SupportersView()
		{
			InitializeComponent();
		}

		private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
		{
			e.Accepted = e.Item is SupporterViewModel supporterViewModel && (supporterViewModel.Quantity.Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase)
														   || supporterViewModel.Supporter.Balance.ToString().Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase)
														   || (supporterViewModel.Supporter.LastSupport.HasValue && DateTimeOffset.FromUnixTimeMilliseconds(supporterViewModel.Supporter.LastSupport.Value).DateTime.ToString().Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase))
														   || supporterViewModel.Supporter.SupporterId.Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase)
														   || supporterViewModel.Supporter.SupporterName.Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase));
		}

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			CollectionViewSource.GetDefaultView(SupportersItemsControl.ItemsSource).Refresh();
			e.Handled = true;
		}
	}
}
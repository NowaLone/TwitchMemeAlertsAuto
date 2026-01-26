using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.WPF.Views
{
	/// <summary>
	/// Interaction logic for LogView.xaml
	/// </summary>
	public partial class LogView : UserControl
	{
		public LogView()
		{
			InitializeComponent();
		}

		private void CollectionViewSource_Filter(object sender, System.Windows.Data.FilterEventArgs e)
		{
			e.Accepted = e.Item is History history && (history.Message.Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase) || history.Timestamp.DateTime.ToLongTimeString().Contains(FilterTextBox.Text, StringComparison.OrdinalIgnoreCase));
		}

		private void FilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			CollectionViewSource.GetDefaultView(LogsItemsControl.ItemsSource).Refresh();
			e.Handled = true;
		}

		private void LogView_Loaded(object? sender, RoutedEventArgs e)
		{
			if (!Resources.Contains("LogGrouped"))
				return;

			if (Resources["LogGrouped"] is CollectionViewSource cvs && cvs.View is INotifyCollectionChanged incc)
			{
				incc.CollectionChanged += (_, __) => ScrollToBottom();
			}
		}

		private void ScrollToBottom()
		{
			if (LogScrollViewer == null)
				return;

			// Capture current offsets synchronously
			double verticalOffset = LogScrollViewer.VerticalOffset;
			double scrollableHeight = LogScrollViewer.ScrollableHeight;

			// If user is near the bottom, auto-scroll. Otherwise, do nothing to avoid interrupting the user.
			const double threshold = 20.0; // pixels from bottom considered "at bottom"
			if (scrollableHeight <= 0 || verticalOffset >= scrollableHeight - threshold)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
				{
					LogScrollViewer.ScrollToEnd();
				}));
			}
		}
	}
}
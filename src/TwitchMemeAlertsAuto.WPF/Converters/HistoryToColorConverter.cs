using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TwitchMemeAlertsAuto.Core;

namespace TwitchMemeAlertsAuto.WPF.Converters
{
	public class HistoryToColorConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is History history)
			{
				if (history.EventId == EventIds.Error.Id)
				{
					return new SolidColorBrush(Colors.Red);
				}
				else
				{
					return new SolidColorBrush(Colors.Black);
				}
			}
			else
			{
				return value;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
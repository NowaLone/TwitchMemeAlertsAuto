using System;
using System.Globalization;
using System.Windows.Data;

namespace TwitchMemeAlertsAuto.WPF.Converters
{
	public class MemeAlertsSupporterAvatarToUriConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Uri uri && !uri.IsAbsoluteUri)
			{
				var builder = new UriBuilder("https", "f15085b0-2b15-43d6-8979-78f5e2440f23.selstorage.ru", 443, uri.OriginalString);
				return builder.Uri;
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
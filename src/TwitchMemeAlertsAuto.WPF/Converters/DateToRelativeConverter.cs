using System;
using System.Globalization;
using System.Windows.Data;

namespace TwitchMemeAlertsAuto.WPF.Converters
{
	public class DateToRelativeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return Properties.Resources.Unknown;

			DateTime date;
			if (value is DateTime dt)
			{
				date = dt.Date;
			}
			else if (value is DateTimeOffset dto)
			{
				date = dto.Date;
			}
			else if (value is long unixEpoch)
			{
				date = DateTimeOffset.FromUnixTimeMilliseconds(unixEpoch).Date;
			}
			else
			{
				// Try parse
				if (!DateTime.TryParse(value.ToString(), out date))
					return value.ToString();
				date = date.Date;
			}

			var today = DateTime.Today;

			if (date == today)
			{
				return Properties.Resources.Today;
			}
			if (date == today.AddDays(-1))
			{
				return Properties.Resources.Yesterday;
			}
			if (date == today.AddDays(-2))
			{
				return Properties.Resources.DayBeforeYesterday;
			}
			else
			{
				return parameter is string format ? date.ToString(format) : date.ToLongDateString();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
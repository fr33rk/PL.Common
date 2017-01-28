using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace PL.Common.Wpf.Converters
{
	public class InverseBoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
			{
				return new BooleanToVisibilityConverter().Convert(!(bool)value, targetType, parameter, culture);
			}

			throw new InvalidOperationException($"Converter is meant for boolean type.");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new InvalidOperationException();
		}
	}
}
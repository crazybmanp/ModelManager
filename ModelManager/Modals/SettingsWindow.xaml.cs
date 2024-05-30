
using System.Windows;
using System.Windows.Data;

namespace ModelManager
{

	public partial class SettingsWindow : Window
	{
		public Configuration Config { get; init; }

		public SettingsWindow(Configuration config)
		{
			Config = config;

			InitializeComponent();
		}

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			if (Config.HasChanges)
			{
				if (Config.IsChangesValid)
				{
					Config.Save();
					Close();
				}
			}
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			Config.ClearChanges();
			Close();
		}
	}

	[ValueConversion(typeof(string[]), typeof(string))]
	public class ListToStringConverter : IValueConverter
	{

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(string))
				throw new InvalidOperationException("The target must be a String");

			return String.Join(Environment.NewLine, (string[])value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(string[]))
				throw new InvalidOperationException("The target must be a List of String");

			return ((string)value).Split(Environment.NewLine).ToArray();
		}
	}
}

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for ModelVisual.xaml
	/// </summary>
	public partial class ModelVisual : UserControl
	{
		public static readonly DependencyProperty MainWindowProperty = DependencyProperty.Register(
			nameof(MainWindow), typeof(MainWindow), typeof(ModelVisual));

		public required MainWindow MainWindow
		{
			get => (MainWindow)GetValue(MainWindowProperty);
			set => SetValue(MainWindowProperty, value);
		}

		public ModelVisual()
		{

			InitializeComponent();
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start(new ProcessStartInfo
			{
				Verb = "open",
				UseShellExecute = true,
				FileName = e.Uri.AbsoluteUri
			});
			e.Handled = true;
		}

		private void GetInfoButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			ModelViewer mv = new ModelViewer((Model)DataContext, MainWindow);
			mv.Show();
		}

		private void MoveButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			MainWindow.Move((Model)DataContext);
		}

		private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			MainWindow.Delete((Model)DataContext);
		}
	}
}

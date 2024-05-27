using System.Diagnostics;
using System.Windows.Controls;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for ModelVisual.xaml
	/// </summary>
	public partial class ModelVisual : UserControl
	{
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
	}
}

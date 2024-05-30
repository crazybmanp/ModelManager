using System.Windows.Controls;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for ModelVisual.xaml
	/// </summary>
	public partial class ModelVisual : UserControl
	{
		//private Model Model { get; init; }
		public ModelVisual()
		{
			//Model= model;

			InitializeComponent();
		}

		public ModelVisual(Model model)
		{
			//Model= model;
			
			InitializeComponent();
		}
	}
}

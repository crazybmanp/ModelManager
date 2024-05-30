using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private List<Model> models;
		private List<Model> displayModels;

		private string? filterName = null;
		private bool? filterLink = null;
		private bool? filterPreview = null;
		private bool? filterJsonCompleted = null;
		private bool? filterCompleted = null;

		public bool? FilterCompleted
		{
			get => filterCompleted;
			set
			{
				filterCompleted = value;
				FilterList();
			}
		}

		public bool? FilterJsonCompleted
		{
			get => filterJsonCompleted;
			set => filterJsonCompleted = value;
		}

		public bool? FilterPreview
		{
			get => filterPreview;
			set
			{
				filterPreview = value;
				FilterList();
			}
		}

		public bool? FilterLink
		{
			get => filterLink;
			set
			{
				filterLink = value;
				FilterList();
			}
		}

		public string? FilterName
		{
			get => filterName;
			set
			{
				filterName = value;
				FilterList();
			}
		}

		public List<Model> DisplayModels
		{
			get => displayModels;
			private set
			{
				displayModels = value;
				ModelList.ItemsSource = displayModels;
			}
		}

		public MainWindow()
		{
			models = LoadAllLoras();
			displayModels = models;

			InitializeComponent();
			ModelList.ItemsSource = models;

			// foreach (Model model in models)
			// {
			// 	if (!model.IsComplete)
			// 	{
			// 		continue;
			//
			// 	}
			// 	ModelVisual mv = new ModelVisual(model);
			// 	modelVisuals.Add(mv);
			// 	FileViewerStack.Children.Add(new ModelVisual(model));
			// }
		}

		private List<Model> LoadAllLoras()
		{
			DirectoryInfo loraDir = new DirectoryInfo(Path.Join(Model.SDPath, Model.LoraPath, @"Characters\"));
			FileInfo[] files = loraDir.GetFiles("*", SearchOption.AllDirectories);
			List<FileInfo> modelFiles = files.Where(e => Model.ValidModelFiletypes.Contains(Path.GetExtension(e.Name))).ToList();

			List<Model> model = new List<Model>();
			foreach (FileInfo modelFile in modelFiles)
			{
				model.Add(new Model(modelFile, ModelType.Lora));
			}

			return model;
		}

		private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
		{
			WriteIndented = true,
			UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
			Converters =
			{
				new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)

			}
		};

		private string[] getCategories()
		{
			return models.Select(e => e.Category).ToArray();
		}

		private void FilterList()
		{
			List<Model> filtered = models.ToList();

			if (FilterCompleted.HasValue)
			{
				filtered = filtered.Where(e => e.IsComplete == FilterCompleted).ToList();
			}

			if (FilterJsonCompleted.HasValue)
			{
				filtered = filtered.Where(e => e.IsJsonComplete == FilterJsonCompleted).ToList();
			}

			if (FilterPreview.HasValue)
			{
				filtered = filtered.Where(e => e.isPreviewComplete == FilterPreview).ToList();
			}

			if (FilterLink.HasValue)
			{
				filtered = filtered.Where(e => e.isLinkComplete == FilterLink).ToList();
			}

			if (FilterName is not null)
			{
				filtered = filtered.Where(e => e.Name.Contains(FilterName)).ToList();
			}

			DisplayModels = filtered;
		}

		private void JsonBtn_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("This is not implemented yet");
		}
	}
}
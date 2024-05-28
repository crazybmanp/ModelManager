using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Data;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SDFileProcessor;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		private List<Model> models;
		private List<Model> displayModels;

		private string? filterName = null;
		private bool? filterLink = null;
		private bool? filterPreview = null;
		private bool? filterJsonCompleted = null;
		private bool? filterCompleted = null;
		private List<string>? categories = null;
		private string selectedCategory = "No Category";

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

		public List<string>? Categories
		{
			get => categories;
			set
			{
				categories = value;
				FilterList();
			}
		}

		public string SelectedCategory
		{
			get => selectedCategory;
			set
			{
				selectedCategory = value;
				FilterList();
			}
		}

		public List<Model> DisplayModels
		{
			get => displayModels;
			private set
			{
				displayModels = value;
				OnPropertyChanged();
			}
		}

		public MainWindow()
		{
			models = LoadAllLoras();
			displayModels = models;
			categories = GetCategories();

			InitializeComponent();
		}

		private List<Model> LoadAllLoras()
		{
			DirectoryInfo loraDir = new DirectoryInfo(Path.Join(Model.SDPath, Model.LoraPath));
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

		private List<string> GetCategories()
		{
			List<string> categories =
			[
				"No Category"
			];
			categories.AddRange(models.Select(e => e.Category).Distinct());
			return categories;
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

			if (selectedCategory != "No Category")
			{
				filtered = filtered.Where(e => e.Category == selectedCategory).ToList();
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

		private void FindOrphansButton_Click(object sender, RoutedEventArgs e)
		{
			OrphanFiles win = new OrphanFiles(models);
			win.Show();
		}

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			FileProcessorWindow win = new FileProcessorWindow();
			win.Show();
		}
	}
}
using SDFileProcessor;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public static Configuration Config;

		public static string SDPath => Config.SDModelFolder;
		public static string LoraPath = @"Lora\";

		private List<Model> models;
		private List<Model> displayModels;

		private string? filterName = null;
		private bool? filterLink = null;
		private bool? filterPreview = null;
		private bool? filterJsonCompleted = null;
		private bool? filterCompleted = null;
		private List<string> categories;
		private string selectedCategory = "No Category";

		public bool? FilterCompleted
		{
			get => filterCompleted;
			set
			{
				filterCompleted = value;
				FilterList();
				OnPropertyChanged();
			}
		}

		public bool? FilterJsonCompleted
		{
			get => filterJsonCompleted;
			set
			{
				filterJsonCompleted = value;
				OnPropertyChanged();
			}
		}

		public bool? FilterPreview
		{
			get => filterPreview;
			set
			{
				filterPreview = value;
				FilterList();
				OnPropertyChanged();
			}
		}

		public bool? FilterLink
		{
			get => filterLink;
			set
			{
				filterLink = value;
				FilterList();
				OnPropertyChanged();
			}
		}

		public string? FilterName
		{
			get => filterName;
			set
			{
				filterName = value;
				FilterList();
				OnPropertyChanged();
			}
		}

		public List<string> Categories
		{
			get => categories;
			set
			{
				categories = value;
				if (!categories.Contains(SelectedCategory)) SelectedCategory = "No Category";
				FilterList();
				OnPropertyChanged();
			}
		}

		public string SelectedCategory
		{
			get => selectedCategory;
			set
			{
				selectedCategory = value;
				FilterList();
				OnPropertyChanged();
			}
		}

		public List<Model> DisplayModels
		{
			get => displayModels;
			private set
			{
				displayModels = value;
				OnPropertyChanged();
				OnPropertyChanged();
			}
		}

		public List<Model> GetModels => models;

		static MainWindow()
		{
			Config = new Configuration();
		}

		public MainWindow()
		{

			if (string.IsNullOrEmpty(Config.SDModelFolder) || !Directory.Exists(SDPath))
			{
				MessageBox.Show("You will need to configure the settings first.");
				new SettingsWindow(Config).ShowDialog();
			}

			if (!Directory.Exists(SDPath))
			{
				throw new Exception("Invalid SD path after getting settings");
			}

			models = LoadAllLoras();
			displayModels = models;
			categories = GetCategories();

			InitializeComponent();
		}

		private static List<Model> LoadAllLoras()
		{
			DirectoryInfo loraDir = new DirectoryInfo(Path.Join(SDPath, LoraPath));
			FileInfo[] files = loraDir.GetFiles("*", SearchOption.AllDirectories);
			files = FilterIgnored(files);
			List<FileInfo> modelFiles = files.Where(e => Model.ValidModelFiletypes.Contains(Path.GetExtension(e.Name))).ToList();

			List<Model> model = new List<Model>();
			foreach (FileInfo modelFile in modelFiles)
			{
				model.Add(new Model(modelFile, ModelType.Lora));
			}

			return model;
		}
		public static FileInfo[] FilterIgnored(FileInfo[] fileList)
		{
			FileInfo[] files = fileList.ToArray();
			foreach (string ignoredFolder in Config.IgnoredModelFolders)
			{
				string fullFolder = Path.Join(Config.SDModelFolder, ignoredFolder);
				files = files.Where(e => !e.FullName.StartsWith(fullFolder)).ToArray();
			}

			return files;
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
			categories.AddRange(models.Select(e => e.Category).Distinct().OrderBy(e => e));
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
				filtered = filtered.Where(e => e.Name.ToLower().Contains(FilterName.ToLower())).ToList();
			}

			DisplayModels = filtered;
		}

		private void JsonBtn_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show("This is not implemented yet");
		}

		private void FindOrphansButton_Click(object sender, RoutedEventArgs e)
		{
			OrphanFiles win = new OrphanFiles(this);
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

		private void FileProcessorButton_Click(object sender, RoutedEventArgs e)
		{
			if (!Directory.Exists(Config.OutputFolder))
			{
				throw new Exception("Invalid Output Folder path");
			}
			FileProcessorWindow win = new FileProcessorWindow(Config.OutputFolder);
			win.Show();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		public void Refresh()
		{
			models = LoadAllLoras();
			Categories = GetCategories();
			FilterList();
		}

		public void Move(Model model)
		{
			//Pop up new modal to ask what category to move to;
			ChooseCategory catDiag = new ChooseCategory(categories, model.Category, model.Name);
			catDiag.ShowDialog();

			if (catDiag.DialogResult != true)
			{
				return;
			}

			if (catDiag.ModelNameModified)
			{
				if (models.Any(e => e.Name == catDiag.ModelName))
				{
					MessageBox.Show("That model name is already in use!");
					return;
				}

				model.Move(catDiag.ResultString, catDiag.ModelName);
			}
			else
			{
				model.Move(catDiag.ResultString);
			}

			Refresh();
		}

		public void Delete(Model model)
		{
			//As user for confirmation of the destructive operation;
			MessageBoxResult result = MessageBox.Show("Are you sure you want to delete this model?", "Delete Model", MessageBoxButton.YesNo);
			if (result == MessageBoxResult.No)
			{
				return;
			}

			model.Delete();

			Refresh();
		}

		private void SettingsButton_click(object sender, RoutedEventArgs e)
		{
			new SettingsWindow(Config).ShowDialog();

			Refresh();
		}
	}
}
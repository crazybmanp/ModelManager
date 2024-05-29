using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace ModelManager;

/// <summary>
/// Interaction logic for ChooseCategory.xaml
/// </summary>
public partial class ChooseCategory : Window, INotifyPropertyChanged
{
	private string newCategoryName = string.Empty;
	private List<string> categories;
	private string selectedCategory;
	private string modelName;
	private string originalModelName;

	private const string AddNewCategory = "Add New Category";

	public ChooseCategory(List<string> categoryList, string currentCategory, string originalModelName)
	{
		categories = categoryList.ToList();
		categories.Remove("No Category");
		categories.Insert(0, AddNewCategory);
		selectedCategory = currentCategory;
		this.originalModelName = originalModelName;
		modelName = originalModelName;

		InitializeComponent();
	}

	public bool AddNewCategoryEnabled => SelectedCategory == AddNewCategory;

	public string ResultString
	{
		get
		{
			if (DialogResult == true)
				if (AddNewCategoryEnabled)
					return NewCategoryName;
				else
					return SelectedCategory;
			else
				return string.Empty;
		}
	}

	public string NewCategoryName
	{
		get => newCategoryName;
		set
		{
			newCategoryName = value;
			OnPropertyChanged();
		}
	}

	public List<string> Categories
	{
		get => categories;
		set
		{
			categories = value;
			OnPropertyChanged();
		}
	}

	public string SelectedCategory
	{
		get => selectedCategory;
		set
		{
			selectedCategory = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(AddNewCategoryEnabled));
		}
	}

	public string ModelName
	{
		get => modelName;
		set
		{
			modelName = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ModelNameModified));
			OnPropertyChanged(nameof(ModelNameColor));
		}
	}

	public bool ModelNameModified => ModelName != originalModelName;

	public Brush? ModelNameColor => ModelNameModified ? new SolidColorBrush(Colors.MediumSeaGreen) : null;

	private void MoveButton_Click(object sender, RoutedEventArgs e)
	{
		if (SelectedCategory == AddNewCategory)
		{
			if (NewCategoryName == String.Empty)
			{
				MessageBox.Show("We cannot move to an invalid path");
				return;
			}
		}
		DialogResult = true;
		Close();
	}

	private void CancelButton_Click(object sender, RoutedEventArgs e)
	{
		SelectedCategory = AddNewCategory;
		NewCategoryName = String.Empty;
		modelName = originalModelName;
		DialogResult = false;
		Close();
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
}
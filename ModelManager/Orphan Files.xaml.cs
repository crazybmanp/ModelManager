using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace ModelManager
{
	/// <summary>
	/// Interaction logic for Orphan_Files.xaml
	/// </summary>
	public partial class OrphanFiles : Window
	{
		private readonly MainWindow parent;
		private readonly ObservableCollection<Orphan> orphanList = new ObservableCollection<Orphan>();
		private List<Model> Models => parent.GetModels;

		public ObservableCollection<Orphan> OrphanList => orphanList;

		public OrphanFiles(MainWindow parent)
		{
			this.parent = parent;
			FindOrphanFiles();

			InitializeComponent();
		}

		private void FindOrphanFiles()
		{
			List<string> allFiles = GetAllFiles();
			List<string> attachedFiles = GetAttachedFiles();
			List<string> orphanFiles = allFiles.Except(attachedFiles).ToList();

			List<Orphan> orphans = orphanFiles.Select(path => new Orphan { Path = path }).ToList();

			orphanList.Clear();
			orphans.ForEach(e => orphanList.Add(e));
		}

		private List<string> GetAllFiles()
		{
			DirectoryInfo loraDir = new DirectoryInfo(Path.Join(MainWindow.SDPath, MainWindow.LoraPath));
			FileInfo[] allFiles = loraDir.GetFiles("*", SearchOption.AllDirectories);
			return MainWindow.FilterIgnored(allFiles).Select(e => e.FullName).ToList();
		}

		private List<string> GetAttachedFiles()
		{
			List<string> paths = new List<string>();

			foreach (Model model in Models)
			{
				paths.Add(model.ModelFile.FullName);
				if (model.PreviewFile != null) paths.Add(model.PreviewFile.FullName);
				if (model.LinkFile != null) paths.Add(model.LinkFile.FullName);
				if (model.JsonFile != null) paths.Add(model.JsonFile.MetaFile.FullName);
			}

			return paths;
		}

		private void KillButton_Click(object sender, RoutedEventArgs e)
		{
			MessageBoxResult result = MessageBox.Show("Are you sure you want to delete the selected files?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (result == MessageBoxResult.Yes)
			{
				List<Orphan> selectedOrphans = OrphanList.Where(orphan => orphan.Selected).ToList();

				foreach (Orphan orphan in selectedOrphans)
				{
					File.Delete(orphan.Path);
				}

				Refresh();
			}
			else
			{
				MessageBox.Show("Canceling Deletion", "Canceled", MessageBoxButton.OK);
			}
		}

		private void MoveButton_Click(object sender, RoutedEventArgs e)
		{
			//have the user select a folder

			FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
			DialogResult result = folderBrowserDialog.ShowDialog();

			if (result == System.Windows.Forms.DialogResult.OK)
			{
				string newPath = folderBrowserDialog.SelectedPath;
				List<Orphan> selectedOrphans = OrphanList.Where(orphan => orphan.Selected).ToList();

				foreach (Orphan orphan in selectedOrphans)
				{
					string relativePath = Path.GetRelativePath(Path.Join(MainWindow.SDPath), orphan.Path);
					string newFilePath = Path.Join(newPath, relativePath);
					Directory.CreateDirectory(Path.GetDirectoryName(newFilePath) ?? throw new InvalidOperationException());
					File.Move(orphan.Path, newFilePath);
				}

				Refresh();
			}
			else
			{
				MessageBox.Show("No folder selected");
			}
		}

		private void Refresh()
		{
			parent.Refresh();
			FindOrphanFiles();
		}

		private void RefreshButton_Click(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
		{
			bool ch = SelectAllCb.IsChecked ?? false;

			foreach (Orphan orphan in orphanList)
			{
				orphan.Selected = ch;
			}

			//Hack to force the list to update
			List<Orphan> orphans = orphanList.ToList();
			orphanList.Clear();
			orphans.ForEach(e => orphanList.Add(e));
		}

		private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
		{
			Process.Start("explorer.exe", "/select, " + e.Uri.AbsoluteUri);
			e.Handled = true;
		}
	}

	public class Orphan
	{
		public required string Path { get; init; }
		public bool Selected { get; set; } = false;

		public string DisplayPath => System.IO.Path.GetRelativePath(MainWindow.SDPath, Path);
	}
}

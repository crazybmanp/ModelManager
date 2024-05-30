using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Config.Net;

namespace ModelManager
{
	public interface IConfiguration
	{
		public string SDModelFolder { get; set; }

		[DefaultValue("")]
		public String[] IgnoredModelFolders { get; set; }

		public string OutputFolder { get; set; }
	}

	public class Configuration : IConfiguration, INotifyPropertyChanged
	{
		private static readonly IConfiguration Config = new ConfigurationBuilder<IConfiguration>()
			.UseJsonFile("config.json")
			.Build();

		private string? sdModelFolderModified = null;
		private string[]? ignoredModelFoldersModified = null;
		private string? outputFolderModified = null;

		#region Properties

		public string SDModelFolder
		{
			get => sdModelFolderModified ?? Config.SDModelFolder;
			set
			{
				if (value != Config.SDModelFolder)
				{
					sdModelFolderModified = value;
				}
				else
				{
					sdModelFolderModified = null;
				}
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsSdModelFolderValid));
				OnPropertyChanged(nameof(SdFolderBackgroundBrush));
				OnPropertyChanged(nameof(IsChangesValid));
				OnPropertyChanged(nameof(HasChanges));
			}
		}

		public string[] IgnoredModelFolders
		{
			get => ignoredModelFoldersModified ?? Config.IgnoredModelFolders;
			set
			{
				if (value.SequenceEqual(Config.IgnoredModelFolders))
				{
					ignoredModelFoldersModified = null;
				}
				else
				{
					ignoredModelFoldersModified = value;
				}
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsIgnoredModelFoldersValid));
				OnPropertyChanged(nameof(IgnoredModelFoldersBackgroundBrush));
				OnPropertyChanged(nameof(IsChangesValid));
				OnPropertyChanged(nameof(HasChanges));
			}
		}

		public string OutputFolder
		{
			get => outputFolderModified ?? Config.OutputFolder;
			set
			{
				if (value != Config.OutputFolder)
				{
					outputFolderModified = value;
				}
				else
				{
					outputFolderModified = null;
				}
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsOutputFolderValid));
				OnPropertyChanged(nameof(OutputFolderBackgroundBrush));
				OnPropertyChanged(nameof(IsChangesValid));
				OnPropertyChanged(nameof(HasChanges));
			}
		}

		public bool IsSdModelFolderValid
		{
			get
			{
				return Directory.Exists(SDModelFolder);
			}
		}

		public bool IsIgnoredModelFoldersValid
		{
			get
			{
				foreach (string folder in IgnoredModelFolders)
				{
					if (!Directory.Exists(Path.Join(SDModelFolder, folder)))
					{
						return false;
					}
				}

				return true;
			}
		}

		public bool IsOutputFolderValid
		{
			get
			{
				return Directory.Exists(OutputFolder);
			}
		}

		public bool IsChangesValid => IsSdModelFolderValid && IsIgnoredModelFoldersValid && IsOutputFolderValid;

		public bool HasChanges => sdModelFolderModified is not null || ignoredModelFoldersModified is not null ||
		                          outputFolderModified is not null;

		public Brush? SdFolderBackgroundBrush => IsSdModelFolderValid ? null : new SolidColorBrush(Colors.IndianRed);
		public Brush? IgnoredModelFoldersBackgroundBrush => IsIgnoredModelFoldersValid ? null : new SolidColorBrush(Colors.IndianRed);
		public Brush? OutputFolderBackgroundBrush => IsOutputFolderValid ? null : new SolidColorBrush(Colors.IndianRed);

		#endregion

		public void Save()
		{
			if(sdModelFolderModified is not null)
			{
				Config.SDModelFolder = sdModelFolderModified;
				sdModelFolderModified = null;
			}

			if(ignoredModelFoldersModified is not null)
			{
				Config.IgnoredModelFolders = ignoredModelFoldersModified;
				ignoredModelFoldersModified = null;
			}

			if(outputFolderModified is not null)
			{
				Config.OutputFolder = outputFolderModified;
				outputFolderModified = null;
			}

			OnPropertyChanged(nameof(IsChangesValid));
			OnPropertyChanged(nameof(HasChanges));
		}

		public void ClearChanges()
		{
			sdModelFolderModified = null;
			ignoredModelFoldersModified = null;
			outputFolderModified = null;

			OnPropertyChanged(nameof(IsChangesValid));
			OnPropertyChanged(nameof(HasChanges));
		}

		#region INotifyPropertyChanged

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

		#endregion
	}
}

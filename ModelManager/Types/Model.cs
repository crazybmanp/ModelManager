﻿using System;
using System.IO;
using System.Security.Policy;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic.FileIO;
using static System.Net.WebRequestMethods;
using File = System.IO.File;

namespace ModelManager;


public class Model
{
	public ModelType Type;
	public string Name { get; set; }

	public FileInfo ModelFile;
	public ModelFileType ModelFileType;
	public ModelJson? JsonFile;
	public FileInfo? PreviewFile;
	public FileInfo? LinkFile;

	public String Category { get; set; }

	public bool IsJsonComplete => JsonFile != null && JsonFile.IsComplete();
	public bool isPreviewComplete => PreviewFile is not null;
	public bool isLinkComplete => LinkFile is not null;

	public string GetmodelFileBase => Path.Join(MainWindow.SDPath, MainWindow.LoraPath, Category, Name);

	public BitmapImage ImageSource
	{
		get
		{
			if (PreviewFile is not null)
			{
				using FileStream stream = File.OpenRead(PreviewFile.FullName);
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = stream;
				image.EndInit();
				return image;
			}
			else
			{
				return NoPreview;
			}
		}
	}

	public bool IsComplete => (JsonFile is not null && PreviewFile is not null && LinkFile is not null && JsonFile.IsComplete());
	public Brush IsCompleteColor => new SolidColorBrush(IsComplete ? Colors.LightGreen : Colors.IndianRed);
	public string DisplayModelPositivePrompt => String.IsNullOrEmpty(JsonFile?.ActivationText ?? "") ? "[Not Specified]" : JsonFile!.ActivationText!;
	public string DisplayModelDefaultWeight => JsonFile?.PreferredWeight?.ToString() ?? "None";
	public string DisplayModelDescription => String.IsNullOrEmpty(JsonFile?.Description ?? "") ? "[Not Specified]" : JsonFile!.Description!;

	public static BitmapImage? noPreviewImage;
	public static BitmapImage NoPreview
	{
		get
		{
			if (noPreviewImage is not null)
			{
				return noPreviewImage;
			}
			else
			{
				var path = Path.Join(Directory.GetCurrentDirectory(), @"Resources\card-no-preview.png");
				using FileStream stream = File.OpenRead(path);
				BitmapImage image = new BitmapImage();
				image.BeginInit();
				image.CacheOption = BitmapCacheOption.OnLoad;
				image.StreamSource = stream;
				image.EndInit();
				return image;
			}
		}
	}

	public string? GetUrl
	{
		get
		{
			if (LinkFile != null)
			{
				string[] lines = File.ReadAllLines(LinkFile.FullName);

				foreach (string line in lines)
				{
					if (line.StartsWith("URL=", StringComparison.OrdinalIgnoreCase))
					{
						return line.Substring(4);
					}
				}
			}

			return null;
		}
	}

	public string GetLinkTitle
	{
		get
		{
			//if the link exists, get the domain for the link
			string? link = GetUrl;

			if (link != null)
			{
				Uri uri = new Uri(link);
				return uri.Host;
			}
			else
			{
				return "No Link";
			}
		}
	}

	public Model(FileInfo ModelFile, ModelType Type)
	{
		string path = Path.GetDirectoryName(ModelFile.FullName) ?? throw new Exception("Model cannot be the root");

		this.Type = Type;
		Name = Path.GetFileNameWithoutExtension(ModelFile.Name);

		this.ModelFile = ModelFile;
		ModelFileType = GetModelFileType(ModelFile.Extension);

		string jfn = Path.Join(path, $"{Name}.json");
		JsonFile = File.Exists(jfn) ? new ModelJson(new FileInfo(jfn)) : null;

		string pfn = Path.Join(path, $"{Name}.png");
		PreviewFile = File.Exists(pfn) ? new FileInfo(pfn) : null;

		string lfn = Path.Join(path, $"{Name}.url");
		LinkFile = File.Exists(lfn) ? new FileInfo(lfn) : null;

		//The Category should be the relative path from SDPath + LoraPath to the directory that contains the model file
		Category = Path.GetRelativePath(Path.Join(MainWindow.SDPath, MainWindow.LoraPath), path);
	}

	public static readonly string[] ValidModelFiletypes = [".safetensors", ".pkl", ".pickle", ".pt"];
	private static ModelFileType GetModelFileType(string extension)
	{
		return extension switch
		{
			".safetensors" => ModelFileType.Safetensors,
			".pkl" or ".pickle" or ".pt" => ModelFileType.Pickle,
			_ => ModelFileType.Unknown
		};
	}

	public Model(ModelDto data)
	{
		Type = data.Type;
		Name = data.Name;

		ModelFile = new FileInfo(data.ModelFile);
		ModelFileType = data.ModelFileType;
		JsonFile = data.JsonFile is not null ? new ModelJson(data.JsonFile) : null;
		PreviewFile = data.PreviewFile is not null ? new FileInfo(data.PreviewFile) : null;
		LinkFile = data.LinkFile is not null ? new FileInfo(data.LinkFile) : null;
		Category = data.Category;
	}

	public ModelDto GetDto()
	{
		return new ModelDto
		{
			Type = Type,
			Name = Name,
			ModelFile = ModelFile.FullName,
			ModelFileType = ModelFileType,
			JsonFile = JsonFile?.GetDto(),
			PreviewFile = PreviewFile?.FullName,
			LinkFile = LinkFile?.FullName,
			Category = Category
		};
	}

	public void RemoveEmptyDirectory(string category)
	{
		DirectoryInfo directory = new DirectoryInfo(Path.Join(MainWindow.SDPath, MainWindow.LoraPath, category));
		if (directory.GetFiles().Length == 0 && directory.GetDirectories().Length == 0)
		{
			directory.Delete();
		}
	}

	public void Move(string category, string? newName=null)
	{
		string newDirectory = Path.Join(MainWindow.SDPath, MainWindow.LoraPath, category);
		Directory.CreateDirectory(newDirectory);
		string newPath = Path.Join(newDirectory, newName ?? Name);

		if (File.Exists($"{newPath}{ModelFile.Extension}"))
		{
			MessageBox.Show("Cannot move the file, a model with the same name already exists there.");
			return;
		}

		if (PreviewFile is not null && File.Exists($"{newPath}{PreviewFile.Extension}"))
		{
			MessageBox.Show("Cannot move the file, a preview with the same name already exists there.");
			return;
		}

		if (LinkFile is not null && File.Exists($"{newPath}{LinkFile.Extension}"))
		{
			MessageBox.Show("Cannot move the file, a link file with the same name already exists there.");
			return;
		}

		if (JsonFile is not null && File.Exists($"{newPath}{JsonFile.MetaFile.Extension}"))
		{
			MessageBox.Show("Cannot move the file, a json file with the same name already exists there.");
			return;
		}


		ModelFile.MoveTo($"{newPath}{ModelFile.Extension}");

		if (PreviewFile is not null)
		{
			PreviewFile.MoveTo($"{newPath}{PreviewFile.Extension}");
		}

		if (LinkFile is not null)
		{
			LinkFile.MoveTo($"{newPath}{LinkFile.Extension}");
		}

		if (JsonFile is not null)
		{
			JsonFile.MetaFile.MoveTo($"{newPath}{JsonFile.MetaFile.Extension}");
		}

		RemoveEmptyDirectory(Category);

		Category = category;
	}

	public void Delete()
	{
		FileSystem.DeleteFile(ModelFile.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);

		if (PreviewFile is not null)
		{
			FileSystem.DeleteFile(PreviewFile.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
		}

		if (LinkFile is not null)
		{
			FileSystem.DeleteFile(LinkFile.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
		}

		if (JsonFile is not null)
		{
			FileSystem.DeleteFile(JsonFile.MetaFile.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
		}

		RemoveEmptyDirectory(Category);
	}

	internal void SetUrl(string modifiedLink)
	{
		string path = Path.Join(MainWindow.SDPath, MainWindow.LoraPath, Category, $"{Name}.url");

		File.WriteAllText(path, $"[InternetShortcut]\nURL={modifiedLink}");
	}
}

public class ModelDto
{
	public required ModelType Type { get; init; }
	public required string Name { get; init; }

	public required string ModelFile { get; init; }
	public required ModelFileType ModelFileType { get; init; }
	public ModelFileDto? JsonFile { get; init; }
	public string? PreviewFile { get; init; }
	public string? LinkFile { get; init; }
	public required string Category { get; init; }
}

public enum ModelType
{
	Checkpoint,
	Lora,
	Vae,
}

public enum ModelFileType
{
	Safetensors,
	Pickle,
	Unknown
}
using System.IO;

namespace ModelManager;


public class Model
{

	public static string SDPath = @"K:\SD webui\models\";
	public static string LoraPath = @"Lora\";

	public ModelType Type;
	public string Name { get; set; }

	public FileInfo ModelFile;
	public ModelFileType ModelFileType;
	public ModelJson? JsonFile;
	public FileInfo? PreviewFile;
	public FileInfo? LinkFile;
	
	public String Category { get; set; }
	
	public bool IsJsonComplete => JsonFile != null && JsonFile.IsComplete();
	public bool IsJsonPPComplete => JsonFile is { ActivationText: not null };
	public bool IsJsonDWComplete => JsonFile is { PreferredWeight: not null };
	public bool isPreviewComplete => PreviewFile is not null;
	public bool isLinkComplete => LinkFile is not null;

	public string ImageSource => PreviewFile?.FullName ?? Path.Join(Directory.GetCurrentDirectory(), @"Resources\card-no-preview.png");

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
		Category = Path.GetRelativePath(Path.Join(SDPath, LoraPath), path);
	}

	public static readonly string[] ValidModelFiletypes = [".safetensors", ".pkl", ".pickle"];
	private static ModelFileType GetModelFileType(string extension)
	{
		return extension switch
		{
			".safetensors" => ModelFileType.Safetensors,
			".pkl" or ".pickle" => ModelFileType.Pickle,
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

	public bool IsComplete => (JsonFile is not null && PreviewFile is not null && LinkFile is not null && JsonFile.IsComplete());
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
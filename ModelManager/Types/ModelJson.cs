using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ModelManager
{
	public class ModelJson
	{
		public FileInfo MetaFile { get; init; }
		public string? Description { get; set; }
		public string? SdVersion { get; set; }
		public string? ActivationText { get; set; }
		public double? PreferredWeight { get; set; }
		public string? NegativeText { get; set; }
		public string? Notes { get; set; }

		public ModelJson(FileInfo metaFile)
		{
			this.MetaFile = metaFile;

			string f = File.ReadAllText(metaFile.FullName);

			SDModelInfo data = JsonSerializer.Deserialize<SDModelInfo>(f, SerializerOptions) ?? throw new Exception("ModelJsonFailed to deserialize");

			Description = data.Description;
			SdVersion = data.SdVersion;
			ActivationText = data.ActivationText;
			PreferredWeight = data.PreferredWeight;
			NegativeText = data.NegativeText;
			Notes = data.Notes;
		}

		public ModelJson(ModelFileDto data)
		{
			MetaFile = new FileInfo(data.MetaFile);
			Description = data.Description;
			SdVersion = data.SdVersion;
			ActivationText = data.ActivationText;
			PreferredWeight = data.PreferredWeight;
			NegativeText = data.NegativeText;
			Notes = data.Notes;
		}

		public ModelJson(string fileName, string? description, string? sdVersion, string? activationText, double? preferredWeight, string? negativeText, string? notes)
		{
			MetaFile = new FileInfo(fileName);

			Description = description;
			SdVersion = sdVersion;
			ActivationText = activationText;
			PreferredWeight = preferredWeight;
			NegativeText = negativeText;
			Notes = notes;

			Save();
		}
		public void Save()
		{
			SDModelInfo data = new SDModelInfo()
			{
				Description = Description??"",
				SdVersion = SdVersion??"SD1",
				ActivationText = ActivationText??"",
				PreferredWeight = PreferredWeight??0,
				NegativeText = NegativeText??"",
				Notes = Notes??""
			};

			string jsonString = JsonSerializer.Serialize(data, SerializerOptions);
			File.WriteAllText(MetaFile.FullName, jsonString);
		}

		public ModelFileDto GetDto()
		{
			return new ModelFileDto
			{
				MetaFile = MetaFile.FullName,
				Description = Description,
				SdVersion = SdVersion,
				ActivationText = ActivationText,
				PreferredWeight = PreferredWeight,
				NegativeText = NegativeText,
				Notes = Notes
			};
		}

		public bool IsComplete() =>
		(
			SdVersion is not null &&
			ActivationTextIsComplete &&
			PreferredWeightIsComplete
		);

		private bool PreferredWeightIsComplete
		{
			get
			{
				if (PreferredWeight is not null && PreferredWeight is not 0.0) return true;
				if (string.IsNullOrEmpty(Description)) return false;
				return HasWeightRange(Description);
			}
		}

		private bool ActivationTextIsComplete
		{
			get
			{
				if (!string.IsNullOrEmpty(ActivationText)) return true;
				if (string.IsNullOrEmpty(Description)) return false;
				return Description.Contains(@"N/A");
			}
		}

		private static bool HasWeightRange(string description)
		{
			//A weight range is in the format of either [-2 to 2] or [-2.0 to 2.0]
			Regex regex = new Regex(@"\[-?\d+(\.\d+)? to -?\d+(\.\d+)?\]");
			return regex.IsMatch(description);
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
	}

	public class ModelFileDto
	{
		public required string MetaFile { get; init; }
		public string? Description { get; set; }
		public string? SdVersion { get; set; }
		public string? ActivationText { get; set; }
		public double? PreferredWeight { get; set; }
		public string? NegativeText { get; set; }
		public string? Notes { get; set; }
	}

	public class SDModelInfo
	{
		[JsonPropertyName("description")]
		public string? Description { get; init; }

		[JsonPropertyName("sd version")]
		public string? SdVersion { get; init; }

		[JsonPropertyName("activation text")]
		public string? ActivationText { get; init; }

		[JsonPropertyName("preferred weight")]
		public double? PreferredWeight { get; init; }

		[JsonPropertyName("negative text")]
		public string? NegativeText { get; init; }

		[JsonPropertyName("notes")]
		public string? Notes { get; init; }
	}
}

﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace ModelManager
{

	public partial class ModelViewer : Window, INotifyPropertyChanged
	{
		private Model targetModel;

		#region BackingValues

		private string? modifiedName;
		private string? modifiedCategory;

		private string? modifiedDescription;
		private string? modifiedSdVersion;
		private string? modifiedActivationText;
		private double? modifiedPreferredWeight;
		private string? modifiedNegativeText;
		private string? modifiedNotes;

		private string? modifiedLink;

		#endregion

		#region Properties

		public string Image => targetModel.ImageSource;

		public string Link
		{
			get => (modifiedLink ?? targetModel.GetUrl) ?? string.Empty;
			set
			{
				modifiedLink = value == targetModel.GetUrl ? null : value;
				if (modifiedLink == "" && targetModel.GetUrl is null) modifiedLink = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string ModelName
		{
			get => modifiedName ?? targetModel.Name;
			set
			{
				modifiedName = value == targetModel.Name ? null : value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string Category
		{
			get => modifiedCategory ?? targetModel.Category;
			set
			{
				modifiedCategory = value == targetModel.Category ? null : value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string Description
		{
			get => modifiedDescription ?? targetModel.JsonFile?.Description ?? string.Empty;
			set
			{
				modifiedDescription = value == targetModel.JsonFile?.Description ? null : value;
				if (modifiedDescription == "" && targetModel.JsonFile?.Description is null) modifiedDescription = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string SdVersion
		{
			get => modifiedSdVersion ?? targetModel.JsonFile?.SdVersion ?? "Not Set";
			set
			{
				modifiedSdVersion = value == targetModel.JsonFile?.SdVersion ? null : value;
				if (modifiedSdVersion == "Not Set" && targetModel.JsonFile?.SdVersion is null) modifiedSdVersion = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string ActivationText
		{
			get => modifiedActivationText ?? targetModel.JsonFile?.ActivationText ?? string.Empty;
			set
			{
				modifiedActivationText = value == targetModel.JsonFile?.ActivationText ? null : value;
				if (modifiedActivationText == "" && targetModel.JsonFile?.ActivationText is null) modifiedActivationText = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public double PreferredWeight
		{
			get => modifiedPreferredWeight ?? targetModel.JsonFile?.PreferredWeight ?? 0.0;
			set
			{
				modifiedPreferredWeight = value == targetModel.JsonFile?.PreferredWeight ? null : value;
				if (modifiedPreferredWeight == 0.0 && targetModel.JsonFile?.PreferredWeight is null) modifiedPreferredWeight = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string NegativeText
		{
			get => modifiedNegativeText ?? targetModel.JsonFile?.NegativeText ?? string.Empty;
			set
			{
				modifiedNegativeText = value == targetModel.JsonFile?.NegativeText ? null : value;
				if (modifiedNegativeText == "" && targetModel.JsonFile?.NegativeText is null) modifiedNegativeText = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public string Notes
		{
			get => modifiedNotes ?? targetModel.JsonFile?.Notes ?? string.Empty;
			set
			{
				modifiedNotes = value == targetModel.JsonFile?.Notes ? null : value;
				if (modifiedNotes == "" && targetModel.JsonFile?.Notes is null) modifiedNotes = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(IsEdited));
			}
		}

		public bool IsEdited =>
			modifiedName != null ||
			modifiedCategory != null ||
			modifiedDescription != null ||
			modifiedSdVersion != null ||
			modifiedActivationText != null ||
			modifiedPreferredWeight != null ||
			modifiedNegativeText != null ||
			modifiedNotes != null ||
			modifiedLink != null;

		#endregion

		public ModelViewer(Model model)
		{
			targetModel = model;

			InitializeComponent();
		}

		#region EventHandlers

		private void SaveButton_Click(object sender, RoutedEventArgs e)
		{
			//Validation
			ReadOnlyObservableCollection<ValidationError> result = Validation.GetErrors(PreferedWeightTxt);
			if (result.Count > 0)
			{
				MessageBox.Show("Please enter a valid number for the preferred weight.");
				return;
			}

			//commit data to the model

			//Save

		}

		#endregion

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

using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WildRP.AMVTool.GUI;

public partial class AmvBakerGui : Control
{
	[ExportGroup("Model loading")]
		[Export] private PackedScene _modelListItem;
		[ExportSubgroup("UI Elements")]
			[Export] private Button _loadModelButton;
			[Export] private Label _modelNameLabel;
			[Export] private FileDialog _fileDialog;
			[Export] private VBoxContainer _modelListContainer;
			
	[ExportGroup("AMV")]
		[Export] private PackedScene _amvScene;
		[Export] private Node3D _amvContainerNode;
		[ExportSubgroup("UI Elements")]
			[Export] private AmvList _amvList;
			[Export] private AMVListContextMenu _amvListContextMenu;
			[Export] private Button _newAmvButton;
	[ExportGroup("AMV Details")]
		[Export] private SpinBox _textureName;
		[Export] private SpinBox _rotation;
		[Export] private Button _randomizeTextureNameButton;
		[ExportSubgroup("Position")]
			[Export] private SpinBox _positionX;
			[Export] private SpinBox _positionY;
			[Export] private SpinBox _positionZ;
		[ExportSubgroup("Size")]
			[Export] private SpinBox _sizeX;
			[Export] private SpinBox _sizeY;
			[Export] private SpinBox _sizeZ;
		[ExportSubgroup("Size")]
			[Export] private SpinBox _spacingX;
			[Export] private SpinBox _spacingY;
			[Export] private SpinBox _spacingZ;
	
	private readonly List<ModelListItem> _modelListItems = new();
	
	public static AmbientMaskVolume SelectedAmv
	{
		get;
		private set;
	}
	
	public override void _Ready()
	{
		// File dialog
		_fileDialog.AddFilter("*.gltf, *.glb; GLTF Model File");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += LoadModel;
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};

		
		_newAmvButton.Pressed += CreateNewAmv;

		// AMV Select and deselect
		_amvList.ItemSelected += index => SelectAmv(AmvBaker.Instance.GetVolume(_amvList.GetItemText((int)index)));
		_amvList.EmptyClicked += (position, index) => SelectAmv(null);
		
		// AMV List Context Menu
		_amvList.OnRightClickItem += (name, pos) =>
		{
			_amvListContextMenu.Popup();
			_amvListContextMenu.Position = new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
			_amvListContextMenu.Select(name);
		};
	}

	private void LoadModel(string path)
	{
		var (e, result) = AmvBaker.Instance.LoadModel(path);
		
		// Clear out the list from whatever model we had loaded before
		foreach (var item in _modelListItems)
		{
			item.QueueFree();
		}
		
		if (e != Error.Ok) return; // display an error message here probably

		_modelNameLabel.Text = path.GetFile();
		
		foreach (var t in result)
		{
			if (_modelListItem.Instantiate() is not ModelListItem item) continue;
			
			item.Setup(t.Item1.Name, t.Item1, t.Item2);
			_modelListContainer.AddChild(item);
			_modelListItems.Add(item);
		}
	}

	private void CreateNewAmv()
	{
		var amv = _amvScene.Instantiate() as AmbientMaskVolume;
		string name = EnsureUniqueName($"AMV {_amvList.ItemCount+1}");
		var item =_amvList.AddItem(name);
		amv.Setup(name);
		_amvContainerNode.AddChild(amv);
		
		AmvBaker.Instance.RegisterAmv(amv);

		amv.Deleted += volume =>
		{
			for (int i = 0; i < _amvList.ItemCount; i++)
			{
				if (_amvList.GetItemText(i) != volume.ListName) continue;
				_amvList.RemoveItem(i);
				break;
			}
		};
		
		_amvList.Select(item);
		SelectAmv(amv);
	}

	string EnsureUniqueName(string name)
	{
		var n = name;
		var i = 1;
		while (IsNameUnique(n) == false)
		{
			n = $"{name} ({i})";
			i++;
		}
		return n;
	}
	
	bool IsNameUnique(string name)
	{
		for (int i = 0; i < _amvList.ItemCount; i++)
		{
			if (_amvList.GetItemText(i) == name) return false;
		}

		return true;
	}

	private void SelectAmv(AmbientMaskVolume volume)
	{
		if (SelectedAmv != null)
			SelectedAmv.SizeChanged -= UpdateAmvGuiValues;
		
		SelectedAmv = volume;
		SelectedAmv.SizeChanged += UpdateAmvGuiValues;
		UpdateAmvGuiValues();
	}

	private void UpdateAmvGuiValues()
	{
		// v for valid
		bool v = SelectedAmv != null;

		_textureName.GetLineEdit().Text = v ? SelectedAmv.TextureName.ToString(CultureInfo.InvariantCulture) : _textureName.MinValue.ToString(CultureInfo.InvariantCulture);
		_rotation.GetLineEdit().Text = v ? SelectedAmv.Rotation.Y.ToString(CultureInfo.InvariantCulture) : "0";
		
		// Note that we swap Z and Y here to present RDR2-style coordinates to the end user
		_positionX.GetLineEdit().Text = v ? SelectedAmv.Position.X.ToString(CultureInfo.InvariantCulture) : "0";
		_positionY.GetLineEdit().Text = v ? SelectedAmv.Position.Z.ToString(CultureInfo.InvariantCulture) : "0";
		_positionZ.GetLineEdit().Text = v ? SelectedAmv.Position.Y.ToString(CultureInfo.InvariantCulture) : "0";
		
		_sizeX.GetLineEdit().Text = v ? SelectedAmv.Size.X.ToString(CultureInfo.InvariantCulture) : "0";
		_sizeY.GetLineEdit().Text = v ? SelectedAmv.Size.Z.ToString(CultureInfo.InvariantCulture) : "0";
		_sizeZ.GetLineEdit().Text = v ? SelectedAmv.Size.Y.ToString(CultureInfo.InvariantCulture) : "0";
		
		_spacingX.GetLineEdit().Text = v ? SelectedAmv.Spacing.X.ToString(CultureInfo.InvariantCulture) : "0";
		_spacingY.GetLineEdit().Text = v ? SelectedAmv.Spacing.Z.ToString(CultureInfo.InvariantCulture) : "0";
		_spacingZ.GetLineEdit().Text = v ? SelectedAmv.Spacing.Y.ToString(CultureInfo.InvariantCulture) : "0";
	}
}

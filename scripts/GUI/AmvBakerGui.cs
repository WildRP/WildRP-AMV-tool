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
			[Export] private Control _amvInfoPanel;
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
		_amvList.EmptyClicked += (position, index) =>
		{
			SelectAmv(null);
			_amvList.DeselectAll();
		};
		
		// AMV List Context Menu
		_amvList.OnRightClickItem += (name, pos) =>
		{
			_amvListContextMenu.Popup();
			_amvListContextMenu.Position = new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
			_amvListContextMenu.Select(name);
		};
		
		ConnectAmvGui();
		_amvInfoPanel.Visible = false;
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
		amv.TextureName = (ulong) _textureName.MinValue;
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

		_amvInfoPanel.Visible = volume != null;
		
		SelectedAmv = volume;
		if (SelectedAmv != null)
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
		_positionX.SetValueNoSignal(v ? SelectedAmv.Position.X : 0);
		_positionY.SetValueNoSignal(v ? SelectedAmv.Position.Z : 0);
		_positionZ.SetValueNoSignal(v ? SelectedAmv.Position.Y : 0);
		
		_sizeX.SetValueNoSignal(v ? SelectedAmv.Size.X : 0);
		_sizeY.SetValueNoSignal(v ? SelectedAmv.Size.Z : 0);
		_sizeZ.SetValueNoSignal(v ? SelectedAmv.Size.Y : 0);
		
		_spacingX.SetValueNoSignal(v ? SelectedAmv.Spacing.X : 0);
		_spacingY.SetValueNoSignal(v ? SelectedAmv.Spacing.Z : 0);
		_spacingZ.SetValueNoSignal(v ? SelectedAmv.Spacing.Y : 0);
	}

	private void ConnectAmvGui()
	{
		_positionX.ValueChanged += value => SelectedAmv?.SetPositionX(value);
		_positionY.ValueChanged += value => SelectedAmv?.SetPositionZ(value);
		_positionZ.ValueChanged += value => SelectedAmv?.SetPositionY(value);
		
		_sizeX.ValueChanged += value => SelectedAmv?.SetSizeX(value);
		_sizeY.ValueChanged += value => SelectedAmv?.SetSizeZ(value);
		_sizeZ.ValueChanged += value => SelectedAmv?.SetSizeY(value);
		
		_spacingX.ValueChanged += value => SelectedAmv?.SetSpacingX(value);
		_spacingY.ValueChanged += value => SelectedAmv?.SetSpacingZ(value);
		_spacingZ.ValueChanged += value => SelectedAmv?.SetSpacingY(value);
	}
	
}

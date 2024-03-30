using Godot;
using System;
using System.Collections.Generic;
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
	
	private readonly List<ModelListItem> _modelListItems = new();

	private AmbientMaskVolume _selectedAmv;
	private AmbientMaskVolume SelectedAmv
	{
		get => _selectedAmv;
		set
		{
			if (SelectedAmv != null) SelectedAmv.Selected = false;
			value.Selected = true;
			_selectedAmv = value;
		}
	}
	
	public override void _Ready()
	{
		_fileDialog.AddFilter("*.gltf, *.glb; GLTF Model File");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += LoadModel;
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};

		_newAmvButton.Pressed += CreateNewAmv;

		_amvList.ItemSelected += SelectAmv;
		_amvList.OnRightClickItem += (name, pos) =>
		{
			_amvListContextMenu.Popup();
			_amvListContextMenu.Position = new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
			_amvListContextMenu.Select(name);
		};
	}

	private void LoadModel(string path)
	{
		var (e, result) = AMVBaker.Instance.LoadModel(path);
		
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
		_amvList.AddItem(name);
		amv.Setup(name);
		_amvContainerNode.AddChild(amv);
		
		AMVBaker.Instance.RegisterAmv(amv);

		amv.OnDeleted += volume =>
		{
			for (int i = 0; i < _amvList.ItemCount; i++)
			{
				if (_amvList.GetItemText(i) != volume.ListName) continue;
				_amvList.RemoveItem(i);
				break;
			}
		};
	}

	private void SelectAmv(long listIdx)
	{
		var volume = AMVBaker.Instance.GetVolume(_amvList.GetItemText((int)listIdx));
		SelectedAmv = volume;
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
}

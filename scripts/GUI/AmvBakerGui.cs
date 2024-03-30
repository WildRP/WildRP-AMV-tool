using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WildRP.AMVTool.GUI;

public partial class AmvBakerGui : Control
{
	[Export] private Button _testButton;
	
	[ExportGroup("Model loading")]
	[Export] private Button _loadModelButton;
	[Export] private Label _modelNameLabel;
	[Export] private FileDialog _fileDialog;

	[Export] private VBoxContainer _modelListContainer;
	[Export] private PackedScene _modelListItem;

	[ExportGroup("AMV")]
	[Export] private ItemList _amvList;
	[Export] private PopupMenu _amvListContextMenu;
	[Export] private Button _newAmvButton;
	
	private readonly List<ModelListItem> _modelListItems = new();
	private readonly List<AmbientMaskVolume> _ambientMaskVolumes = new();

	public float MaxOcclusionDistance = -1;
	
	public override void _Ready()
	{
		_fileDialog.AddFilter("*.gltf, *.glb; GLTF Model File");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += LoadModel;
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};

		_testButton.Pressed += () => AMVBaker.Instance.BakeTestProbes();
	}

	void LoadModel(string path)
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
	
}

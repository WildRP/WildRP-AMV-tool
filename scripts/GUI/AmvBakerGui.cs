using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WildRP.AMVTool.GUI;

public partial class AmvBakerGui : Control
{
	[ExportGroup("Model loading")]
	[Export] private Button _loadModelButton;
	[Export] private Label _modelNameLabel;
	[Export] private FileDialog _fileDialog;

	[Export] private VBoxContainer _modelListContainer;
	[Export] private PackedScene _modelListItem;

	[Export] private Button _testButton;
	
	private List<ModelListItem> _modelListItems = new();

	public float MaxOcclusionDistance = -1;
	
	public override void _Ready()
	{
		_fileDialog.AddFilter("*.glb; GLTF Binary");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += LoadModel;
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};

		_testButton.Pressed += () => AMVBaker.Instance.Bake();
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
		}
	}
	
}

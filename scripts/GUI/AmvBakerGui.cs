using Godot;
using System;

public partial class AmvBakerGui : Control
{
	[ExportGroup("Model loading")]
	[Export] private Button _loadModelButton;
	[Export] private Label _modelNameLabel;
	[Export] private FileDialog _fileDialog;
	
	public override void _Ready()
	{
		_fileDialog.AddFilter("*.glb; GLTF Binary");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += path => AMVBaker.Instance.LoadModel(path);
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

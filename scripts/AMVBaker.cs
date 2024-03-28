using Godot;
using System;

public partial class AMVBaker : Node3D
{
	[Export] private Node3D _placeholder;
	private Node _modelRoot;
	
	public static AMVBaker Instance { get; private set; }
	
	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
			QueueFree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Error LoadModel(string path)
	{
		if (path.EndsWith(".glb") == false) return Error.InvalidParameter;

		_modelRoot?.QueueFree();
		_placeholder.Visible = false;

		var modelDoc = new GltfDocument();
		var modelState = new GltfState();

		var error = modelDoc.AppendFromFile(path, modelState);
		if (error != Error.Ok) return error;

		_modelRoot = modelDoc.GenerateScene(modelState);
		AddChild(_modelRoot);
		
		return error;
	}
}

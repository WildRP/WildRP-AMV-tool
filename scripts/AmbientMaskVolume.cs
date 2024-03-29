using Godot;
using System;

public partial class AmbientMaskVolume : Node3D
{
	private Aabb _bounds = new(Vector3.Zero, Vector3.One * 2f);
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

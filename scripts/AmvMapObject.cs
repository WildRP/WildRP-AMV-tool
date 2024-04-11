using Godot;
using System;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.AmvMap;

public partial class AmvMapObject : Sprite2D
{
	public AmvMapGui.AmvMapInfo AmvInfo;
	public override void _Ready()
	{
		Position = new Vector2(AmvInfo.Position.X, -AmvInfo.Position.Y);
		RotationDegrees = AmvInfo.Rotation;
		Scale = new Vector2(AmvInfo.Scale.X*2, AmvInfo.Scale.Y*2);
	}
}

using Godot;
using System;
using WildRP.AMVTool.GUI;

namespace WildRP.AMVTool.AmvMap;

public partial class AmvMapObject : Sprite2D
{
	[Export] private StaticBody2D _collider;
	[Export] private Color _lowestLayerColor = Colors.Black;
	[Export] private Color _highestLayerColor = Colors.White;
	[Export] private Color _hoverColor;
	[Export] private Color _selectedColor = Colors.White;
	public AmvMapGui.AmvMapInfo AmvInfo;
	
	public override void _Ready()
	{
		Position = new Vector2(AmvInfo.Position.X, -AmvInfo.Position.Y);
		RotationDegrees = AmvInfo.Rotation;
		Scale = new Vector2(AmvInfo.Scale.X*2, AmvInfo.Scale.Y*2);

		_collider.SetCollisionLayerValue(Mathf.Clamp(Mathf.Abs(AmvInfo.Layer+1),1,32), true);
		ZIndex = AmvInfo.Layer;
	}

	public override void _Process(double delta)
	{
		var vis = (AmvInfo.Interior && AmvMapGui.InteriorsVisible) || (AmvInfo.Exterior && AmvMapGui.ExteriorsVisible);
		if (AmvInfo.AttachedToDoor) vis = AmvMapGui.DoorsVisible;

		Visible = vis;
		
		
		if (AmvMapGui.SelectedAmv == this)
		{
			Modulate = _selectedColor;
			return;
		}

		var col = _lowestLayerColor.Lerp(_highestLayerColor, (float)AmvInfo.Layer / AmvMapGui.HighestLayer);
		
		Modulate = AmvMapGui.HoveredAmv == this ? _hoverColor : col;
	}
}

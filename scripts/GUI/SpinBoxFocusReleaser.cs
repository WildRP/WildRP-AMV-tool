using Godot;
using System;

namespace WildRP.AMVTool.GUI;

public partial class SpinBoxFocusReleaser : SpinBox
{
	public override void _Ready()
	{
		// how is this not default behavior for a spinbox?
		GetLineEdit().TextSubmitted += text => GetLineEdit().ReleaseFocus();
	}


	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey key)
		{
			if (key.IsAction("ui_accept")) GetLineEdit().ReleaseFocus();
		}
		
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
		{
			var r = new Rect2(GetLineEdit().Position, GetLineEdit().Size);
			var rthis = new Rect2(Vector2.Zero, Size);
			if (r.HasPoint(btn.Position - GlobalPosition) == false && r.HasPoint(btn.Position - GlobalPosition) == false)
			{
				GetLineEdit().ReleaseFocus();
			}
		}
	}
}

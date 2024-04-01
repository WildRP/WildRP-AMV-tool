using Godot;

namespace WildRP.AMVTool.GUI;

// TODO: Let the user type in values. I don't know why this isn't triggering that.
public partial class SpinBoxFocusReleaser : SpinBox
{
	public override void _Ready()
	{
		// how is this not default behavior for a spinbox?
		GetLineEdit().TextSubmitted += text =>
		{
			if (GetLineEdit().HasFocus() == false) return;
		};
	}


	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left } btn)
		{
			var r = new Rect2(GetLineEdit().Position, GetLineEdit().Size);
			var rthis = new Rect2(Vector2.Zero, Size);
			if (r.HasPoint(btn.Position - GlobalPosition) == false && r.HasPoint(btn.Position - GlobalPosition) == false && GetLineEdit().HasFocus())
			{
				GetLineEdit().ReleaseFocus();
			}
		}
	}
}

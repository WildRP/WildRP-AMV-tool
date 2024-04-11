using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using WildRP.AMVTool.AmvMap;

namespace WildRP.AMVTool.GUI;

public partial class MapViewPanel : Control
{
	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton {ButtonIndex: MouseButton.Right} btn:
				AmvMapGui.Panning = btn.Pressed;
				break;
			case InputEventMouseButton {ButtonIndex: MouseButton.Left} btn:
				if (btn.Pressed)
					SelectMapAmv();
				break;
			case InputEventMouseButton {Pressed: true} wheel:
				if (wheel.ButtonIndex == MouseButton.WheelUp)
					AmvMapGui.ZoomInput = -1;
				else if (wheel.ButtonIndex == MouseButton.WheelDown)
					AmvMapGui.ZoomInput = 1;
				break;
			case InputEventMouseMotion motion:
				AmvMapGui.CameraInput = motion.Relative;
				break;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		PickMapAmv();
	}

	void SelectMapAmv()
	{
		AmvMapGui.SelectedAmv = AmvMapGui.HoveredAmv;
	}
	
	void PickMapAmv()
	{
		var spaceState = GetWorld2D().DirectSpaceState;
		var pos = RedDeadMap.Container.GetGlobalMousePosition();
		var query = new PhysicsPointQueryParameters2D();
		query.Position = pos;
		var result = spaceState.IntersectPoint(query);

		List<AmvMapObject> hits = [];
		
		if (result.Count > 0)
		{
			foreach (var res in result)
			{
				hits.Add(res["collider"].As<Node2D>().GetParent<AmvMapObject>());
			}
			
			var hit = hits.OrderByDescending(x => x.AmvInfo.Scale.LengthSquared())
				.ThenBy(x => x.AmvInfo.Layer).FirstOrDefault(x => x != AmvMapGui.SelectedAmv && x.Visible);
			if (hit != null)
				AmvMapGui.HoveredAmv = hit;
		}
		else
		{
			AmvMapGui.HoveredAmv = null;
		}
		
	}
}

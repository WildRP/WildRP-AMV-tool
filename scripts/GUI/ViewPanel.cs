using Godot;
using System;
using WildRP.AMVTool.Sceneview;

namespace WildRP.AMVTool.GUI;
public partial class ViewPanel : Control
{
	public bool MouseOver { get; private set; }
	public override void _Ready()
	{
		SceneView.RegisterViewPanel(this);

		MouseEntered += () => MouseOver = true;
		MouseExited += () => MouseOver = false;
	}
}

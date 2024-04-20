using Godot;

namespace WildRP.AMVTool.GUI;

public partial class ModelListItem : HBoxContainer
{
	[Export] private Label _itemName;
	[Export] private CheckBox _visibilityCheck;
	[Export] private CheckBox _renderCheck;

	private MeshInstance3D _renderMesh;
	private StaticBody3D _staticBody;
	
	public void Setup(string text, MeshInstance3D mesh, StaticBody3D body)
	{
		_itemName.Text = text;
		_renderMesh = mesh;
		_staticBody = body;
		
		_visibilityCheck.Toggled += on => _renderMesh.Visible = on;
		_renderCheck.Toggled += on => _staticBody.ProcessMode = on ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;

	}

	// Setup for deferred probes
	public void Setup(string text, MeshInstance3D mesh, MeshInstance3D meshInViewport)
	{
		_itemName.Text = text;
		_renderMesh = mesh;
		
		_visibilityCheck.Toggled += on => meshInViewport.Visible = on;
		_renderCheck.Toggled += on => mesh.Visible = on;
	}

	public void Remove()
	{
		QueueFree();
		_renderMesh.QueueFree();
		_staticBody?.QueueFree();
	}
}

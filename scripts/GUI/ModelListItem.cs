using Godot;
using WildRP.AMVTool.BVH;

namespace WildRP.AMVTool.GUI;

public partial class ModelListItem : HBoxContainer
{
	[Export] private Label _itemName;
	[Export] private CheckBox _visibilityCheck;
	[Export] private CheckBox _renderCheck;

	private MeshInstance3D _renderMesh;
	private BoundingVolumeHierarchy _boundingVolumeHierarchy;
	
	public void Setup(string text, MeshInstance3D mesh, BoundingVolumeHierarchy bvh)
	{
		_itemName.Text = text;
		_renderMesh = mesh;
		_boundingVolumeHierarchy = bvh;
		
		_visibilityCheck.Toggled += on => _renderMesh.Visible = on;
		_renderCheck.Toggled += on => _boundingVolumeHierarchy.Enabled = on;

	}

	public void Remove()
	{
		QueueFree();
		_renderMesh.QueueFree();
	}
}

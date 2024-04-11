using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using WildRP.AMVTool.AmvMap;

namespace WildRP.AMVTool.GUI;

public partial class AmvMapGui : PanelContainer
{
	[Export] private Node2D _mapRoot;
	[Export] private PackedScene _mapScene;

	[Export] private Camera2D _mapCamera;
	[Export] private Label _positionLabel;
	[Export] private Label _scaleLabel;
	[Export] private Label _offsetLabel;
	[Export] private Control _selectedAmvPanel;
	[Export] private RichTextLabel _selectedAmvLabel;

	[Export] private CheckBox _interiorCheck;
	[Export] private CheckBox _exteriorCheck;
	[Export] private CheckBox _doorCheck;
	
	public static bool Panning;

	public static Vector2 CameraInput = Vector2.Zero;
	public static float ZoomInput = 0;

	private static RichTextLabel _amvLabel;
	private static Control _amvPanel;
	
	private static AmvMapObject _selectedAmv = null;
	public static AmvMapObject SelectedAmv
	{
		get => _selectedAmv;
		set
		{
			_amvPanel.Visible = value != null;
			_selectedAmv = value;
			UpdateAmvInfoText();
		}
	}
	public static AmvMapObject HoveredAmv = null;
	public static int HighestLayer { get; private set; }

	public static bool InteriorsVisible { get; private set; }
	public static bool ExteriorsVisible { get; private set; }
	public static bool DoorsVisible { get; private set; }
	
	public override void _Ready()
	{
		using var f = FileAccess.Open("res://amvlist.json", FileAccess.ModeFlags.Read);

		var options = new JsonSerializerOptions()
		{
			PropertyNameCaseInsensitive = true
		};
		
		var amvs = JsonSerializer.Deserialize<IList<AmvMapInfo>>(f.GetAsText(), options);
		GD.Print(amvs.Count);
		
		foreach (var amv in amvs)
		{
			var m = _mapScene.Instantiate() as AmvMapObject;
			m.AmvInfo = amv;

			if (amv.Layer > HighestLayer)
				HighestLayer = amv.Layer;
			
			_mapRoot.AddChild(m);
		}
		GD.Print(HighestLayer);

		RedDeadMap.ScaleLabel = _scaleLabel;
		RedDeadMap.OffsetLabel = _offsetLabel;

		_mapCamera.Zoom = Vector2.One * .25f;
		
		_amvLabel = _selectedAmvLabel;
		_amvPanel = _selectedAmvPanel;
	}

	static void UpdateAmvInfoText()
	{
		if (_selectedAmv == null) return;
		var i = _selectedAmv.AmvInfo;
		var source = i.Source.Replace("_", "\\_");
		var text =  
			$"###{source} \n" +
		    $"position: **{i.Position}**\n" +
		    $"scale: **{i.Scale}**\n" +
		    $"rotation: **{i.Rotation}**\n" +
		    $"uuid: **{i.Texture}**\n" +
		    $"layer: **{i.Layer}**\n" +
		    $"interior: **{i.Interior}**\n" +
		    $"exterior: **{i.Exterior}**\n" +
		    $"attachedToDoor: **{i.AttachedToDoor}**";
		
		_amvLabel.Set("markdown_text", text);
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_mapRoot.Visible = Visible;
		_positionLabel.Text = $"(X: {_mapCamera.Position.X}, Y: {-_mapCamera.Position.Y})";

		if (Panning)
			_mapCamera.Position -= CameraInput / _mapCamera.Zoom;
		
		CameraInput = Vector2.Zero;
		
		_mapCamera.Zoom += Vector2.One * (ZoomInput * _mapCamera.Zoom.X) * (float) delta * 3f;
		_mapCamera.Zoom = _mapCamera.Zoom.Clamp(Vector2.One * .25f, Vector2.One * 24);
		ZoomInput = 0;

		DoorsVisible = _doorCheck.ButtonPressed;
		InteriorsVisible = _interiorCheck.ButtonPressed;
		ExteriorsVisible = _exteriorCheck.ButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		
		//var query = 

	}

	public class AmvMapInfo
	{
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Position;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Scale;
		[JsonInclude] public float Rotation;
		[JsonInclude] public ulong Texture;
		[JsonInclude] public string Source;
		[JsonInclude] public int Layer;
		[JsonInclude] public bool Interior;
		[JsonInclude] public bool Exterior;
		[JsonInclude] public bool AttachedToDoor;
	}
}

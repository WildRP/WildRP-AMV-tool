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

	private bool _panning;

	private bool Panning => _panning && Visible;
	
	private Vector2 _cameraInput = Vector2.Zero;
	private float _zoomInput = 0;
	
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
			_mapRoot.AddChild(m);
		}

		RedDeadMap.ScaleLabel = _scaleLabel;
		RedDeadMap.OffsetLabel = _offsetLabel;

		_mapCamera.Zoom = Vector2.One * .25f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_mapRoot.Visible = Visible;
		_positionLabel.Text = $"(X: {_mapCamera.Position.X}, Y: {-_mapCamera.Position.Y})";

		if (Panning)
			_mapCamera.Position -= _cameraInput / _mapCamera.Zoom;
		
		_cameraInput = Vector2.Zero;
		
		_mapCamera.Zoom += Vector2.One * (_zoomInput * _mapCamera.Zoom.X) * (float) delta * 3f;
		_mapCamera.Zoom = _mapCamera.Zoom.Clamp(Vector2.One * .25f, Vector2.One * 8);
		_zoomInput = 0;
	}
		
	public class AmvMapInfo
	{
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Position;
		[JsonInclude]
		public float Rotation;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Scale;
		[JsonInclude]
		public ulong Texture;
		[JsonInclude]
		public string Source;
	}

	public override void _GuiInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseButton {ButtonIndex: MouseButton.Right} btn:
				_panning = btn.Pressed;
				break;
			case InputEventMouseButton {Pressed: true} wheel:
				if (wheel.ButtonIndex == MouseButton.WheelUp)
					_zoomInput = -1;
				else if (wheel.ButtonIndex == MouseButton.WheelDown)
					_zoomInput = 1;
				break;
			case InputEventMouseMotion motion:
				_cameraInput = motion.Relative;
				break;
		}
	}
}

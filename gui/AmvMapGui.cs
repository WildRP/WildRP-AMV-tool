using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WildRP.AMVTool.GUI;

public partial class AmvMapGui : PanelContainer
{
	[Export] private Node2D _mapRoot;
	[Export] private PackedScene _mapScene;
	// Called when the node enters the scene tree for the first time.
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		_mapRoot.Visible = Visible;
	}

	public class AmvMapList
	{
		private IList<AmvMapInfo> Amvs;
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
}

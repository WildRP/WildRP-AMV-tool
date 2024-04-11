using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AsyncResourceLoaderRuntime;

namespace WildRP.AMVTool.AmvMap;
public partial class RedDeadMap : Node2D
{
	[Export] private int _tileSize = 256;
	private List<Sprite2D> _mapTiles;
	private Node2D _container;

	public static Label OffsetLabel, ScaleLabel;
	public override void _Ready()
	{
		_container = new Node2D();
		AddChild(_container);
		
		LoadTiles();
	}
	
	async void LoadTiles()
	{
		var imageFiles = new List<string>();
		const string filePath = "res://maptiles";
		
		using var dir = DirAccess.Open(filePath);
		{
			dir.ListDirBegin();

			while (true)
			{
				var fileName = dir.GetNext();
				if (fileName == "") break;
				
				if (dir.CurrentIsDir() == false && fileName.EndsWith(".webp"))
				{
					imageFiles.Add(fileName);
				}
			}
		}
		if (imageFiles.Count == 0) return; // no image files in this build

		List<Task<Texture2D>> filesToLoad = [];
		
		Texture2D sample = ResourceLoader.Load<Texture2D>(filePath + '/' + imageFiles[0]);
		var size = sample.GetSize();

		var maxPos = Vector2.Zero;
		foreach (var filename in imageFiles)
		{
			var n = filename.TrimSuffix(".webp").Split("_");
			var posX = (1 + n[0].ToInt()) * size.X;
			var posY = (1 + n[1].ToInt()) * size.Y;
			
			if (posX > maxPos.X) maxPos.X = posX;
			if (posY > maxPos.Y) maxPos.Y = posY;
		}

		Vector2 worldBoundsMin = new Vector2(-7680, -8192);
		Vector2 worldBoundsMax = new Vector2(4608, 4096);

		var worldSize = worldBoundsMax - worldBoundsMin;
		worldSize = worldSize.Abs();
		
		// figured these values out by hand
		// these work for the highest res Detailed map but I am sure it could be figured out programmatically
		_container.Scale = Vector2.One * 0.5032283f;
		_container.Position = new Vector2(-3240.2302f, 89.774765f);
		
		foreach (var file in imageFiles)
		{
			var fullPath = filePath + "/" + file;

			filesToLoad.Add(AsyncResourceLoader.LoadResource<Texture2D>(fullPath));
		}

		while (filesToLoad.Any())
		{
			var finished = await Task.WhenAny(filesToLoad);
			filesToLoad.Remove(finished);
			
			var sprite = new Sprite2D();
			sprite.Texture = finished.Result;
			
			Vector2 pos;
			var posString = finished.Result.ResourcePath.GetFile().TrimSuffix(".webp").Split("_");
			pos.X = posString[0].ToInt() * size.X;
			pos.Y = posString[1].ToInt() * size.Y;
			pos += worldBoundsMin;
			_container.AddChild(sprite);
			sprite.Position = pos;
		}
	}
}

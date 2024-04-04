using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

namespace WildRP.AMVTool.GUI;

public partial class HelpGui : PanelContainer
{
	[Export] private ItemList _helpList;
	[Export] private RichTextLabel _markdownLabel;
	
	private readonly List<string> _markdownFiles = [];
	public override void _Ready()
	{
		
		var files = DirAccess.GetFilesAt("res://help");

		foreach (var path in files)
		{
			using var f = FileAccess.Open($"res://help/{path}", FileAccess.ModeFlags.Read);
			_markdownFiles.Add(f.GetAsText());

			_helpList.AddItem(path.TrimSuffix(".md"));
		}

		_markdownFiles.Add(FileAccess.Open("res://README.md", FileAccess.ModeFlags.Read).GetAsText());
		_helpList.AddItem("Read me");

		_markdownLabel.Set("markdown_text", _markdownFiles[0]);
		
		_helpList.ItemSelected += index => _markdownLabel.Set("markdown_text", _markdownFiles[(int)index]);
	}
}

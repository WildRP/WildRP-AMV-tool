using Godot;
using System;
using System.Globalization;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;
public partial class SettingsGui : Control
{
	[ExportSubgroup("UI Scale")]
	[Export] private HSlider _uiScaleSlider;
	[Export] private Label _uiScaleLabel;
	public override void _Ready()
	{

		_uiScaleSlider.ValueChanged += value => { _uiScaleLabel.Text = value.ToString(".0#"); };
		_uiScaleSlider.Value = Settings.UiScale;
		_uiScaleSlider.DragEnded += changed =>
		{
			if (changed) Settings.UiScale = (float)_uiScaleSlider.Value;
		};
	}
}

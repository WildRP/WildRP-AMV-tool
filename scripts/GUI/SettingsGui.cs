using Godot;
using System;
using System.Globalization;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;
public partial class SettingsGui : Control
{
	[ExportGroup("UI")]
	[Export] private HSlider _uiScaleSlider;
	[Export] private Label _uiScaleLabel;
	[ExportGroup("Rendering")]
	[Export] private OptionButton _sampleQualityDropdown;
	public override void _Ready()
	{

		_uiScaleSlider.ValueChanged += value => { _uiScaleLabel.Text = value.ToString(".0#"); };
		_uiScaleSlider.Value = Settings.UiScale;
		_uiScaleSlider.DragEnded += changed =>
		{
			if (changed) Settings.UiScale = (float)_uiScaleSlider.Value;
		};
		
		_sampleQualityDropdown.Select(Settings.SampleCount-5);
		_sampleQualityDropdown.ItemSelected += index => Settings.SampleCount = _sampleQualityDropdown.GetItemId((int)index);
	}
}

using System;
using System.Globalization;
using Godot;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;
public partial class SettingsGui : Control
{
	
	[ExportGroup("UI")]
	[Export] private HSlider _uiScaleSlider;
	[Export] private Label _uiScaleLabel;
	[ExportGroup("Rendering")]
	[Export] private OptionButton _sampleQualityDropdown;
	
	
	[Export] private HSlider _bounceCountSlider;
	[Export] private Label _bounceCountLabel;
	
	[Export] private HSlider _bounceEnergySlider;
	[Export] private Label _bounceEnergyLabel;

	[Export] private OptionButton _textureFormatDropdown;
	
 	public override void _Ready()
	{
		_uiScaleSlider.ValueChanged += value => _uiScaleLabel.Text = value.ToString("0.#");
		_uiScaleSlider.Value = Settings.UiScale;
		_uiScaleSlider.DragEnded += changed =>
		{
			if (changed) Settings.UiScale = (float)_uiScaleSlider.Value;
		};
		
		_sampleQualityDropdown.Select(Settings.SampleCount-7);
		_sampleQualityDropdown.ItemSelected += index => Settings.SampleCount = _sampleQualityDropdown.GetItemId((int)index);

		_bounceCountSlider.ValueChanged += value => _bounceCountLabel.Text = value.ToString("0");
		_bounceCountSlider.DragEnded += changed => Settings.BounceCount = (int) _bounceCountSlider.Value;
		_bounceCountSlider.Value = Settings.BounceCount;
		
		_bounceEnergySlider.ValueChanged += value => _bounceEnergyLabel.Text = value.ToString("##%");
		_bounceEnergySlider.DragEnded += changed => Settings.BounceEnergy = (float) _bounceEnergySlider.Value;
		_bounceEnergySlider.Value = Settings.BounceEnergy;
		
		for (int i = 0; i < _textureFormatDropdown.ItemCount; i++)
		{
			if (_textureFormatDropdown.GetItemText(i) == Enum.GetName(Settings.AmvTextureFormat))
			{
				_textureFormatDropdown.Select(i);
				break;
			}
		}
		
		_textureFormatDropdown.ItemSelected += index =>
		{
			Enum.TryParse(_textureFormatDropdown.GetItemText((int)index), true, out Tex.TextureFormat result);
			Settings.AmvTextureFormat = result;
		};
		
	}
}

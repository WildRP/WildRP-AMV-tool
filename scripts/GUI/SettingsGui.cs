using Godot;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;
public partial class SettingsGui : Control
{
	[Export] private LineEdit _texAssemblePath;
	[Export] private Button _texAssembleBrowse;
	[Export] private FileDialog _texAssembleDialog;
	
	[ExportGroup("UI")]
	[Export] private HSlider _uiScaleSlider;
	[Export] private Label _uiScaleLabel;
	[ExportGroup("Rendering")]
	[Export] private OptionButton _sampleQualityDropdown;
	[Export] private HSlider _minBrightSlider;
	[Export] private Label _minBrightLabel;
	public override void _Ready()
	{
		_texAssembleDialog.UseNativeDialog = true;
		
		_uiScaleSlider.ValueChanged += value => { _uiScaleLabel.Text = value.ToString(".0#"); };
		_uiScaleSlider.Value = Settings.UiScale;
		_uiScaleSlider.DragEnded += changed =>
		{
			if (changed) Settings.UiScale = (float)_uiScaleSlider.Value;
		};
		
		_sampleQualityDropdown.Select(Settings.SampleCount-7);
		_sampleQualityDropdown.ItemSelected += index => Settings.SampleCount = _sampleQualityDropdown.GetItemId((int)index);

		_texAssembleBrowse.Pressed += () => _texAssembleDialog.Popup();
		_texAssembleDialog.FileSelected += path =>
		{
			_texAssemblePath.Text = path;
			Settings.TexAssembleLocation = path;
		};

		_texAssemblePath.TextSubmitted += text => Settings.TexAssembleLocation = text;
		
		_minBrightSlider.ValueChanged += value => { _minBrightLabel.Text = value.ToString(".000#"); };
		_minBrightSlider.Value = Settings.MinBrightness;
		_minBrightSlider.DragEnded += changed =>
		{
			if (changed) Settings.MinBrightness = (float)_minBrightSlider.Value;
			GD.Print($"Set Min Brighness to {_minBrightSlider.Value}");
		};
	}
}

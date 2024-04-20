using Godot;
using System;

namespace WildRP.AMVTool.GUI;

public partial class RenamePopup : PopupPanel
{
	private Volume _targetVolume;

	[Export] private LineEdit _textField;

	[Export] private Button _okButton;

	[Export] private Button _cancelButton;

	public static RenamePopup Instance;
	
	public override void _Ready()
	{
		if (Instance == null)
			Instance = this;
		else
		{
			QueueFree();
			return;
		}
		
		_okButton.Pressed += () =>
		{
			if (_targetVolume == null) return;
			_targetVolume.RenameVolume(_textField.Text);
			Hide();
		};

		_cancelButton.Pressed += () =>
		{
			_targetVolume = null;
			Hide();
		};
	}
	
	public override void _Process(double delta)
	{
		_okButton.Disabled = _textField.Text.Length < 3;
	}

	public void Trigger(Volume v)
	{
		_targetVolume = v;
		PopupCentered();
		GD.Print($"Rename popup triggered with target {v.GuiListName}");
	}
}

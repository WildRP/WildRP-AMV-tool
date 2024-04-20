using Godot;
using System;

public partial class ErrorPopup : PopupPanel
{
	[Export] private Label _label;
	[Export] private Button _closeBtn;

	public static ErrorPopup Instance;
	
	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			QueueFree();
			return;
		}
		
		_closeBtn.Pressed += Hide;
	}

	public void Trigger(string error)
	{
		PopupCentered();
		_label.Text = error;
	}
}

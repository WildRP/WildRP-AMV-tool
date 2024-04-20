using Godot;
using System;

namespace WildRP.AMVTool.GUI;

public partial class ProjectSettingsGui : PanelContainer
{
	[Export] private LineEdit _ymapNameField;
	[Export] private LineEdit _interiorNameField;

	[Export] private SpinBox _ymapPosX;
	[Export] private SpinBox _ymapPosY;
	[Export] private SpinBox _ymapPosZ;

	[Export] private Button _saveProjectButton;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SaveManager.ProjectLoaded += GetValuesFromProject;

		_ymapPosX.ValueChanged += (f) => UpdateYmapPosition();
		_ymapPosY.ValueChanged += (f) => UpdateYmapPosition();
		_ymapPosZ.ValueChanged += (f) => UpdateYmapPosition();

		_ymapNameField.TextChanged += SaveManager.UpdateYmapName;
		_interiorNameField.TextChanged += SaveManager.UpdateInteriorName;

		_saveProjectButton.Pressed += () => SaveManager.SaveProject();
	}

	void GetValuesFromProject(SaveManager.Project project)
	{
		_ymapNameField.Text = project.YMapName;
		_interiorNameField.Text = project.InteriorName;

		var pos = project.YMapPosition;
		_ymapPosX.SetValueNoSignal(pos.X);
		_ymapPosY.SetValueNoSignal(pos.Y);
		_ymapPosZ.SetValueNoSignal(pos.Z);

		_ymapPosX.GetLineEdit().Text = _ymapPosX.Value.ToString();
		_ymapPosY.GetLineEdit().Text = _ymapPosY.Value.ToString();
		_ymapPosZ.GetLineEdit().Text = _ymapPosZ.Value.ToString();
	}

	public void UpdateYmapPosition()
	{
		var pos = Vector3.Zero;
		pos.X = (float) _ymapPosX.Value;
		pos.Y = (float) _ymapPosY.Value;
		pos.Z = (float) _ymapPosZ.Value;
		SaveManager.UpdateYmapPosition(pos);
	}
}

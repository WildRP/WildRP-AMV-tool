using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace WildRP.AMVTool.GUI;

public partial class AmvBakerGui : Control
{
	[ExportGroup("Save & Load")]
		[Export] private Button _saveProjectBtn;
		[Export] private Button _loadProjectBtn;
		[Export] private Control _projectPanel;
		[Export] private Button _projectFolderBtn;
			
	[ExportGroup("Model loading")]
		[Export] private PackedScene _modelListItem;
		[ExportSubgroup("UI Elements")]
			[Export] private Button _loadModelButton;
			[Export] private Label _modelNameLabel;
			[Export] private FileDialog _fileDialog;
			[Export] private VBoxContainer _modelListContainer;
			
	[ExportGroup("AMV")]
		[Export] private PackedScene _amvScene;
		[Export] private Node3D _amvContainerNode;

		[ExportSubgroup("UI Elements")]
			[Export] private Control _controlToHide;
			[Export] private AmvList _amvList;
			[Export] private AMVListContextMenu _amvListContextMenu;
			[Export] private Button _newAmvButton;
			[Export] private Button _bakeAllButton;
			[Export] private Button _exportTexturesBtn;
			[Export] private ProgressBar _bakeProgressBar;
			[Export] private Button _cancelBakeBtn;
		[ExportGroup("AMV Details")] 
			[Export] private Control _amvInfoPanel;
			[Export] private SpinBox _textureName;
			[Export] private SpinBox _rotation;
			[Export] private Button _randomizeTextureNameButton;
			[ExportSubgroup("YMAP Position")]
				[Export] private SpinBox _ymapPositionX;
				[Export] private SpinBox _ymapPositionY;
				[Export] private SpinBox _ymapPositionZ;
			[ExportSubgroup("Position")]
				[Export] private SpinBox _positionX;
				[Export] private SpinBox _positionY;
				[Export] private SpinBox _positionZ;
			[ExportSubgroup("Size")]
				[Export] private SpinBox _sizeX;
				[Export] private SpinBox _sizeY;
				[Export] private SpinBox _sizeZ;
			[ExportSubgroup("Size")]
				[Export] private SpinBox _probesX;
				[Export] private SpinBox _probesY;
				[Export] private SpinBox _probesZ;
	
	private readonly List<ModelListItem> _modelListItems = [];
	
	public static AmbientMaskVolume SelectedAmv
	{
		get;
		private set;
	}
	
	public override void _Ready()
	{
		SaveManager.ProjectLoaded += LoadProject;
		
		// File dialog
		_fileDialog.AddFilter("*.gltf, *.glb; GLTF Model File");
		_fileDialog.Title = "Load Model...";

		_fileDialog.FileSelected += LoadModel;
		
		_loadModelButton.Pressed += () =>
		{
			_fileDialog.Popup();
		};
		
		_newAmvButton.Pressed += CreateNewAmv;

		// AMV Select and deselect
		_amvList.ItemSelected += index => SelectAmv(AmvBaker.Instance.GetVolume(_amvList.GetItemText((int)index)));
		_amvList.EmptyClicked += (position, index) =>
		{
			SelectAmv(null);
			_amvList.DeselectAll();
		};
		
		// AMV List Context Menu
		_amvList.OnRightClickItem += (name, pos) =>
		{
			_amvListContextMenu.Popup();
			_amvListContextMenu.Position = new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
			_amvListContextMenu.Select(name);
		};

		_bakeAllButton.Pressed += () =>
		{
			AmvBaker.Instance.BakeAll();
			_controlToHide.Visible = false;
			_bakeProgressBar.GetParentControl().Visible = true;
		};
		AmvBaker.Instance.UpdatePercentage += f => _bakeProgressBar.Value = f;
		AmvBaker.Instance.BakeFinished += () =>
		{
			_controlToHide.Visible = true;
			_bakeProgressBar.GetParentControl().Visible = false;
		};
		
		ConnectAmvGui();
		_amvInfoPanel.Visible = false;

		_saveProjectBtn.Pressed += () =>
		{
			AmvBaker.Instance.UpdateProjectAmvs();
			SaveManager.SaveProject();
		};

		_loadProjectBtn.Pressed += () => _projectPanel.Visible = true;
		
		_projectFolderBtn.Pressed += () => OS.ShellOpen(SaveManager.GetGlobalizedProjectPath());

		_exportTexturesBtn.Pressed += () =>
		{
			AmvBaker.Instance.GenerateTextures();
		};

		_cancelBakeBtn.Pressed += () => AmvBaker.Instance.CancelBake();
	}

	private void UnloadModel()
	{
		// Clear out the list from whatever model we had loaded before
		foreach (var item in _modelListItems)
		{
			item.Remove();
		}
		
		SaveManager.SetModel("");
	}
	
	private void LoadModel(string path)
	{
		var (e, result) = AmvBaker.Instance.LoadModel(path);
		
		UnloadModel();
		
		if (e != Error.Ok) return; // display an error message here probably
		
		SaveManager.SetModel(path);
		
		_modelNameLabel.Text = path.GetFile();
		
		foreach (var t in result)
		{
			if (_modelListItem.Instantiate() is not ModelListItem item) continue;
			
			item.Setup(t.Item1.Name, t.Item1, t.Item2);
			_modelListContainer.AddChild(item);
			_modelListItems.Add(item);
		}
	}
	
	private void CreateNewAmv()
	{
		var amv = _amvScene.Instantiate() as AmbientMaskVolume;
		string name = EnsureUniqueName($"AMV {_amvList.ItemCount+1}");
		
		amv.Setup(name);
		amv.TextureName = (ulong) _textureName.MinValue;
		_amvContainerNode.AddChild(amv);
		
		AmvBaker.Instance.RegisterAmv(amv);
		SaveManager.UpdateAmv(amv.GuiListName, amv.Save());

		amv.Deleted += OnDeleteAmv;
		
		var item =_amvList.AddItem(name);
		_amvList.Select(item);
		SelectAmv(amv);
	}

	private void LoadProject(SaveManager.Project project)
	{
		var path = project.ModelPath;
		if (path.Length > 0 && (path.EndsWith(".glb") || path.EndsWith(".gltf")))
			LoadModel(project.ModelPath);
		else
			UnloadModel(); // Project doesn't have a valid model file
		
		foreach (var data in project.Volumes)
		{
			var amv = _amvScene.Instantiate() as AmbientMaskVolume;
			amv.Load(data);
			
			var item =_amvList.AddItem(data.Key);
			
			_amvContainerNode.AddChild(amv);
			AmvBaker.Instance.RegisterAmv(amv);
			amv.Deleted += OnDeleteAmv;
		}
	}

	private void OnDeleteAmv(AmbientMaskVolume volume)
	{
		for (int i = 0; i < _amvList.ItemCount; i++)
		{
			if (_amvList.GetItemText(i) != volume.GuiListName) continue;
			_amvList.RemoveItem(i);
			break;
		}
		if (SelectedAmv == volume) SelectAmv(null);
	}

	private string EnsureUniqueName(string name)
	{
		var n = name;
		var i = 1;
		while (IsNameUnique(n) == false)
		{
			n = $"{name} ({i})";
			i++;
		}
		return n;
	}
	
	bool IsNameUnique(string name)
	{
		for (int i = 0; i < _amvList.ItemCount; i++)
		{
			if (_amvList.GetItemText(i) == name) return false;
		}

		return true;
	}

	public void SelectAmv(AmbientMaskVolume volume)
	{
		if (SelectedAmv != null)
			SelectedAmv.SizeChanged -= UpdateAmvGuiValues;

		_amvInfoPanel.Visible = volume != null;
		
		SelectedAmv = volume;
		if (SelectedAmv != null)
			SelectedAmv.SizeChanged += UpdateAmvGuiValues;
		UpdateAmvGuiValues();
	}

	
	
	private void UpdateAmvGuiValues()
	{
		// v for valid
		bool v = SelectedAmv != null;

		_textureName.GetLineEdit().Text = v ? SelectedAmv.TextureName.ToString(CultureInfo.InvariantCulture) : _textureName.MinValue.ToString(CultureInfo.InvariantCulture);
		_rotation.GetLineEdit().Text = v ? (-SelectedAmv.RotationDegrees.Y).ToString(CultureInfo.InvariantCulture) : "0";
		
		// Note that we swap Z and Y here to present RDR2-style coordinates to the end user
		// Z also gets inverted
		_positionX.SetValueNoSignal(v ? SelectedAmv.Position.X : 0);
		_positionY.SetValueNoSignal(v ? -SelectedAmv.Position.Z : 0);
		_positionZ.SetValueNoSignal(v ? SelectedAmv.Position.Y : 0);
		
		_sizeX.SetValueNoSignal(v ? SelectedAmv.Size.X : 0);
		_sizeY.SetValueNoSignal(v ? SelectedAmv.Size.Z : 0);
		_sizeZ.SetValueNoSignal(v ? SelectedAmv.Size.Y : 0);
		
		_probesX.SetValueNoSignal(v ? SelectedAmv.ProbeCount.X : 0);
		_probesY.SetValueNoSignal(v ? SelectedAmv.ProbeCount.Z : 0);
		_probesZ.SetValueNoSignal(v ? SelectedAmv.ProbeCount.Y : 0);
	}

	private void ConnectAmvGui()
	{
		_ymapPositionX.ValueChanged += value => SelectedAmv?.SetYmapPositionX(value);
		_ymapPositionY.ValueChanged += value => SelectedAmv?.SetYmapPositionZ(value);
		_ymapPositionZ.ValueChanged += value => SelectedAmv?.SetYmapPositionY(value);
		
		_positionX.ValueChanged += value => SelectedAmv?.SetPositionX(value);
		_positionY.ValueChanged += value => SelectedAmv?.SetPositionZ(value);
		_positionZ.ValueChanged += value => SelectedAmv?.SetPositionY(value);
		
		_sizeX.ValueChanged += value => SelectedAmv?.SetSizeX(value);
		_sizeY.ValueChanged += value => SelectedAmv?.SetSizeZ(value);
		_sizeZ.ValueChanged += value => SelectedAmv?.SetSizeY(value);
		
		_probesX.ValueChanged += value => SelectedAmv?.SetProbesX(value);
		_probesY.ValueChanged += value => SelectedAmv?.SetProbesY(value);
		_probesZ.ValueChanged += value => SelectedAmv?.SetProbesZ(value);

		_rotation.ValueChanged += value => SelectedAmv?.SetRotation(value);

		_textureName.ValueChanged += value => SelectedAmv.TextureName = Convert.ToUInt64(Math.Round(value));

		_randomizeTextureNameButton.Pressed += () =>
		{
			_textureName.Value = Random.Shared.NextInt64(Convert.ToInt64(_textureName.MinValue), Convert.ToInt64(_textureName.MaxValue));
		};
	}
	
}

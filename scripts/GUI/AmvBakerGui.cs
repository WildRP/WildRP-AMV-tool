using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using WildRP.AMVTool.Autoloads;

namespace WildRP.AMVTool.GUI;

public partial class AmvBakerGui : Control
{
	[Export] private Node3D _volumeModelContainer;
	
	[ExportGroup("Save & Load")]
		[Export] private Button _saveProjectBtn;
		[Export] private Button _loadProjectBtn;
		[Export] private Control _projectPanel;
			
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

		[ExportSubgroup("Blur")]
		[Export] private OptionButton _blurSizeDropdown;
		[Export] private HSlider _blurStrengthSlider;
		
		[ExportSubgroup("UI Elements")]
			[Export] private Control _controlToHide;
			[Export] private VolumeList _volumeList;
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
			[ExportSubgroup("Position")]
				[Export] private SpinBox _positionX;
				[Export] private SpinBox _positionY;
				[Export] private SpinBox _positionZ;
			[ExportSubgroup("Size")]
				[Export] private SpinBox _sizeX;
				[Export] private SpinBox _sizeY;
				[Export] private SpinBox _sizeZ;
			[ExportSubgroup("Probe Count")]
				[Export] private SpinBox _probesX;
				[Export] private SpinBox _probesY;
				[Export] private SpinBox _probesZ;
			
	
	private readonly List<ModelListItem> _modelListItems = [];

	public static event Action<bool> GuiToggled;
	public static bool GuiVisible { get; private set; }
	
	public static AmbientMaskVolume SelectedAmv
	{
		get;
		private set;
	}

	public override void _Process(double delta)
	{
		GuiVisible = Visible;
		
		if (Visible && Input.IsActionJustReleased("ui_cancel")) SelectAmv(null);
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
		_volumeList.ItemSelected += index => SelectAmv(AmvBaker.Instance.GetVolume(_volumeList.GetItemText((int)index)));
		_volumeList.EmptyClicked += (position, index) =>
		{
			SelectAmv(null);
			_volumeList.DeselectAll();
		};
		
		// AMV List Context Menu
		_volumeList.OnRightClickItem += (name, pos) =>
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
		
		_amvInfoPanel.Visible = false;

		_saveProjectBtn.Pressed += SaveManager.SaveProject;

		_loadProjectBtn.Pressed += () => _projectPanel.Visible = true;

		_exportTexturesBtn.Pressed += () =>
		{
			AmvBaker.Instance.GenerateTextures();
		};

		_cancelBakeBtn.Pressed += () => AmvBaker.Instance.CancelBake();
		
		_randomizeTextureNameButton.Pressed += () =>
		{
			_textureName.Value = Random.Shared.NextInt64(Convert.ToInt64(_textureName.MinValue), Convert.ToInt64(_textureName.MaxValue));
		};

		VisibilityChanged += () =>
		{
			GuiToggled?.Invoke(Visible);
			_volumeModelContainer.Visible = Visible;
		};
		
		_blurStrengthSlider.Value = Settings.BlurStrength;
		_blurSizeDropdown.Select(_blurSizeDropdown.GetItemIndex(Settings.BlurSize));

		_blurStrengthSlider.ValueChanged += value =>
		{
			if (Engine.GetProcessFrames() % 2 != 0) return;
			Settings.BlurStrength = (float)value;
			UpdateBlur();
		};
		_blurStrengthSlider.DragEnded += changed =>
		{
			Settings.BlurStrength = (float)_blurStrengthSlider.Value;
			UpdateBlur();
		};
		_blurSizeDropdown.ItemSelected += index =>
		{
			Settings.BlurSize = _blurSizeDropdown.GetItemId((int)index);
			UpdateBlur();
		};

		
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
		string name = EnsureUniqueName($"AMV {_volumeList.ItemCount+1}");
		
		amv.Setup(name);
		amv.TextureName = (ulong) _textureName.MinValue;
		_amvContainerNode.AddChild(amv);
		
		AmvBaker.Instance.RegisterAmv(amv);
		SaveManager.UpdateAmv(amv.GuiListName, amv.Save());

		amv.Deleted += OnDeleteAmv;
		amv.VolumeRenamed += RenameVolume;
		
		var item =_volumeList.AddItem(name);
		_volumeList.Select(item);
		SelectAmv(amv);
	}

	private void LoadProject(SaveManager.Project project)
	{
		Reset();
		
		var path = project.ModelPath;
		if (path.Length > 0 && (path.EndsWith(".glb") || path.EndsWith(".gltf")))
			LoadModel(project.ModelPath);
		
		foreach (var data in project.Volumes)
		{
			var amv = _amvScene.Instantiate() as AmbientMaskVolume;
			amv.Load(data);
			
			var item =_volumeList.AddItem(data.Key);
			
			_amvContainerNode.AddChild(amv);
			AmvBaker.Instance.RegisterAmv(amv);
			amv.Deleted += OnDeleteAmv;
			amv.VolumeRenamed += RenameVolume;
		}
	}

	private void OnDeleteAmv(Volume volume)
	{
		for (int i = 0; i < _volumeList.ItemCount; i++)
		{
			if (_volumeList.GetItemText(i) != volume.GuiListName) continue;
			_volumeList.RemoveItem(i);
			break;
		}
		if (SelectedAmv == volume) SelectAmv(null);
	}

	private void Reset()
	{
		_volumeList.Clear();
		AmvBaker.Instance.Clear();
		UnloadModel();
	}
	
	private void RenameVolume(string from, string to)
	{
		var uniqueName = EnsureUniqueName(to);
		_volumeList.SetItemText(_volumeList.GetIndexByName(from), uniqueName);
		AmvBaker.Instance.RenameVolume(from, uniqueName);
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
		for (int i = 0; i < _volumeList.ItemCount; i++)
		{
			if (_volumeList.GetItemText(i) == name) return false;
		}

		return true;
	}

	// If you do this while you have a field selected the new AMV will get the value from that field...
	private void SelectAmv(AmbientMaskVolume volume)
	{
		if (SelectedAmv != null)
		{
			SelectedAmv.SizeChanged -= UpdateAmvGuiValues;
			DisconnectAmvGui();
		}

		_amvInfoPanel.Visible = volume != null;
		
		SelectedAmv = volume;
		if (SelectedAmv != null)
		{
			SelectedAmv.SizeChanged += UpdateAmvGuiValues;
			ConnectAmvGui();
		}
		else
		{
			_volumeList.DeselectAll();
		}
		UpdateAmvGuiValues();
	}

	private void UpdateBlur()
	{
		AmvBaker.Instance.UpdateBlur();
	}
	
	private void UpdateAmvGuiValues()
	{
		// v for valid
		bool v = SelectedAmv != null;

		_textureName.GetLineEdit().Text = v ? SelectedAmv.TextureName.ToString(CultureInfo.InvariantCulture) : _textureName.MinValue.ToString(CultureInfo.InvariantCulture);
		_rotation.SetValueNoSignal(v ? SelectedAmv.RotationDegrees.Y : 0);
		_rotation.GetLineEdit().Text = v ? (-SelectedAmv.RotationDegrees.Y).ToString(CultureInfo.InvariantCulture) : "0";
		
		// Note that we swap Z and Y here to present RDR2-style coordinates to the end user
		// Z also gets inverted
		_positionX.SetValueNoSignal(v ? SelectedAmv.Position.X : 0);
        _positionX.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.Position.X) : "0" ;
		_positionY.SetValueNoSignal(v ? -SelectedAmv.Position.Z : 0);
        _positionY.GetLineEdit().Text = v ? Convert.ToString(-SelectedAmv.Position.Z) : "0" ;
		_positionZ.SetValueNoSignal(v ? SelectedAmv.Position.Y : 0);
        _positionZ.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.Position.Y) : "0" ;
		
		_sizeX.SetValueNoSignal(v ? SelectedAmv.Size.X : 0);
        _sizeX.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.Size.X) : "0" ;
		_sizeY.SetValueNoSignal(v ? SelectedAmv.Size.Z : 0);
        _sizeY.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.Size.Z) : "0" ;
		_sizeZ.SetValueNoSignal(v ? SelectedAmv.Size.Y : 0);
        _sizeZ.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.Size.Y) : "0" ;
		
		_probesX.SetValueNoSignal(v ? SelectedAmv.ProbeCount.X : 0);
        _probesX.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.ProbeCount.X) : "0" ;
		_probesY.SetValueNoSignal(v ? SelectedAmv.ProbeCount.Z : 0);
        _probesY.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.ProbeCount.Z) : "0" ;
		_probesZ.SetValueNoSignal(v ? SelectedAmv.ProbeCount.Y : 0);
        _probesZ.GetLineEdit().Text = v ? Convert.ToString(SelectedAmv.ProbeCount.Y) : "0" ;
	}

	private void ConnectAmvGui()
	{
		_positionX.ValueChanged += SelectedAmv.SetPositionX;
		_positionY.ValueChanged += SelectedAmv.SetPositionZ;
		_positionZ.ValueChanged += SelectedAmv.SetPositionY;
		
		_sizeX.ValueChanged += SelectedAmv.SetSizeX;
		_sizeY.ValueChanged += SelectedAmv.SetSizeZ;
		_sizeZ.ValueChanged += SelectedAmv.SetSizeY;
		
		_probesX.ValueChanged += SelectedAmv.SetProbesX;
		_probesY.ValueChanged += SelectedAmv.SetProbesY;
		_probesZ.ValueChanged += SelectedAmv.SetProbesZ;

		_rotation.ValueChanged += SelectedAmv.SetRotation;

		_textureName.ValueChanged += SetTextureName;
	}
	
	private void SetTextureName(double value) => SelectedAmv.TextureName = Convert.ToUInt64(Math.Round(value));
	
	private void DisconnectAmvGui()
	{
		
		_positionX.ValueChanged -= SelectedAmv.SetPositionX;
		_positionY.ValueChanged -= SelectedAmv.SetPositionZ;
		_positionZ.ValueChanged -= SelectedAmv.SetPositionY;
		
		_positionX.ReleaseFocus();
        _positionX.GetLineEdit().ReleaseFocus();
		_positionY.ReleaseFocus();
        _positionY.GetLineEdit().ReleaseFocus();
		_positionZ.ReleaseFocus();
        _positionZ.GetLineEdit().ReleaseFocus();
		
		_sizeX.ValueChanged -= SelectedAmv.SetSizeX;
		_sizeY.ValueChanged -= SelectedAmv.SetSizeZ;
		_sizeZ.ValueChanged -= SelectedAmv.SetSizeY;
		
		_sizeX.ReleaseFocus();
        _sizeX.GetLineEdit().ReleaseFocus();
		_sizeY.ReleaseFocus();
        _sizeY.GetLineEdit().ReleaseFocus();
		_sizeZ.ReleaseFocus();
        _sizeZ.GetLineEdit().ReleaseFocus();
		
		_probesX.ValueChanged -= SelectedAmv.SetProbesX;
		_probesY.ValueChanged -= SelectedAmv.SetProbesY;
		_probesZ.ValueChanged -= SelectedAmv.SetProbesZ;
		
		_probesX.ReleaseFocus();
        _probesX.GetLineEdit().ReleaseFocus();
		_probesY.ReleaseFocus();
        _probesY.GetLineEdit().ReleaseFocus();
		_probesZ.ReleaseFocus();
        _probesZ.GetLineEdit().ReleaseFocus();

		_rotation.ValueChanged -= SelectedAmv.SetRotation;
		
		_rotation.ReleaseFocus();
		_rotation.GetLineEdit().ReleaseFocus();

		_textureName.ValueChanged -= SetTextureName;
		
		_textureName.ReleaseFocus();
		_textureName.GetLineEdit().ReleaseFocus();
	}
	
}

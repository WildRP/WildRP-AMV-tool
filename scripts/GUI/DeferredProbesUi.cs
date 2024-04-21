using Godot;
using System;
using System.Collections.Generic;

namespace WildRP.AMVTool.GUI;

public partial class DeferredProbesUi : Control
{
	[Export] private Node3D _probeModelContainer;
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
			
		[ExportGroup("Deferred Probes")]
			[Export] private PackedScene _deferredProbeScene;
			[Export] private Node _probeContainerNode;

			[ExportSubgroup("UI Elements")]
				[Export] private Control _controlToHide;
				[Export] private VolumeList _volumeList;
				[Export] private Button _newProbeButton;
				[Export] private Button _bakeAllButton;
				[Export] private ProgressBar _bakeProgressBar;
				[Export] private Button _cancelBakeBtn;
		[ExportGroup("Probe Details")] 
			[Export] private Control _probeInfoPanel;
			[Export] private LineEdit _guid;
			[Export] private SpinBox _rotation;
			[Export] private Button _randomizeUuidButton;
			[Export] private ProbeContextMenu _probeContextMenu;
				[ExportSubgroup("Center Offset")]
					[Export] private SpinBox _centerOffsetX;
					[Export] private SpinBox _centerOffsetY;
					[Export] private SpinBox _centerOffsetZ;
				[ExportSubgroup("Size")]
					[Export] private SpinBox _sizeX;
					[Export] private SpinBox _sizeY;
					[Export] private SpinBox _sizeZ;
				[ExportSubgroup("Influence Extents")]
				[Export] private SpinBox _extentsX;
				[Export] private SpinBox _extentsY;
				[Export] private SpinBox _extentsZ;
			
	private readonly List<ModelListItem> _modelListItems = [];
	
	public static event Action<bool> GuiToggled;
	public static bool GuiVisible { get; private set; }

	public static DeferredProbe SelectedProbe
	{
		get;
		private set;
	}
	
	// Called when the node enters the scene tree for the first time.
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

		_newProbeButton.Pressed += CreateNewProbe;
		
		// Probe Select and deselect
		_volumeList.ItemSelected += index => SelectProbe(DeferredProbeBaker.Instance.GetProbe(_volumeList.GetItemText((int)index)));
		_volumeList.EmptyClicked += (position, index) =>
		{
			SelectProbe(null);
			_volumeList.DeselectAll();
		};
		_volumeList.OnRightClickItem += (name, pos) => 
		{
			_probeContextMenu.Popup();
			_probeContextMenu.Position = new Vector2I(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
			_probeContextMenu.Select(name);
		};

		_saveProjectBtn.Pressed += SaveManager.SaveProject;

		_loadProjectBtn.Pressed += () => _projectPanel.Visible = true;
		
		VisibilityChanged += () =>
		{
			GuiToggled?.Invoke(Visible);
			_probeModelContainer.Visible = Visible;
		};

		_bakeAllButton.Pressed += () =>
		{
			DeferredProbeBaker.Instance.BakeAll();
			_controlToHide.Visible = false;
			_bakeProgressBar.GetParentControl().Visible = true;
		};

		DeferredProbeBaker.UpdateBakeProgress += UpdateBakeProgress;
		
		_randomizeUuidButton.Pressed += () =>
		{
			if (SelectedProbe == null) return; // shouldnt happen but still

			var r1 = GD.Randi();
			var r2 = GD.Randi();
			SelectedProbe.Guid = r1 | (ulong) r2 << 32;
			_guid.Text = "0x"+SelectedProbe.Guid.ToString("x16");
		};

		_probeInfoPanel.Visible = false;
	}

	private void Reset()
	{
		_volumeList.Clear();
		DeferredProbeBaker.Instance.Clear();
		UnloadModel();
	}

	void UpdateBakeProgress(float progress)
	{
		_bakeProgressBar.Value = progress;
		
		if (progress < 1)
			return;
		
		_controlToHide.Visible = true;
		_bakeProgressBar.GetParentControl().Visible = false;
	}
	
	private void UnloadModel()
	{
		// Clear out the list from whatever model we had loaded before
		foreach (var item in _modelListItems)
		{
			item.Remove();
		}
		
		SaveManager.SetProbeModel("");
	}
	
	private void LoadModel(string path)
	{
		var result = DeferredProbeBaker.Instance.LoadModel(path);
		
		UnloadModel();
		
		if (result == null) return; // display an error message here probably
		
		SaveManager.SetProbeModel(path);
		GD.Print($"Loaded model for Reflection Probes: {path}");
		
		_modelNameLabel.Text = path.GetFile();
		
		foreach (var t in result)
		{
			if (_modelListItem.Instantiate() is not ModelListItem item) continue;
			
			item.Setup(t.Item1.Name, t.Item1, t.Item2);
			_modelListContainer.AddChild(item);
			_modelListItems.Add(item);
		}
	}
	
	private void LoadProject(SaveManager.Project project)
	{
		Reset();
		var path = project.ReflectionModelPath;
		if (path.Length > 0 && (path.EndsWith(".glb") || path.EndsWith(".gltf")))
			LoadModel(project.ReflectionModelPath);
		
		foreach (var data in project.Probes)
		{
			var probe = _deferredProbeScene.Instantiate() as DeferredProbe;
			probe.Load(data);
			
			var item =_volumeList.AddItem(data.Key);
			
			_probeContainerNode.AddChild(probe);
			DeferredProbeBaker.Instance.RegisterProbe(probe);
			probe.Deleted += OnDeleteProbe;
			probe.VolumeRenamed += RenameProbe;
		}
	}
	
	private void OnDeleteProbe(Volume volume)
	{
		for (int i = 0; i < _volumeList.ItemCount; i++)
		{
			if (_volumeList.GetItemText(i) != volume.GuiListName) continue;
			_volumeList.RemoveItem(i);
			break;
		}
		if (SelectedProbe == volume) SelectProbe(null);
	}
	
	private void CreateNewProbe()
	{
		var probe = _deferredProbeScene.Instantiate() as DeferredProbe;
		string name = EnsureUniqueName($"Probe {_volumeList.ItemCount+1}");
		
		probe.Setup(name);
		probe.Guid = 0;
		_probeContainerNode.AddChild(probe);
		
		DeferredProbeBaker.Instance.RegisterProbe(probe);
		SaveManager.UpdateDeferredProbe(probe.GuiListName, probe.Save());

		probe.Deleted += OnDeleteProbe;
		probe.VolumeRenamed += RenameProbe;

		var item =_volumeList.AddItem(name);
		_volumeList.Select(item);
		SelectProbe(probe);
	}

	private void RenameProbe(string from, string to)
	{
		var uniqueName = EnsureUniqueName(to);
		_volumeList.SetItemText(_volumeList.GetIndexByName(from), uniqueName);
		DeferredProbeBaker.Instance.RenameProbe(from, uniqueName);
	}
	
	private void SelectProbe(DeferredProbe p)
	{
		if (SelectedProbe != null)
			DisconnectProbeGui();

		SelectedProbe = p;
		_probeInfoPanel.Visible = p != null;
		
		if (p != null)
		{
			UpdateProbeGuiValues();
			ConnectProbeGui();
		}
		else
		{
			_volumeList.DeselectAll();
		}
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
	
	public override void _Process(double delta)
	{
		GuiVisible = Visible;
		
		if (Visible && Input.IsActionJustReleased("ui_cancel")) SelectProbe(null);
	}

	private void UpdateProbeGuiValues()
	{
		bool v = SelectedProbe != null;

		_guid.Text = v ? "0x"+SelectedProbe.Guid.ToString("x16") : "";
		_rotation.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.RotationDegrees.Y) : "0";
		
		_centerOffsetX.SetValueNoSignal(v ? SelectedProbe.CenterOffset.X : 0);
		_centerOffsetX.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.CenterOffset.X) : "0" ;
		_centerOffsetY.SetValueNoSignal(v ? -SelectedProbe.CenterOffset.Z : 0);
		_centerOffsetY.GetLineEdit().Text = v ? Convert.ToString(-SelectedProbe.CenterOffset.Z) : "0" ;
		_centerOffsetZ.SetValueNoSignal(v ? SelectedProbe.CenterOffset.Y : 0);
		_centerOffsetZ.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.CenterOffset.Y) : "0" ;
		
		_sizeX.SetValueNoSignal(v ? SelectedProbe.Size.X : 0);
		_sizeX.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.Size.X) : "0" ;
		_sizeY.SetValueNoSignal(v ? SelectedProbe.Size.Z : 0);
		_sizeY.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.Size.Z) : "0" ;
		_sizeZ.SetValueNoSignal(v ? SelectedProbe.Size.Y : 0);
		_sizeZ.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.Size.Y) : "0" ;
		
		_extentsX.SetValueNoSignal(v ? SelectedProbe.InfluenceExtents.X : 0);
		_extentsX.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.InfluenceExtents.X) : "0" ;
		_extentsY.SetValueNoSignal(v ? SelectedProbe.InfluenceExtents.Z : 0);
		_extentsY.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.InfluenceExtents.Z) : "0" ;
		_extentsZ.SetValueNoSignal(v ? SelectedProbe.InfluenceExtents.Y : 0);
		_extentsZ.GetLineEdit().Text = v ? Convert.ToString(SelectedProbe.InfluenceExtents.Y) : "0" ;
	}

	private void ConnectProbeGui()
	{
		_centerOffsetX.ValueChanged += SelectedProbe.SetCenterOffsetX;
		_centerOffsetY.ValueChanged += SelectedProbe.SetCenterOffsetZ;
		_centerOffsetZ.ValueChanged += SelectedProbe.SetCenterOffsetY;
		
		_sizeX.ValueChanged += SelectedProbe.SetSizeX;
		_sizeY.ValueChanged += SelectedProbe.SetSizeZ;
		_sizeZ.ValueChanged += SelectedProbe.SetSizeY;
		
		_extentsX.ValueChanged += SelectedProbe.SetExtentsX;
		_extentsY.ValueChanged += SelectedProbe.SetExtentsY;
		_extentsZ.ValueChanged += SelectedProbe.SetExtentsZ;

		_rotation.ValueChanged += SelectedProbe.SetRotation;
	}
	
	private void DisconnectProbeGui()
	{
		_centerOffsetX.ValueChanged -= SelectedProbe.SetCenterOffsetX;
		_centerOffsetY.ValueChanged -= SelectedProbe.SetCenterOffsetZ;
		_centerOffsetZ.ValueChanged -= SelectedProbe.SetCenterOffsetY;
		
		_centerOffsetX.ReleaseFocus();
        _centerOffsetX.GetLineEdit().ReleaseFocus();
		_centerOffsetY.ReleaseFocus();
        _centerOffsetY.GetLineEdit().ReleaseFocus();
		_centerOffsetZ.ReleaseFocus();
        _centerOffsetZ.GetLineEdit().ReleaseFocus();
		
		_sizeX.ValueChanged -= SelectedProbe.SetSizeX;
		_sizeY.ValueChanged -= SelectedProbe.SetSizeZ;
		_sizeZ.ValueChanged -= SelectedProbe.SetSizeY;
		
		_sizeX.ReleaseFocus();
        _sizeX.GetLineEdit().ReleaseFocus();
		_sizeY.ReleaseFocus();
        _sizeY.GetLineEdit().ReleaseFocus();
		_sizeZ.ReleaseFocus();
        _sizeZ.GetLineEdit().ReleaseFocus();
		
		_extentsX.ValueChanged -= SelectedProbe.SetExtentsX;
		_extentsY.ValueChanged -= SelectedProbe.SetExtentsY;
		_extentsZ.ValueChanged -= SelectedProbe.SetExtentsZ;

		_extentsX.ReleaseFocus();
        _extentsX.GetLineEdit().ReleaseFocus();
		_extentsY.ReleaseFocus();
        _extentsY.GetLineEdit().ReleaseFocus();
		_extentsZ.ReleaseFocus();
        _extentsZ.GetLineEdit().ReleaseFocus();
		
		_rotation.ValueChanged -= SelectedProbe.SetRotation;
		
		_rotation.ReleaseFocus();
		_rotation.GetLineEdit().ReleaseFocus();
	}
}

using System;
using Godot;

public partial class ProjectMenu : Control
{
	[Export] private ItemList _projectList;
	[Export] private Button _loadProjectBtn;
	[Export] private Button _newProjectBtn;
	[Export] private Button _returnToCurrentBtn;
	[Export] private Button _changeProjectFolderButton;
	[Export] private FileDialog _projectFolderDialog;
	
	[ExportGroup("New Project View")]
	[Export] private Control _newProjectPanel;
	[Export] private LineEdit _newProjectNameBox;
	[Export] private Button _createButton;
	[Export] private Button _cancelButton;

	public static ProjectMenu Instance { get; private set; }
	
	public override void _Ready()
	{
		// menuscreens and popups being singletones is very convenient because there will only ever be one of them
		// and then you can trigger them from any other node without having to find a way to [Export] and link it
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			QueueFree();
			return;
		}

		GetTree().CreateTimer(0.01).Timeout += LoadProjectList;
		
		Visible = true; // This should be the screen you see at boot
		_returnToCurrentBtn.Visible = false;

		_loadProjectBtn.Pressed += () =>
		{
			if (_projectList.IsAnythingSelected() == false) return;

			var idx = _projectList.GetSelectedItems()[0];
			var name = _projectList.GetItemText(idx);
			SaveManager.LoadProject(name);
		};

		_newProjectBtn.Pressed += () => _newProjectPanel.Visible = true;
		
		SaveManager.ProjectLoaded += data =>
		{
			_newProjectPanel.Visible = false;
			_newProjectNameBox.Text = "";
			Visible = false;
		};

		_createButton.Pressed += () =>
		{
			var projectPath = _newProjectNameBox.Text;
			projectPath = projectPath.Split("/")[^1]; // can't be making subfolders you silly user
			projectPath = projectPath.Split("\"")[^1]; // also a weird choice, user
			projectPath = SaveManager.CleanPath(projectPath); // clean up the rest
			SaveManager.CreateProject(projectPath);
		};

		_cancelButton.Pressed += () =>
		{
			_newProjectNameBox.Text = "";
			_newProjectPanel.Visible = false;
		};

		VisibilityChanged += () => _returnToCurrentBtn.Visible = SaveManager.HasProject();
		_returnToCurrentBtn.Pressed += () => Visible = false;

		_changeProjectFolderButton.Pressed += () => _projectFolderDialog.PopupCentered();
		_projectFolderDialog.DirSelected += AttemptChangeFolder;
	}

	void LoadProjectList()
	{
		_projectList.Clear();
		
		foreach (var item in SaveManager.GetProjectList())
		{
			_projectList.AddItem(item);
		}
	}

	void AttemptChangeFolder(string newProjectFolder)
	{
		GD.Print($"Attempting to change folder to: {newProjectFolder}");
		var result = SaveManager.ChangeProjectFolder(newProjectFolder);
		if (result == Error.Ok)
		{
			LoadProjectList();
		}
		else
		{
			switch (result)
			{
				case Error.FileCantWrite:
					ErrorPopup.Instance.Trigger("ERROR: No write access in that folder!");
					break;
				case Error.FileCantOpen:
					ErrorPopup.Instance.Trigger("ERROR: Couldn't open directory!");
					break;
				default:
					ErrorPopup.Instance.Trigger($"ERROR: {Enum.GetName(result)}");
					break;
			}
			
		}
	}
}

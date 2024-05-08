using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WildRP.AMVTool;
using System.Text.Json;
using System.Text.Json.Serialization;
using WildRP.AMVTool.Autoloads;
using FileAccess = Godot.FileAccess;

public partial class SaveManager : Node
{
	public static event Action<Project> ProjectLoaded;
	public static event Action SavingProject;
	
	private static Project _currentProject;
	public static Project CurrentProject => _currentProject;

	private const string DefaultProjectsFolder = "user://projects";
	private const string JsonFileName = "project.json";

	private static string _projectsFolder = "";
	private static string _currentProjectPath = "";

	public static string GetProjectPath()
	{
		return _currentProjectPath;
	}

	public static string GetProjectsFolder()
	{
		return _projectsFolder == "" ? ProjectSettings.GlobalizePath(DefaultProjectsFolder) : _projectsFolder;
	}

	public override void _Ready()
	{
		ChangeProjectFolder(Settings.ProjectFolder);
		
		if (DirAccess.DirExistsAbsolute(DefaultProjectsFolder) == false)
			DirAccess.MakeDirAbsolute(DefaultProjectsFolder);
	}
	
	public static bool HasProject() => _currentProject != null;
	
	public static void SaveProject()
	{
		_currentProject.Volumes.Clear();
		_currentProject.Probes.Clear();
		
		SavingProject?.Invoke(); // send in your updated data my friends!
		
		if (_currentProject.Volumes.Count == 0 && _currentProject.Probes.Count == 0) return;

		if (DirAccess.DirExistsAbsolute(_currentProjectPath) == false)
			DirAccess.MakeDirAbsolute(_currentProjectPath);
		
		using var f = FileAccess.Open($"{_currentProjectPath}/{JsonFileName}", FileAccess.ModeFlags.Write);
		if (f == null) return;


		var options = new JsonSerializerOptions
		{
			WriteIndented = true
		};
		var serialized = JsonSerializer.Serialize(_currentProject, options);
		f.StoreString(serialized);
		GD.Print(serialized);
		GD.Print(_currentProject);
	}

	public static void LoadProject(string name)
	{
		_currentProject = null;
		_currentProjectPath = null;
		AmvBaker.Instance.Clear();
		DeferredProbeBaker.Instance.Clear();
		
		using var f = FileAccess.Open($"{GetProjectsFolder()}/{name}/{JsonFileName}", FileAccess.ModeFlags.Read);
		if (f == null) return; // error dialogs should be implemented at some point

		_currentProjectPath = f.GetPath().GetBaseDir();
		var jsonString = f.GetAsText();
		GD.Print(jsonString);
		_currentProject = JsonSerializer.Deserialize<Project>(jsonString);

		ProjectLoaded(_currentProject);
	}
	
	public static string[] GetProjectList() => DirAccess.GetDirectoriesAt(GetProjectsFolder());

	public static void CreateProject(string name)
	{
		AmvBaker.Instance.Clear();
		DeferredProbeBaker.Instance.Clear();
		_currentProject = new Project() { Name = name};
		_currentProjectPath = $"{GetProjectsFolder()}/{name}";
		SaveProject();
		ProjectLoaded(_currentProject);
	}

	public static void UpdateAmv(string name, AmbientMaskVolume.AmvData data)
	{
		_currentProject.Volumes[name] = data;
	}
	public static void UpdateDeferredProbe(string name, DeferredProbe.DeferredProbeData data)
	{
		_currentProject.Probes[name] = data;
	}
	
	public static void SetModel(string path) => _currentProject.ModelPath = path;
	public static void SetProbeModel(string path) => _currentProject.ReflectionModelPath = path;

	public static void UpdateYmapName(string name) => _currentProject.YMapName = name;
	public static void UpdateInteriorName(string name) => _currentProject.InteriorName = name;
	public static void UpdateYmapPosition(Vector3 pos) => _currentProject.YMapPosition = pos;
	
	public class Project()
	{
		[JsonInclude] public string Name = "";
		[JsonInclude] public string ModelPath = "";
		[JsonInclude] public string ReflectionModelPath = "";
		[JsonInclude, JsonConverter(typeof(Vector3JsonConverter))] public Vector3 YMapPosition = Vector3.Zero;
		[JsonInclude] public string YMapName = "";
		[JsonInclude] public string InteriorName = "";
		[JsonInclude] public Dictionary<string, AmbientMaskVolume.AmvData> Volumes = [];
		[JsonInclude] public Dictionary<string, DeferredProbe.DeferredProbeData> Probes = [];
	}
	
	// Shamelessly stolen from:
	// https://chrisbitting.com/2014/04/14/fixing-removing-invalid-characters-from-a-file-path-name-c/
	public static string CleanPath(string toCleanPath, string replaceWith = "-")
	{  
		//get just the filename - can't use Path.GetFileName since the path might be bad!  
		var pathParts = toCleanPath.Split(['\\']);  
		var newFileName = pathParts[^1];  
		//get just the path  
		var newPath = toCleanPath[..^newFileName.Length];   
		//clean bad path chars  
		newPath = Path.GetInvalidPathChars().Aggregate(newPath, (current, badChar) => current.Replace(badChar.ToString(), replaceWith));
		//clean bad filename chars  
		newFileName = Path.GetInvalidFileNameChars().Aggregate(newFileName, (current, badChar) => current.Replace(badChar.ToString(), replaceWith));
		//remove duplicate "replaceWith" characters. ie: change "test-----file.txt" to "test-file.txt"  
		if (string.IsNullOrWhiteSpace(replaceWith)) return newPath + newFileName;
		
		newPath = newPath.Replace(replaceWith + replaceWith, replaceWith);  
		newFileName = newFileName.Replace(replaceWith + replaceWith, replaceWith);
		//return new, clean path:  
		return newPath + newFileName;  
	}

	public class Vector3IJsonConverter : JsonConverter<Vector3I>
	{
		public override Vector3I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return GD.StrToVar(reader.GetString()).AsVector3I();
		}

		public override void Write(Utf8JsonWriter writer, Vector3I value, JsonSerializerOptions options)
		{
			var v = Variant.CreateFrom(value);
			writer.WriteStringValue(GD.VarToStr(v));
		}
	}
	
	public class Vector3JsonConverter : JsonConverter<Vector3>
	{
		public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			
			return GD.StrToVar(reader.GetString()).AsVector3();
		}

		public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
		{
			var v = Variant.CreateFrom(value);
			writer.WriteStringValue(GD.VarToStr(v));
		}
	}

	public static Error ChangeProjectFolder(string path)
	{
		path = CleanPath(path);
		if (path == "") return Error.Ok;
		
		var testFilePath = path + "/writetestfile";
		using var f = FileAccess.Open(testFilePath, FileAccess.ModeFlags.Write);
		if (f == null)
		{
			return FileAccess.GetOpenError();
		}
		f.Close();
		DirAccess.RemoveAbsolute(testFilePath);

		_projectsFolder = path;
		Settings.ProjectFolder = path;
		
		return Error.Ok;
	}
}

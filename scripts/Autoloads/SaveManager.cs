using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WildRP.AMVTool;
using System.Text.Json;
using System.Text.Json.Serialization;
using WildRP.AMVTool.GUI;
using FileAccess = Godot.FileAccess;

public partial class SaveManager : Node
{
	public static event Action<Project> ProjectLoaded;
	
	private static Project _currentProject;

	private const string ProjectsFolder = "user://projects";
	private const string JsonFileName = "project.json";

	private static string GetProjectPath() => $"{ProjectsFolder}/{_currentProject.Name}";
	public static string GetGlobalizedProjectPath() => ProjectSettings.GlobalizePath(GetProjectPath());
	public override void _Ready()
	{
		if (DirAccess.DirExistsAbsolute(ProjectsFolder) == false)
			DirAccess.MakeDirAbsolute(ProjectsFolder);
	}

	public static bool HasProject() => _currentProject != null;
	
	public static bool SaveProject()
	{
		if (_currentProject.ModelPath == "" && _currentProject.Volumes.Count == 0) return false;

		if (DirAccess.DirExistsAbsolute(GetProjectPath()) == false)
			DirAccess.MakeDirAbsolute(GetProjectPath());
		
		using var f = FileAccess.Open($"{GetProjectPath()}/{JsonFileName}", FileAccess.ModeFlags.Write);
		if (f == null) return false;
		
		var serialized = JsonSerializer.Serialize(_currentProject);
		f.StoreString(serialized);
		GD.Print(serialized);
		GD.Print(_currentProject);
		return true;
	}

	public static void LoadProject(string name)
	{
		_currentProject = null;
		AmvBaker.Instance.Clear();
		
		using var f = FileAccess.Open($"{ProjectsFolder}/{name}/{JsonFileName}", FileAccess.ModeFlags.Read);
		if (f == null) return; // error dialogs should be implemented at some point

		var jsonString = f.GetAsText();
		_currentProject = JsonSerializer.Deserialize<Project>(jsonString);

		ProjectLoaded(_currentProject);
	}
	
	public static string[] GetProjectList() => DirAccess.GetDirectoriesAt(ProjectsFolder);

	public static void CreateProject(string name)
	{
		_currentProject = new Project() { Name = name};
		SaveProject();
		ProjectLoaded(_currentProject);
	}

	public static void UpdateAmv(string name, AmbientMaskVolume.AmvData data)
	{
		_currentProject.Volumes[name] = data;
	}

	public static void DeleteAmv(string name)
	{
		_currentProject.Volumes.Remove(name);
	}

	public static void SetModel(string path) => _currentProject.ModelPath = path;
	
	public class Project()
	{
		[JsonInclude]
		public string Name;
		
		[JsonInclude]
		public string ModelPath = "";
		
		[JsonInclude]
		public Dictionary<string, AmbientMaskVolume.AmvData> Volumes = [];
	}
	
	// Shamelessly stolen from:
	// https://chrisbitting.com/2014/04/14/fixing-removing-invalid-characters-from-a-file-path-name-c/
	public static string CleanPath(string toCleanPath, string replaceWith = "-")
	{  
		//get just the filename - can't use Path.GetFileName since the path might be bad!  
		var pathParts = toCleanPath.Split(new char[] { '\\' });  
		var newFileName = pathParts[^1];  
		//get just the path  
		var newPath = toCleanPath[..^newFileName.Length];   
		//clean bad path chars  
		newPath = Path.GetInvalidPathChars().Aggregate(newPath, (current, badChar) => current.Replace(badChar.ToString(), replaceWith));
		//clean bad filename chars  
		newFileName = Path.GetInvalidFileNameChars().Aggregate(newFileName, (current, badChar) => current.Replace(badChar.ToString(), replaceWith));
		//remove duplicate "replaceWith" characters. ie: change "test-----file.txt" to "test-file.txt"  
		if (string.IsNullOrWhiteSpace(replaceWith)) return newPath + newFileName;
		
		newPath = newPath.Replace(replaceWith.ToString() + replaceWith.ToString(), replaceWith.ToString());  
		newFileName = newFileName.Replace(replaceWith.ToString() + replaceWith.ToString(), replaceWith.ToString());
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
}

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using SimpleImageIO;
using WildRP.AMVTool.Autoloads;
using WildRP.AMVTool.GUI;
using WildRP.AMVTool.Sceneview;
using FileAccess = Godot.FileAccess;

namespace WildRP.AMVTool;

public partial class AmbientMaskVolume : Volume
{
	[Export] private AmvBoundsMesh _boundsMesh;
	[Export] private PackedScene _probeScene;
	public bool IncludeInFullBake { get; set; } = true;
	
	private int _samples;

	public int Samples
	{
		get => _samples;
		set
		{
			_samples = value;
			if (_samples >= AmvBaker.GetSampleCount()) Baked = true;
		}
	}

	private List<AmvProbe> _probes = [];
	
	private Vector3I _probeCount = Vector3I.One * 2;
	public Vector3I ProbeCount
	{
		get => _probeCount;
		set => _probeCount = value;
	}

	public ulong TextureName { get; set; }
	public event Action ProbeCountChanged;

	public void GenerateTextures()
	{
		// Channel mapping:
		// Tex 0: RGB = XZY+
		// Tex 1: RGB = XZY-
		// Remember: Y is up in Godot. Z is up in RDR2.

		var tex0Dir = $"{SaveManager.GetProjectPath()}/{TextureName.ToString()}_0";
		var tex0DirG = ProjectSettings.GlobalizePath(tex0Dir);
		var tex1Dir = $"{SaveManager.GetProjectPath()}/{TextureName.ToString()}_1";
		var tex1DirG = ProjectSettings.GlobalizePath(tex1Dir);

		if (DirAccess.DirExistsAbsolute(tex0Dir) == false)
			DirAccess.MakeDirAbsolute(tex0Dir);

		if (DirAccess.DirExistsAbsolute(tex1Dir) == false)
			DirAccess.MakeDirAbsolute(tex1Dir);

		List<string> img0List = [];
		List<string> img1List = [];
		
		for (int y = 0; y < ProbeCount.Y; y++)
		{
			// Generate a new iamge per layer
			RgbImage img0 = new(ProbeCount.X, ProbeCount.Z);
			RgbImage img1 = new(ProbeCount.X, ProbeCount.Z);
			
			for (int x = 0; x < ProbeCount.X; x++)
			{
				for (int z = 0; z < ProbeCount.Z; z++)
				{
					var idx = CellToIndex(new Vector3I(x, y, z));
					var p = _probes[idx].GetValue();
					
					// flip Z because Godot's Z axis is backwards
					var col0 = new RgbColor((float) p.X.Positive, (float) p.X.Negative, (float) p.Z.Negative);
					var col1 = new RgbColor((float) p.Z.Positive, (float) p.Y.Positive, (float) p.Y.Negative);
					
					img0.SetPixel(x,ProbeCount.Z - 1 - z, col0);
					img1.SetPixel(x,ProbeCount.Z - 1 - z, col1);
				}
			}
			
			var tex0Name = $"{tex0DirG}/slice_{y}.hdr";
			var tex1Name = $"{tex1DirG}/slice_{y}.hdr";
			
			img0.WriteToFile(tex0Name);
			img1.WriteToFile(tex1Name);
			
			img0List.Add(tex0Name);
			img1List.Add(tex1Name);
			
			img0.Dispose();
			img1.Dispose();
		}
		
		using var f0 = FileAccess.Open($"{tex0Dir}/imgs.txt", FileAccess.ModeFlags.Write);
			img0List.ForEach(s => f0.StoreLine(s));
			
		using var f1 = FileAccess.Open($"{tex1Dir}/imgs.txt", FileAccess.ModeFlags.Write);
			img1List.ForEach(s => f1.StoreLine(s));
			

			var tx0 = new Process();
			tx0.StartInfo.FileName = Settings.TexAssembleLocation;
			tx0.StartInfo.Arguments =
				$"array -O \"{SaveManager.GetGlobalizedProjectPath()}\\{TextureName}_0.dds\" -l -y -if linear -f R11G11B10_FLOAT -fl 12.1 -flist \"{tex0DirG}\\imgs.txt\"";
			tx0.Start();
			
			var tx1 = new Process();
			tx1.StartInfo.FileName = Settings.TexAssembleLocation;
			tx1.StartInfo.Arguments =
				$"array -O \"{SaveManager.GetGlobalizedProjectPath()}\\{TextureName}_1.dds\" -l -y -if linear -f R11G11B10_FLOAT -fl 12.1 -flist \"{tex1DirG}\\imgs.txt\"";
			tx1.Start();
	}
	
	public void CaptureSample()
	{
		foreach (var probe in _probes)
		{
			probe.CaptureSample();
		}

		Samples++;
	}

	public void UpdateAverages(bool bakeFinished = false)
	{
		foreach (var probe in _probes)
		{
			probe.UpdateAverage(bakeFinished);
		}

		if (bakeFinished)
		{
			Blur(Vector3I.Up);
			Blur(Vector3I.Right);
			Blur(Vector3I.Back);
		}
	}
	
	public void ClearSamples()
	{
		Samples = 0;
		Baked = false;
		foreach (var probe in _probes)
		{
			probe.Reset();
		}
	}

	public void Blur(Vector3I axis)
	{
		foreach (var probe in _probes)
		{
			probe.Blur(axis);
		}

		foreach (var probe in _probes)
		{
			probe.SetValueFromBlurred();
		}
	}
	
	public override void _Ready()
	{
		UpdateProbes();
		UpdateProbePositions();
		SizeChanged += UpdateProbePositions;
		ProbeCountChanged += UpdateProbes;
	}
	
	private int _prevProbeCount = 0;

	private void UpdateProbes()
	{
		int numProbes = ProbeCount.X * ProbeCount.Y * ProbeCount.Z;
		if (numProbes > _prevProbeCount) // Generate Extra Probes
		{
			var newProbes = numProbes - _prevProbeCount;
			
			for (int i = 0; i < newProbes; i++)
			{
				var probe = _probeScene.Instantiate() as AmvProbe;
				_probes.Add(probe);
				probe.ParentVolume = this;
				AddChild(probe);
			}

		}
		else if (numProbes < _prevProbeCount)
		{
			var probesToKill = _probes.TakeLast(_prevProbeCount - numProbes).ToList();
			probesToKill.ForEach(p =>
			{
				_probes.Remove(p);
				p.QueueFree();
			});
		}

		for (int j = 0; j < _probes.Count; j++)
		{
			_probes[j].CellPosition = IndexToCell(j);
		}
		
		_prevProbeCount = numProbes;
		UpdateProbePositions();
	}

	private void UpdateProbePositions()
	{
		_probes.ForEach(p =>
		{
			var step = Size / ProbeCount;
			p.Position = -Size / 2 + p.CellPosition * step + step / 2;
		});
		
	}
	
	private Vector3I IndexToCell(int idx)
	{
		var result = Vector3I.Zero;
		result.X = idx % ProbeCount.X;
		result.Y = (idx / ProbeCount.X) % ProbeCount.Y;
		result.Z = idx / (ProbeCount.X * ProbeCount.Y);

		return result;
	}

	private int CellToIndex(Vector3I cell)
	{
		var idx = cell.X + cell.Y * ProbeCount.X + cell.Z * ProbeCount.X * ProbeCount.Y;
		if (idx < 0 || idx > _probes.Count-1) return -1;
		return idx;
	}

	public ProbeSample GetCellValue(Vector3I target)
	{
		var idx = CellToIndex(target);
		return idx < 0 ? 0 : _probes[CellToIndex(target)].GetValue();
	}
	
	public ProbeSample GetCellValueRelative(Vector3I origin, Vector3I target)
	{
		var pos = origin;
		var targetDir = target.Clamp(-Vector3I.One, Vector3I.One);
		var steps = target.Abs()[(int)target.Abs().MaxAxisIndex()];
		var value = new ProbeSample(0);
		
		for (int i = steps; i >= 0; i--)
		{
			var idx = CellToIndex(origin + targetDir * steps);
			if (idx < 0) continue;

			value = _probes[idx].GetValue();
			break;
		}

		return value;
	}
	
	public override void Delete()
	{
		QueueFree();
		SaveManager.DeleteAmv(GuiListName);
		OnDeleted();
	}

	public void Load(KeyValuePair<string, AmvData> data)
	{
		GuiListName = data.Key;
		TextureName = data.Value.TextureName;
		Position = data.Value.Position;
		Size = data.Value.Size;
		ProbeCount = data.Value.ProbeCount;

		var rot = RotationDegrees;
		rot.Y = data.Value.Rotation;
		RotationDegrees = rot;
	}

	public AmvData Save()
	{
		var data = new AmvData();
		
		data.TextureName = TextureName;
		data.Position = Position;
		data.Size = Size;
		data.ProbeCount = ProbeCount;
		data.Rotation = RotationDegrees.Y;

		return data;
	}

	public override string GetXml()
	{
		var xmlSize = Size/2f;
		//xmlSize += Size / ProbeCount / 2;
		var ymapPosition = SaveManager.CurrentProject.YMapPosition;

		var iplHash = 0U;
		if (SaveManager.CurrentProject.YMapName != "")
			iplHash = Utils.JenkinsHash(SaveManager.CurrentProject.YMapName);
		
		return new XDocument(
			new XElement("Item",
				new XElement("enabled", new XAttribute("value", true)),
				new XElement("position", new XAttribute("x", ymapPosition.X + Position.X), new XAttribute("y", ymapPosition.Y - Position.Z), new XAttribute("z", ymapPosition.Z + Position.Y)),
				new XElement("rotation", new XAttribute("x", -RotationDegrees.Y), new XAttribute("y", 0), new XAttribute("z", 0)),
				new XElement("scale", new XAttribute("x", xmlSize.X), new XAttribute("y", xmlSize.Z), new XAttribute("z", xmlSize.Y)),
				new XElement("falloffScaleMin", new XAttribute("x", 1f), new XAttribute("y", 1f), new XAttribute("z", 1f)),
				new XElement("falloffScaleMax", new XAttribute("x", 1.25f), new XAttribute("y", 1.25f), new XAttribute("z", 1.25f)),
				new XElement("samplingOffsetStrength", new XAttribute("value", 0)), // Guessing: Pushes sample point off of walls in direction of normal
				new XElement("falloffPower", new XAttribute("value", 8)),
				new XElement("distance", new XAttribute("value", -1)),
				new XElement("cellCountX", new XAttribute("value", ProbeCount.X)),
				new XElement("cellCountY", new XAttribute("value", ProbeCount.Y)),
				new XElement("cellCountZ", new XAttribute("value", ProbeCount.Z)),
				new XElement("clipPlane0", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)), // Quaternions?
				new XElement("clipPlane1", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlane2", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlane3", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlaneBlend0", new XAttribute("value", 0)), // Strength of clip planes
				new XElement("clipPlaneBlend1", new XAttribute("value", 0)),
				new XElement("clipPlaneBlend2", new XAttribute("value", 0)),
				new XElement("clipPlaneBlend3", new XAttribute("value", 0)),
				new XElement("blendingMode", "BM_Lerp"),
				new XElement("layer", new XAttribute("value", 0)),
				new XElement("order", new XAttribute("value", 10)),
				new XElement("natural", new XAttribute("value", true)),
				new XElement("attachedToDoor", new XAttribute("value", false)),
				new XElement("interior", new XAttribute("value", true)),
				new XElement("exterior", new XAttribute("value", false)),
				new XElement("vehicleInterior", new XAttribute("value", false)),
				new XElement("sourceFolder", "NotRelevant"),
				new XElement("uuid", new XAttribute("value", TextureName)),
				new XElement("iplHash", new XAttribute("value", iplHash))
			)).ToString();
	}

	public override bool Selected() => AmvBakerGui.SelectedAmv == this;

	public class AmvData : VolumeData
	{
		[JsonInclude]
		public ulong TextureName = 0;
		[JsonInclude]
		public float Rotation = 0;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3IJsonConverter))]
		public Vector3I ProbeCount = Vector3I.One * 2;
	}
	
	// These are used to connect to the UI
	#region UIConnectFunctions
	
	public void SetProbesX(double n)
	{
		var v = ProbeCount;
		v.X = (int)n;
		ProbeCount = v;
		ProbeCountChanged();
	}
	
	public void SetProbesZ(double n)
	{
		var v = ProbeCount;
		v.Y = (int)n;
		ProbeCount = v;
		ProbeCountChanged();
	}
	
	public void SetProbesY(double n)
	{
		var v = ProbeCount;
		v.Z = (int)n;
		ProbeCount = v;
		ProbeCountChanged();
	}

	public void SetRotation(double n)
	{
		var v = RotationDegrees;
		v.Y = (float)n;
		RotationDegrees = v;
	}
	#endregion
}

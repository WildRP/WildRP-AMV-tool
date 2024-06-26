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
using Image = Godot.Image;

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

	public int Layer;
	public int Order;
	public float FalloffPower;
	public Vector3 FalloffScaleMin = Vector3.One;
	public Vector3 FalloffScaleMax = Vector3.One * 1.25f;
	public bool Interior;
	public bool Exterior;
	public bool AttachedToDoor;
	

	public void GenerateTextures()
	{
		// Channel mapping:
		// Tex 0: RGB = XZY+
		// Tex 1: RGB = XZY-
		// Remember: Y is up in Godot. Z is up in RDR2.

		var texDir = $"{SaveManager.GetProjectPath()}/{TextureName}";
		var texDirGlobal = ProjectSettings.GlobalizePath(texDir);

		if (DirAccess.DirExistsAbsolute(texDir) == false)
			DirAccess.MakeDirAbsolute(texDir);

		List<string> img0List = [];
		List<string> img1List = [];
		
		for (int y = 0; y < ProbeCount.Y; y++)
		{
			// Generate a new iamge per layer
			var img0 = new RgbImage(ProbeCount.X, ProbeCount.Z);
			var img1 = new RgbImage(ProbeCount.X, ProbeCount.Z);
			for (int x = 0; x < ProbeCount.X; x++)
			{
				for (int z = 0; z < ProbeCount.Z; z++)
				{
					var idx = CellToIndex(new Vector3I(x, y, z));
					var p = _probes[idx].GetValue();
					
					
					// flip Z because Godot's Z axis is backwards
					var col0 = new RgbColor(Mathf.Abs((float) p.X.Positive), Mathf.Abs((float) p.X.Negative), Mathf.Abs((float) p.Z.Negative));
					var col1 = new RgbColor(Mathf.Abs((float) p.Z.Positive), Mathf.Abs((float) p.Y.Positive), Mathf.Abs((float) p.Y.Negative));
					
					img0.SetPixel(x,ProbeCount.Z - 1 - z, col0);
					img1.SetPixel(x,ProbeCount.Z - 1 - z, col1);
				}
			}
			
			var tex0Name = $"{texDirGlobal}/slice_{y}_0.png";
			var tex1Name = $"{texDirGlobal}/slice_{y}_1.png";
			
			img0.WriteToFile(tex0Name);
			img1.WriteToFile(tex1Name);
			
			img0List.Add(tex0Name);
			img1List.Add(tex1Name);
			
			img0.Dispose();
			img1.Dispose();
		}
		
		using var f0 = FileAccess.Open($"{texDir}/imgs_0.txt", FileAccess.ModeFlags.Write);
			img0List.ForEach(s => f0.StoreLine(s));
			
		using var f1 = FileAccess.Open($"{texDir}/imgs_1.txt", FileAccess.ModeFlags.Write);
			img1List.ForEach(s => f1.StoreLine(s));

			var tx0 = Tex.Assemble($"{TextureName}/imgs_0.txt", $"{TextureName}_0.dds", Settings.AmvTextureFormat);
			var tx1 = Tex.Assemble($"{TextureName}/imgs_1.txt", $"{TextureName}_1.dds", Settings.AmvTextureFormat);

			tx0.Exited += CleanupExport;
			tx1.Exited += CleanupExport;
			
			tx0.Run();
			tx1.Run();
	}

	private int _exportSteps = 0;

	private void CleanupExport()
	{
		_exportSteps++;
		if (_exportSteps < 2) return;

		_exportSteps = 0;

		return;
		// Clean up files, leaving only the exported DDS files
		var path = $"{SaveManager.GetProjectPath()}/{TextureName}";
		var files = DirAccess.GetFilesAt(path);

		foreach (var file in files)
		{
			DirAccess.RemoveAbsolute($"{path}/{file}");
		}

		DirAccess.RemoveAbsolute(path);
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
			Baked = true;
			BlurProbes(Vector3I.Up);
			BlurProbes(Vector3I.Right);
			BlurProbes(Vector3I.Back);

			foreach (var probe in _probes)
			{
				probe.UpdateBlur();
			}
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

	public void BlurProbes(Vector3I axis)
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

	public void UpdateBlur()
	{
		if (Baked == false) return;
		
		foreach (var probe in _probes)
		{
			probe.ClearBlur();
		}
		
		BlurProbes(Vector3I.Up);
		BlurProbes(Vector3I.Right);
		BlurProbes(Vector3I.Back);

		foreach (var probe in _probes)
		{
			probe.UpdateBlur();
		}
	}
	
	public override void _Ready()
	{
		UpdateProbes();
		UpdateProbePositions();
		SizeChanged += UpdateProbePositions;
		ProbeCountChanged += UpdateProbes;
	}
	public void SaveToProject() => SaveManager.UpdateAmv(GuiListName,Save());
	
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
		ClearSamples();
	}

	private void UpdateProbePositions()
	{
		_probes.ForEach(p =>
		{
			var step = Size / ProbeCount;
			p.Position = -Size / 2 + p.CellPosition * step + step / 2;
		});
		ClearSamples();
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
		var originalProbe = GetCellValue(origin);

		var targetPos = origin + target;
		
		if (targetPos.X < 0 || targetPos.Y < 0 || targetPos.Z < 0 ||
		    targetPos.X > ProbeCount.X - 1 || targetPos.Y > ProbeCount.Y - 1 || targetPos.Z > ProbeCount.Z - 1)
			return originalProbe;
		
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

		Layer = data.Value.Layer;
		Order = data.Value.Order;
		FalloffPower = data.Value.FalloffPower;
		FalloffScaleMax = data.Value.FalloffScaleMax;
		FalloffScaleMin = data.Value.FalloffScaleMin;
		Interior = data.Value.Interior;
		Exterior = data.Value.Exterior;
		AttachedToDoor = data.Value.AttachedToDoor;
	}

	public AmvData Save()
	{
		var data = new AmvData();
		
		data.TextureName = TextureName;
		data.Position = Position;
		data.Size = Size;
		data.ProbeCount = ProbeCount;
		data.Rotation = RotationDegrees.Y;

		data.Layer = Layer;
		data.Order = Order;
		data.FalloffPower = FalloffPower;
		data.FalloffScaleMax = FalloffScaleMax;
		data.FalloffScaleMin = FalloffScaleMin;
		data.Interior = Interior;
		data.Exterior = Exterior;
		data.AttachedToDoor = AttachedToDoor;
		
		
		return data;
	}

	public void SetupNew(string name, int order)
	{
		base.Setup(name);

		Layer = 0;
		Order = order;
		FalloffPower = 2f;
		FalloffScaleMin = Vector3.One;
		FalloffScaleMax = Vector3.One * 1.25f;
		Interior = true;
		Exterior = false;
		AttachedToDoor = false;
	}

	public override string GetXml()
	{
		var xmlSize = Size/2f;
		//xmlSize += Size / ProbeCount / 2;
		var ymapPosition = SaveManager.CurrentProject.YMapPosition;

		var iplHash = "0";
		if (SaveManager.CurrentProject.YMapName != "")
			iplHash = SaveManager.CurrentProject.YMapName;
		
		return new XDocument(
			new XElement("Item",
				new XElement("enabled", new XAttribute("value", true)),
				new XElement("position", new XAttribute("x", ymapPosition.X + Position.X), new XAttribute("y", ymapPosition.Y - Position.Z), new XAttribute("z", ymapPosition.Z + Position.Y)),
				new XElement("rotation", new XAttribute("x", -RotationDegrees.Y), new XAttribute("y", 0), new XAttribute("z", 0)),
				new XElement("scale", new XAttribute("x", xmlSize.X), new XAttribute("y", xmlSize.Z), new XAttribute("z", xmlSize.Y)),
				new XElement("falloffScaleMin", new XAttribute("x", FalloffScaleMin.X), new XAttribute("y", FalloffScaleMin.Z), new XAttribute("z", FalloffScaleMin.Y)),
				new XElement("falloffScaleMax", new XAttribute("x", FalloffScaleMax.X), new XAttribute("y", FalloffScaleMax.Z), new XAttribute("z", FalloffScaleMax.Y)),
				new XElement("samplingOffsetStrength", new XAttribute("value", 0)), // Guessing: Pushes sample point off of walls in direction of normal
				new XElement("falloffPower", new XAttribute("value", FalloffPower)),
				new XElement("distance", new XAttribute("value", -1)),
				new XElement("cellCountX", new XAttribute("value", ProbeCount.X)),
				new XElement("cellCountY", new XAttribute("value", ProbeCount.Z)),
				new XElement("cellCountZ", new XAttribute("value", ProbeCount.Y)),
				new XElement("clipPlane0", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)), // Quaternions?
				new XElement("clipPlane1", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlane2", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlane3", new XAttribute("x", 0), new XAttribute("y", 0), new XAttribute("z", 0), new XAttribute("w", 1)),
				new XElement("clipPlaneBlend0", new XAttribute("value", 0)), // Strength of clip planes
				new XElement("clipPlaneBlend1", new XAttribute("value", 0)),
				new XElement("clipPlaneBlend2", new XAttribute("value", 0)),
				new XElement("clipPlaneBlend3", new XAttribute("value", 0)),
				new XElement("blendingMode", "BM_Lerp"),
				new XElement("layer", new XAttribute("value", Layer)),
				new XElement("order", new XAttribute("value", Order)),
				new XElement("natural", new XAttribute("value", true)),
				new XElement("attachedToDoor", new XAttribute("value", AttachedToDoor)),
				new XElement("interior", new XAttribute("value", Interior)),
				new XElement("exterior", new XAttribute("value", Exterior)),
				new XElement("vehicleInterior", new XAttribute("value", false)),
				new XElement("sourceFolder", GuiListName),
				new XElement("uuid", new XAttribute("value", TextureName)),
				new XElement("iplHash", new XAttribute("value", iplHash))
			)).ToString();
	}

	public override bool Selected() => AmvBakerGui.SelectedAmv == this;

	public class AmvData : VolumeData
	{
		[JsonInclude]
		public ulong TextureName = 0;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3IJsonConverter))]
		public Vector3I ProbeCount = Vector3I.One * 2;

		[JsonInclude] public int Layer = 0;
		[JsonInclude] public int Order = 1;
		[JsonInclude] public float FalloffPower = 2;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))] public Vector3 FalloffScaleMin = Vector3.One;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))] public Vector3 FalloffScaleMax = Vector3.One * 1.25f;
		[JsonInclude] public bool Interior = true;
		[JsonInclude] public bool Exterior = false;
		[JsonInclude] public bool AttachedToDoor = false;
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

	public void SetLayer(bool v) => Layer = v ? 1 : 0;
	public void SetOrder(double n) => Order = (int) n;

	public void SetFalloffPower(double n) => FalloffPower = (float) n;
	public void SetFalloffScaleMinX(double n) => FalloffScaleMin.X = (float) n;
	public void SetFalloffScaleMinY(double n) => FalloffScaleMin.Y = (float) n;
	public void SetFalloffScaleMinZ(double n) => FalloffScaleMin.Z = (float) n;
	public void SetFalloffScaleMaxX(double n) => FalloffScaleMax.X = (float) n;
	public void SetFalloffScaleMaxY(double n) => FalloffScaleMax.Y = (float) n;
	public void SetFalloffScaleMaxZ(double n) => FalloffScaleMax.Z = (float) n;

	public void SetInterior(bool b) => Interior = b;
	public void SetExterior(bool b) => Exterior = b;
	public void SetAttachedToDoor(bool b) => AttachedToDoor = b;

	#endregion
}

using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using SimpleImageIO;
using WildRP.AMVTool.Autoloads;
using WildRP.AMVTool.GUI;
using WildRP.AMVTool.Sceneview;
using FileAccess = Godot.FileAccess;

namespace WildRP.AMVTool;

public partial class AmbientMaskVolume : Node3D
{
	[Export] private AmvBoundsMesh _boundsMesh;
	[Export] private PackedScene _probeScene;
	public string GuiListName { get; private set; }
	public bool Selected => AmvBakerGui.SelectedAmv == this;
	public bool IncludeInFullBake { get; set; } = true;
	public void Setup(string name) => GuiListName = name;

	private bool _baked;
	public bool Baked
	{
		get;
		set;
	}

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

	private Vector3 _size = Vector3.One;
	private Vector3 _ymapPosition = Vector3.Zero;

	private List<AmvProbe> _probes = [];

	public Vector3 Size
	{
		get => _size;
		private set => _size = value;
	}
	
	private Vector3I _probeCount = Vector3I.One * 2;
	public Vector3I ProbeCount
	{
		get => _probeCount;
		set => _probeCount = value;
	}

	public ulong TextureName { get; set; }

	public event Action<AmbientMaskVolume> Deleted;
	public event Action SizeChanged;

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
					
                    // Attempted configurations:
                    // --- #1: Flipped lighting on front wall?
                    // XZY+ 
                    // XZY-
                    // --- #2: Seems to make no difference whatsoever? Still think this is more correct
                    // X+, Z-, Y+
                    // X-, Z+, Y-
                    // ---
					
					var col0 = new RgbColor((float) p.X.Positive, (float) p.Z.Negative, (float) p.Y.Positive);
					var col1 = new RgbColor((float) p.X.Negative, (float) p.Z.Positive, (float) p.Y.Negative);
					
					img0.SetPixel(x,z, col0);
					img1.SetPixel(x,z, col1);
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
	
	public override void _Ready()
	{
		GenerateProbes();
		SizeChanged += GenerateProbes;
	}

	// This should really be replaced with a dynamic object pool so we don't delete and recreate probes every time we resize
	public void GenerateProbes()
	{
		_probes.ForEach(p => p.QueueFree());
		_probes.Clear();

		var stepSizeX = Size.X / ProbeCount.X;
		var stepSizeY = Size.Y / ProbeCount.Y;
		var stepSizeZ = Size.Z / ProbeCount.Z;
		
		var probePos = Vector3.Zero;
		var probeCounter = 0;
		
		
		// a 3-dimensional loop!
		for (int x = 0; x < ProbeCount.X; x++)
		{
			probePos.X = -Size.X/2 + x * stepSizeX + stepSizeX / 2;
			for (int y = 0; y < ProbeCount.Y; y++)
			{
				probePos.Y = -Size.Y / 2 + y * stepSizeY + stepSizeY / 2;
				for (int z = 0; z < ProbeCount.Z; z++)
				{
					probePos.Z = -Size.Z / 2 + z * stepSizeZ + stepSizeZ / 2;

					var finalPos = probePos;
					var probe = _probeScene.Instantiate() as AmvProbe;
					probe.CellPosition = new Vector3I(x, y, z);
					probe.ParentVolume = this;
					AddChild(probe);
					
					_probes.Add(probe);
					
					probe.Position = finalPos;
					probe.GlobalRotation = GlobalRotation;
					probeCounter++;
				}
			}
		}
	}

	private Vector3I IndexToCell(int idx)
	{
		return new Vector3I
		{
			X = Mathf.PosMod(idx, ProbeCount.X),
			Y = Mathf.PosMod(idx / ProbeCount.X, ProbeCount.Y),
			Z = Mathf.PosMod(idx / ProbeCount.X / ProbeCount.Y, ProbeCount.Z)
		};
	}

	private int CellToIndex(Vector3I cell)
	{
		if (cell.X < 0 || cell.X > ProbeCount.X-1 ||
		    cell.Y < 0 || cell.Y > ProbeCount.Y-1 ||
		    cell.Z < 0 || cell.Z > ProbeCount.Z-1) return -1;
		
		return cell.X + cell.Y * ProbeCount.X + cell.Z * ProbeCount.X * ProbeCount.Y;
	}
	
	public ProbeSample GetCellValue(ProbeSample origin, Vector3I target)
	{
		var idx = CellToIndex(target);
		return idx < 0 ? origin : _probes[idx].GetValue();
	}

	public Vector3I PositionTest(Vector3I pos)
	{
		return IndexToCell(CellToIndex(pos));
	}
	
	public void Delete()
	{
		QueueFree();
		SaveManager.DeleteAmv(GuiListName);
		Deleted(this);
	}

	public void ChangeSizeWithGizmo(Vector3 diff, bool positive)
	{
		
		_size += diff;
		
		if (positive)
			diff *= -1;
		
		Position -= Basis * (diff / 2);
		
		SizeChanged();
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
		_ymapPosition = data.Value.YmapPosition;
	}

	public AmvData Save()
	{
		var data = new AmvData();
		
		data.TextureName = TextureName;
		data.Position = Position;
		data.Size = Size;
		data.ProbeCount = ProbeCount;
		data.Rotation = RotationDegrees.Y;
		data.YmapPosition = _ymapPosition;

		return data;
	}

	public string GetXml()
	{
		var xmlSize = Size/2f;
		xmlSize += Size / ProbeCount / 2;
		
		return new XDocument(
			new XElement("Item",
				new XElement("enabled", new XAttribute("value", true)),
				new XElement("position", new XAttribute("x", _ymapPosition.X + Position.X), new XAttribute("y", _ymapPosition.Z + -Position.Z), new XAttribute("z", _ymapPosition.Y + Position.Y)),
				new XElement("rotation", new XAttribute("x", 90-RotationDegrees.Y), new XAttribute("y", 0), new XAttribute("z", 0)),
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
				new XElement("iplHash", new XAttribute("value", 0))
			)).ToString();
	}
	
	public class AmvData // Used for save and load
	{
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 YmapPosition = Vector3.Zero;
		[JsonInclude]
		public ulong TextureName = 0;
		[JsonInclude]
		public float Rotation = 0;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Position = Vector3.Zero;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3JsonConverter))]
		public Vector3 Size = Vector3.One;
		[JsonInclude, JsonConverter(typeof(SaveManager.Vector3IJsonConverter))]
		public Vector3I ProbeCount = Vector3I.One * 2;
	}
	
	// These are used to connect to the UI
	#region UIConnectFunctions

	public void SetSizeX(double n)
	{
		var v = Size;
		v.X = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetSizeY(double n)
	{
		var v = Size;
		v.Y = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetSizeZ(double n)
	{
		var v = Size;
		v.Z = (float)n;
		Size = v;
		SizeChanged();
	}
	
	public void SetPositionX(double n)
	{
		var v = Position;
		v.X = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetPositionY(double n)
	{
		var v = Position;
		v.Y = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetPositionZ(double n)
	{
		var v = Position;
		v.Z = (float)n;
		Position = v;
		SizeChanged();
	}
	
	public void SetProbesX(double n)
	{
		var v = ProbeCount;
		v.X = (int)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesZ(double n)
	{
		var v = ProbeCount;
		v.Y = (int)n;
		ProbeCount = v;
		SizeChanged();
	}
	
	public void SetProbesY(double n)
	{
		var v = ProbeCount;
		v.Z = (int)n;
		ProbeCount = v;
		SizeChanged();
	}

	public void SetRotation(double n)
	{
		var v = RotationDegrees;
		v.Y = (float)n;
		RotationDegrees = v;
	}
	
	public void SetYmapPositionX(double n)
	{
		var v = _ymapPosition;
		v.X = (float)n;
		_ymapPosition = v;
	}
	
	public void SetYmapPositionY(double n)
	{
		var v = _ymapPosition;
		v.Y = (float)n;
		_ymapPosition = v;
	}
	
	public void SetYmapPositionZ(double n)
	{
		var v = _ymapPosition;
		v.Z = (float)n;
		_ymapPosition = v;
	}
	#endregion
}

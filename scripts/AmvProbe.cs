using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using WildRP.AMVTool.Autoloads;
using WildRP.AMVTool.BVH;

namespace WildRP.AMVTool;

public partial class AmvProbe : MeshInstance3D
{
	[Export] private bool _drawDebugLines = false;
	[Export] private PackedScene _debugLine;

	[Export] private bool _debugPosition = false;
	[Export] private Label3D _receivedPosLabel;
	[Export] private Label3D _requestedPosLabel;
	
	private uint _rayMask = 1;
	private float _maxDistance = 50;
	private ProbeSample _value = new();
	private int _samples = 0;
	private Vector3 _variance;

	public Vector3I CellPosition
	{
		get;
		set;
	} = Vector3I.Zero;
	
	public AmbientMaskVolume ParentVolume { get; set; }

	public void Reset()
	{
		_value = 0;
		_samples = 0;
	}
	
	public override void _Ready()
	{
		SetInstanceShaderParameter("positive_occlusion", Vector3.One * 0.5f);
		SetInstanceShaderParameter("negative_occlusion", Vector3.One * 0.5f);

		if (_debugPosition)
		{
			_receivedPosLabel.Text = CellPosition.ToString();
			_requestedPosLabel.Text = ParentVolume.PositionTest(CellPosition).ToString();
		}
		
		_variance = ParentVolume.Size / ParentVolume.ProbeCount / 2;
	}

	public void CaptureSample()
	{
		ProbeSample sample;
		
		sample.X.Negative = RayHit(Vector3.Left) / AmvBaker.GetSampleCount();
		sample.X.Positive = RayHit(Vector3.Right) / AmvBaker.GetSampleCount() ;
		sample.Y.Negative = RayHit(Vector3.Down) / AmvBaker.GetSampleCount();
		sample.Y.Positive = RayHit(Vector3.Up) / AmvBaker.GetSampleCount();
		sample.Z.Negative = RayHit(Vector3.Forward) / AmvBaker.GetSampleCount();
		sample.Z.Positive = RayHit(Vector3.Back) / AmvBaker.GetSampleCount();

		_value += sample;
		_samples++;
	}

	public void UpdateAverage(bool bakeFinished = false)
	{
		if (_samples == 0)
		{
			_value = 0;
			SetInstanceShaderParameter("positive_occlusion", Vector3.One * .5f);
			SetInstanceShaderParameter("negative_occlusion", Vector3.One * .5f);
			return;
		}

		var completion = (float)_samples / AmvBaker.GetSampleCount();
		var dispValue = _value * (1/completion);

		dispValue.Remap(0,1, Settings.MinBrightness, 1);
		
		if (bakeFinished)
			_value = dispValue;
		
		SetInstanceShaderParameter("positive_occlusion", dispValue.GetPositiveVector());
		SetInstanceShaderParameter("negative_occlusion", dispValue.GetNegativeVector());
		
	}
	
	public ProbeSample GetValue()
	{
		return _value;
	}

	private Vector3 SampleHemisphere(Vector3 norm, float alpha = 0.0f)
	{
		Vector4 rand = new Vector4(GD.Randf(), GD.Randf(), GD.Randf(), GD.Randf());
		float r = Mathf.Pow(rand.W, 1.0f / (1.0f + alpha));
		float angle = rand.Y * Mathf.Tau;
		float sr = Mathf.Sqrt(1.0f - r * r);
		Vector3 ph = new Vector3(sr * Mathf.Cos(angle), sr * Mathf.Sin(angle), r);
		Vector3 tangent = (new Vector3(rand.Z, rand.Y, rand.X) + new Vector3(GD.Randf(), GD.Randf(), GD.Randf()) - Vector3.One).Normalized();
		Vector3 bitangent = norm.Cross(tangent);
		tangent = norm.Cross(bitangent);
		return ph * new Basis(tangent, bitangent, norm);
	}
	
	private float RayHit(Vector3 dir)
	{
		var d = SampleHemisphere(dir).Normalized();

		var randDir = new Vector3((float)GD.Randfn(1, 0), (float)GD.Randfn(1, 0), (float)GD.Randfn(1, 0)).Normalized();

		var maxDist = (randDir * _variance).Length();
		var varianceHit = Raycast(GlobalPosition, randDir, out var hitDist, maxDist);
		if (varianceHit) // Extra check so we don't move inside walls for this
		{
			randDir = randDir * hitDist * 0.5f;
		}
			
		
		var hit = Raycast(GlobalPosition+randDir, d, out float amvHitDistance, _maxDistance);

		if (_drawDebugLines)
		{
			var l = _debugLine.Instantiate() as Node3D;
			AddChild(l);
			
			l.LookAt(GlobalPosition + d);

			var scale = l.Scale;

			scale.Z = hit ? amvHitDistance : _maxDistance;

			l.Scale = scale;
		}

		// TODO: Far away walls should affect AO less?
		/*if (hit != null)
		{
			var dist = GlobalPosition.DistanceTo(hit.Position);
			var distFactor = Mathf.Pow(dist / _maxDistance, 1f);
			return distFactor;
		}*/

		return hit ? 0 : 1;
	}

	private static bool Raycast(Vector3 from, Vector3 dir, out float t, float maxDist = float.PositiveInfinity)
	{
		t = -1;
		var lowestT = float.PositiveInfinity-1;
		var hit = false;

		foreach (var bvh in AmvBaker.Instance.GetBvhList())
		{
			if (bvh.Raycast(from, dir, out float lowt) && lowt < lowestT)
			{
				lowestT = lowt;
				hit = true;
			}
		}

		return hit;
	}
}

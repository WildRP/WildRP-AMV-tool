using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

namespace WildRP.AMVTool;

public partial class AmvProbe : MeshInstance3D
{
	[Export] private bool _drawDebugLines = false;
	[Export] private PackedScene _debugLine;
	
	private uint _rayMask = 1;
	private float _maxDistance = 50;
	private ProbeSample _averageValue = new();
	private readonly List<ProbeSample> _samples = [];

	public Vector3I BoundsPosition
	{
		get;
		set;
	} = Vector3I.Zero;

	public void Reset()
	{
		_samples.Clear();
	}

	public override void _Ready()
	{
		SetInstanceShaderParameter("positive_occlusion", Vector3.One * 0.5f);
		SetInstanceShaderParameter("negative_occlusion", Vector3.One * 0.5f);
	}

	public void CaptureSample()
	{
		ProbeSample sample;
		
		sample.X.Negative = RayHit(Vector3.Left);
		sample.X.Positive = RayHit(Vector3.Right);
		sample.Y.Negative = RayHit(Vector3.Down);
		sample.Y.Positive = RayHit(Vector3.Up);
		sample.Z.Negative = RayHit(Vector3.Forward);
		sample.Z.Positive = RayHit(Vector3.Back);
		
		_samples.Add(sample);
	}

	public void UpdateAverage()
	{
		if (_samples.Count == 0)
		{
			_averageValue = 1.0f;
			return;
		}
		_averageValue = _samples.Aggregate((current, sample) => current + sample) / _samples.Count;
		
		SetInstanceShaderParameter("positive_occlusion", _averageValue.GetPositiveVector());
		SetInstanceShaderParameter("negative_occlusion", _averageValue.GetNegativeVector());
		
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
		
		var hit = Raycast(GlobalPosition, d * _maxDistance, this, _rayMask);

		if (_drawDebugLines)
		{
			var l = _debugLine.Instantiate() as Node3D;
			AddChild(l);
			
			l.LookAt(GlobalPosition + d);

			var scale = l.Scale;

			if (hit != null)
				scale.Z = GlobalPosition.DistanceTo(hit.Position);
			else
				scale.Z = _maxDistance;

			l.Scale = scale;
		}

		// TODO: Far away walls should affect AO less
		/*if (hit != null)
		{
			var dist = GlobalPosition.DistanceTo(hit.Position);
			var distFactor = Mathf.Pow(dist / _maxDistance, 1f);
			return distFactor;
		}*/

		return hit == null ? 1 : 0;
	}
	
	private static PhysicsDirectSpaceState3D _spaceState;
	private static ulong _lastRaycastFrame = 0;

	private static RaycastHit Raycast(Vector3 from, Vector3 dirLength, Node3D caster, uint mask)
	{
		if (_lastRaycastFrame != Engine.GetPhysicsFrames() || Engine.GetPhysicsFrames() == 0)
			_spaceState = caster.GetWorld3D().DirectSpaceState;

		_lastRaycastFrame = Engine.GetPhysicsFrames();
		var to = from + dirLength;
		var query = PhysicsRayQueryParameters3D.Create(from, to, mask);
		query.HitFromInside = true;
		var d = _spaceState.IntersectRay(query);
		
		return d.Count == 0 ? null : new RaycastHit(d);
	}

	// I made this because the syntax of getting this out of the dictionary every time SUCKS
	public class RaycastHit(Dictionary d)
	{
		public Node Collider = d["collider"].As<Node>();
		public Vector3 Normal = d["normal"].AsVector3();
		public Vector3 Position = d["position"].AsVector3();
		public int FaceIndex = d["face_index"].AsInt32();
		public Rid Rid = d["rid"].AsRid();
	}
}

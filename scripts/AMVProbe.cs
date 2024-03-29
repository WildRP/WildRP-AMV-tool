using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;

namespace WildRP.AMVTool;

public partial class AMVProbe : Node3D
{
	private uint _rayMask;
	private float _maxDistance = 150f;
	private ProbeSample _averageValue = new();
	private List<ProbeSample> _samples = new ();

	public override void _Ready()
	{
		for (int i = 0; i < 10; i++)
		{
			CaptureSample();
		}
		UpdateAverage();
		GD.Print(_averageValue);
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

	private void UpdateAverage()
	{
		if (_samples.Count == 0)
		{
			_averageValue = 0;
			return;
		}
		_averageValue = _samples.Aggregate((current, sample) => current + sample) / _samples.Count;
	}

	Vector3 SampleHemisphere(Vector3 norm, float alpha = 0.0f)
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
	
	private int RayHit(Vector3 dir)
	{
		var d = SampleHemisphere(dir).Normalized();
		var hit = Raycast(GlobalPosition, d * _maxDistance, this, _rayMask);
		return hit != null ? 1 : 0;
	}
	
	private static PhysicsDirectSpaceState3D _spaceState;
	private static ulong _lastRaycastFrame = 0;
	public static RaycastHit Raycast(Vector3 from, Vector3 dirLength, Node3D caster, uint mask)
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
	public class RaycastHit
	{
		public Node Collider;
		public Vector3 Normal;
		public Vector3 Position;
		public int FaceIndex;
		public Rid Rid;
		public RaycastHit(Dictionary d)
		{
			Collider = d["collider"].As<Node>();
			Normal = d["normal"].AsVector3();
			Position = d["position"].AsVector3();
			FaceIndex = d["face_index"].AsInt32();
			Rid = d["rid"].AsRid();
		}
	}
}

using Godot;
using System.Collections.Generic;
using System.Linq;
using Godot.Collections;
using WildRP.AMVTool.Autoloads;

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
	private int _samples = 0;
	private Vector3 _variance;

	private ProbeSample _value = new();
	private ProbeSample _blurredSample = new();
	
	
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
	}

	public void CaptureSample()
	{
		ProbeSample sample;
		
		_variance = ParentVolume.Size / ParentVolume.ProbeCount / 2;
		_variance *= .9f;
		
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

	public void Blur(Vector3I Axis)
	{
		float[] weights = [0.0625f, 0.25f, 0.375f, 0.25f, 0.0625f];

		_blurredSample = 0;
		
		for (int i = 0; i < 5; i++)
		{
			int offset = i - 2;
			_blurredSample += ParentVolume.GetCellValueRelative(CellPosition, Axis * offset) * weights[i];
		}
	}

	public void SetValueFromBlurred()
	{
		_value = _blurredSample;
		SetInstanceShaderParameter("positive_occlusion", _value.GetPositiveVector());
		SetInstanceShaderParameter("negative_occlusion", _value.GetNegativeVector());
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
		var d = SampleHemisphere(ToGlobal(dir)).Normalized();

		var randDir = new Vector3((float)GD.Randfn(1, 0), (float)GD.Randfn(1, 0), (float)GD.Randfn(1, 0)).Normalized();

		randDir *= _variance;
		var varianceHit = Raycast(GlobalPosition, randDir, this, _rayMask);
		if (varianceHit != null) // Extra check so we don't move inside walls for this
		{
			var dist = varianceHit.Position.DistanceTo(GlobalPosition);
			randDir = randDir.Normalized() * dist * 0.8f;
		}
			
		
		var hit = Raycast(GlobalPosition+randDir, d * _maxDistance, this, _rayMask);

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


		float contribution = 1f;
		var foundSky = hit == null; // broke out of the interior or whatever we're baking
		
		if (hit != null && AmvBaker.BounceCount > 0) // this is gonna really slow shit down but it makes lighting look nice i hope
		{
			var lastHitPos = hit.Position;
			var bounceDir = d.Bounce(hit.Normal.Normalized());
			for (int i = 0; i < AmvBaker.BounceCount; i++)
			{
				contribution *= AmvBaker.BounceEnergy;
				var bounce = Raycast(lastHitPos + bounceDir * 0.01f, bounceDir * _maxDistance, this, _rayMask);
				if (bounce == null)
				{
					foundSky = true;
					break;
				}

				lastHitPos = bounce.Position;
				bounceDir = bounceDir.Bounce(bounce.Normal.Normalized());
			}
		}

		return foundSky ? contribution : 0;
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

using System;
using System.Text.Json.Serialization;
using Godot;

namespace WildRP.AMVTool;

public struct ProbeSample(SampleAxis x, SampleAxis y, SampleAxis z)
{
    [JsonInclude]
    public SampleAxis X = x, Y = y, Z = z;

    public ProbeSample() : this(0, 0, 0)
    {
    }
    
    public ProbeSample(double val) : this(val, val, val)
    {}

    // For quickly feeding these into the shader
    public Vector3 GetPositiveVector()
    {
        return new Vector3( (float) X.Positive, (float) Y.Positive, (float) Z.Positive);
    }
    
    public Vector3 GetNegativeVector()
    {
        return new Vector3( (float) X.Negative, (float)  Y.Negative, (float)  Z.Negative);
    }

    public void Remap(double inFrom, double inTo, double outFrom, double outTo)
    {
        X.Remap(inFrom, inTo, outFrom, outTo);
        Y.Remap(inFrom, inTo, outFrom, outTo);
        Z.Remap(inFrom, inTo, outFrom, outTo);
    }
    
    public static ProbeSample operator +(ProbeSample a) => a;
    public static ProbeSample operator -(ProbeSample a) => new(-a.X, -a.Y, -a.Z);

    public static ProbeSample operator +(ProbeSample a, ProbeSample b) =>
        new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static ProbeSample operator -(ProbeSample a, ProbeSample b) =>
        new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static ProbeSample operator *(ProbeSample a, ProbeSample b) =>
        new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    
    public static ProbeSample operator /(ProbeSample a, ProbeSample b)
    {
        if (b.X == 0 || b.Y == 0 || b.Z == 0)
            throw new DivideByZeroException();
        
        return new ProbeSample(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }
    
    // Float operators
    public static ProbeSample operator +(ProbeSample a, double b) =>
        new(a.X + b, a.Y + b, a.Z + b);

    public static ProbeSample operator -(ProbeSample a, double b) =>
        new(a.X - b, a.Y - b, a.Z - b);
    
    public static ProbeSample operator *(ProbeSample a, double b) => new(a.X * b, a.Y * b, a.Z * b);
    
    public static ProbeSample operator /(ProbeSample a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException();
        
        return new ProbeSample(a.X / b, a.Y / b, a.Z / b);
    }

    public static implicit operator ProbeSample(double f) => new(f, f, f);
    
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}

public struct SampleAxis(double positive, double negative)
{
    [JsonInclude]
    public double Positive = positive, Negative = negative;

    public SampleAxis() : this(0, 0)
    {
    }

    public void Remap(double inFrom, double inTo, double outFrom, double outTo)
    {
        Positive = Mathf.Remap(Positive, inFrom, inTo, outFrom, outTo);
        Negative = Mathf.Remap(Negative, inFrom, inTo, outFrom, outTo);
    }

    public static SampleAxis operator +(SampleAxis a) => a;
    public static SampleAxis operator -(SampleAxis a) => new(-a.Positive, -a.Negative);

    public static SampleAxis operator +(SampleAxis a, SampleAxis b) =>
        new(a.Positive + b.Positive, a.Negative + b.Negative);

    public static SampleAxis operator -(SampleAxis a, SampleAxis b) =>
        new(a.Positive - b.Positive, a.Negative - b.Negative);

    public static SampleAxis operator *(SampleAxis a, SampleAxis b) =>
        new(a.Positive * b.Positive, a.Negative * b.Negative);
    
    public static SampleAxis operator /(SampleAxis a, SampleAxis b)
    {
        if (b.Positive == 0 || b.Negative == 0)
            throw new DivideByZeroException();
        
        return new SampleAxis(a.Positive / b.Positive, a.Negative / b.Negative);
    }

    public static bool operator ==(SampleAxis a, SampleAxis b) =>
        a.Positive == b.Positive && a.Negative == b.Negative;

    public static bool operator !=(SampleAxis a, SampleAxis b)
    {
        return !(a == b);
    }

    // Float operators
    public static SampleAxis operator +(SampleAxis a, double b) =>
        new(a.Positive + b, a.Negative + b);

    public static SampleAxis operator -(SampleAxis a, double b) =>
        new(a.Positive - b, a.Negative - b);
    
    public static SampleAxis operator *(SampleAxis a, double b) => new(a.Positive * b, a.Negative * b);
    
    public static SampleAxis operator /(SampleAxis a, double b)
    {
        if (b == 0)
            throw new DivideByZeroException();
        
        return new SampleAxis(a.Positive / b, a.Negative / b);
    }

    public static implicit operator SampleAxis(double f) => new(f, f);
    
    public override string ToString()
    {
        return $"({Positive}, {Negative})";
    }
}


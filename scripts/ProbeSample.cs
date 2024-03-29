using System;

namespace WildRP.AMVTool;

public struct ProbeSample
{
    public SampleAxis X, Y, Z;

    public ProbeSample(SampleAxis x, SampleAxis y, SampleAxis z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    
    public ProbeSample()
    {
        X = 0;
        Y = 0;
        Z = 0;
    }
    
    public static ProbeSample operator +(ProbeSample a) => a;
    public static ProbeSample operator -(ProbeSample a) => new ProbeSample(-a.X, -a.Y, -a.Z);

    public static ProbeSample operator +(ProbeSample a, ProbeSample b) =>
        new ProbeSample(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static ProbeSample operator -(ProbeSample a, ProbeSample b) =>
        new ProbeSample(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static ProbeSample operator *(ProbeSample a, ProbeSample b) =>
        new ProbeSample(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    
    public static ProbeSample operator /(ProbeSample a, ProbeSample b)
    {
        if (b.X == 0 || b.Y == 0 || b.Z == 0)
            throw new DivideByZeroException();
        
        return new ProbeSample(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
    }
    
    // Float operators
    public static ProbeSample operator +(ProbeSample a, float b) =>
        new ProbeSample(a.X + b, a.Y + b, a.Z + b);

    public static ProbeSample operator -(ProbeSample a, float b) =>
        new ProbeSample(a.X - b, a.Y - b, a.Z - b);
    
    public static ProbeSample operator *(ProbeSample a, float b) => new ProbeSample(a.X * b, a.Y * b, a.Z * b);
    
    public static ProbeSample operator /(ProbeSample a, float b)
    {
        if (b == 0)
            throw new DivideByZeroException();
        
        return new ProbeSample(a.X / b, a.Y / b, a.Z / b);
    }

    public static implicit operator ProbeSample(float f) => new ProbeSample(f, f, f);
    
    public override string ToString()
    {
        return $"({X}, {Y}, {Z})";
    }
}

public struct SampleAxis
{
    public float Positive, Negative;

    public SampleAxis(float positive, float negative)
    {
        Positive = positive;
        Negative = negative;
    }

    public SampleAxis()
    {
        Positive = 0;
        Negative = 0;
    }

    public static SampleAxis operator +(SampleAxis a) => a;
    public static SampleAxis operator -(SampleAxis a) => new SampleAxis(-a.Positive, -a.Negative);

    public static SampleAxis operator +(SampleAxis a, SampleAxis b) =>
        new SampleAxis(a.Positive + b.Positive, a.Negative + b.Negative);

    public static SampleAxis operator -(SampleAxis a, SampleAxis b) =>
        new SampleAxis(a.Positive - b.Positive, a.Negative - b.Negative);

    public static SampleAxis operator *(SampleAxis a, SampleAxis b) =>
        new SampleAxis(a.Positive * b.Positive, a.Negative * b.Negative);
    
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
    public static SampleAxis operator +(SampleAxis a, float b) =>
        new SampleAxis(a.Positive + b, a.Negative + b);

    public static SampleAxis operator -(SampleAxis a, float b) =>
        new SampleAxis(a.Positive - b, a.Negative - b);
    
    public static SampleAxis operator *(SampleAxis a, float b) => new SampleAxis(a.Positive * b, a.Negative * b);
    
    public static SampleAxis operator /(SampleAxis a, float b)
    {
        if (b == 0)
            throw new DivideByZeroException();
        
        return new SampleAxis(a.Positive / b, a.Negative / b);
    }

    public static implicit operator SampleAxis(float f) => new SampleAxis(f, f);
    
    public override string ToString()
    {
        return $"({Positive}, {Negative})";
    }
}


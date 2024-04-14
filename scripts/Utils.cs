using System.Collections.Generic;
using Godot;

namespace WildRP.AMVTool;

public static class Utils
{
    // this is recursive hehehe sorry not sorry
    public static void GetAllChildren(Node parent, List<Node> nodes, int depth = 0)
    {
        if (depth > 5) return; // ok we have gone too deep. this model is too silly.
		
        var children = parent.GetChildren();
        nodes.AddRange(children);
        foreach (var n in children)
        {
            GetAllChildren(n, nodes, depth + 1);
        }
    }

    // These two functions are from Codewalker and equivalent to RDR2/GTAV hash generators
    public static uint JenkinsHash(string text)
    {
        if (text == null) return 0;
        uint h = 0;
        for (int i = 0; i < text.Length; i++)
        {
            h += (byte)text[i];
            h += (h << 10);
            h ^= (h >> 6);
        }
        h += (h << 3);
        h ^= (h >> 11);
        h += (h << 15);

        return h;
    }

    public static int SignedJenkins(string text)
    {
        return unchecked((int)JenkinsHash(text));
    }
    
    // Input is first half of UUID, second half of UUID, and Jenkins has of interior name
    public static uint LightProbeHash(uint[] ints, uint seed = 0)
    {
        var a2 = ints.Length;
        var v3 = a2;
        var v5 = (uint)(seed + 0xDEADBEEF + 4 * ints.Length);
        var v6 = v5;
        var v7 = v5;

        var c = 0;
        if (ints.Length > 3)
        {
            for (var i = 0; i < (ints.Length - 4) / 3 + 1; i++, v3 -= 3, c += 3)
            {
                var v9 = ints[c + 2] + v5;
                var v10 = ints[c + 1] + v6;
                var v11 = ints[c] - v9;
                var v13 = v10 + v9;
                var v14 = (v7 + v11) ^ BitUtil.RotateLeft(v9, 4);
                var v15 = v10 - v14;
                var v17 = v13 + v14;
                var v18 = v15 ^ BitUtil.RotateLeft(v14, 6);
                var v19 = v13 - v18;
                var v21 = v17 + v18;
                var v22 = v19 ^ BitUtil.RotateLeft(v18, 8);
                var v23 = v17 - v22;
                var v25 = v21 + v22;
                var v26 = v23 ^ BitUtil.RotateLeft(v22, 16);
                var v27 = v21 - v26;
                var v29 = v27 ^ BitUtil.RotateRight(v26, 13);
                var v30 = v25 - v29;
                v7 = v25 + v26;
                v6 = v7 + v29;
                v5 = v30 ^ BitUtil.RotateLeft(v29, 4);
            }
        }

        if (v3 == 3)
        {
            v5 += ints[c + 2];
        }

        if (v3 >= 2)
        {
            v6 += ints[c + 1];
        }

        if (v3 >= 1)
        {
            var v34 = (v6 ^ v5) - BitUtil.RotateLeft(v6, 14);
            var v35 = (v34 ^ (v7 + ints[c])) - BitUtil.RotateLeft(v34, 11);
            var v36 = (v35 ^ v6) - BitUtil.RotateRight(v35, 7);
            var v37 = (v36 ^ v34) - BitUtil.RotateLeft(v36, 16);
            var v38 = BitUtil.RotateLeft(v37, 4);
            var v39 = (((v35 ^ v37) - v38) ^ v36) - BitUtil.RotateLeft((v35 ^ v37) - v38, 14);
            return (v39 ^ v37) - BitUtil.RotateRight(v39, 8);
        }

        return v5;
    }

    private static class BitUtil
    {
        public static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
        public static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }
    }

    public static Aabb GlobalAabb(this VisualInstance3D v)
    {
        return v.GlobalTransform * v.GetAabb();
    }

    public static Color ToColor(this Vector3 v)
    {
        return new Color(v.X, v.Y, v.Z);
    }

    public static Vector3 ToVector(this Color c)
    {
        return new Vector3(c.R, c.G, c.B);
    }
    
}
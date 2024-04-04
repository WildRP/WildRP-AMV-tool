using System.Collections.Generic;
using System.Linq;
using Godot;

namespace WildRP.AMVTool.BVH;

// A tree structure that holds triangles for quick traversal
public class BoundingVolumeHierarchy
{
    public bool Enabled { get; set; } = true;
    private BvhNode _rootNode;
    private Basis _modelBasis;

    public BoundingVolumeHierarchy(Vector3[] tris, Aabb bounds, Basis modelBasis)
    {
        _rootNode = new BvhNode(bounds);
        
        _modelBasis = modelBasis;

        GD.Print($"Making a BVH with bounds {bounds} - starting with {tris.Length} triangles:");
        
        for (int i = 0; i < tris.Length; i += 3) // add all the tris to root node
        {
            var t = new Triangle(tris[i], tris[i + 1], tris[i + 2]);
            _rootNode.Triangles.Add(t);
        }
        
        _rootNode.Split();
        GD.Print($"Finished creating BVH - it has {_rootNode.TriangleCount} triangles");
    }

    public bool Raycast(Vector3 worldOrigin, Vector3 worldDir, out float t)
    {
        if (Enabled == false)
        {
            GD.Print("I do a raycast");
            t = -1;
            return false;
        }
        
        var ray = new Ray( _modelBasis.Inverse() * worldOrigin,  worldDir);
        
        GD.Print($"{_rootNode.Bounds.HasPoint(_modelBasis.Inverse() * worldOrigin)}");
        
        return _rootNode.Raycast(ray, out t);
    }
}

public class Ray(Vector3 origin, Vector3 dir)
{
    public Vector3 Origin = origin;
    public Vector3 Normal = dir;
}

public class BvhNode(Aabb bounds)
{
    public static int MaxDepth = 2;
    
    public Aabb Bounds = bounds;
    public List<BvhNode> Children = null;
    public List<Triangle> Triangles = [];
    
    public bool IsLeaf => Children == null && Triangles != null;

    // I think there's a good chance this might not return the closest triangle?
    // But if we're only checking if it intersects at _all_ that's fine
    public bool Raycast(Ray ray, out float t)
    {
        if (Bounds.Intersects(ray, out t) == false)
            return false; // not in the bounds

        if (Children != null) // Check if the children of this node has the triangle we wanna hit
        {
            foreach (var child in Children)
            {
                if (child.Raycast(ray, out t))
                    return true;
            }
        }

        if (Triangles != null)
        {
            foreach (var triangle in Triangles)
            {
                if (triangle.Intersects(ray, out t))
                    return true;
            }
        }

        return false;
    }

    public int TriangleCount
    {
        get
        {
            int totalCount = 0;
            if (Children != null)
            {
                foreach (var node in Children)
                {
                    totalCount += node.TriangleCount;
                }
            }
            else if (Triangles != null)
            {
                totalCount += Triangles.Count;
            }

            return totalCount;
        }
    }
    
    private void Shake()
    {
        if (Children == null) return;
        
        for (int i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].TriangleCount == 0)
            {
                Children.RemoveAt(i);
            }
        }

        foreach (var node in Children)
        {
            node.Shake();
        }
    }
    
    public void Split()
    {
        Split(0);
        //Shake();
    }

    private static int NodeID = 0;
    private void Split(int depth)
    {
        
        if (IsLeaf && Triangles.Count > 0 && depth < MaxDepth)
        {
            Children = new List<BvhNode>();
            var center = Bounds.GetCenter();
            var extent = Bounds.Size/2;

            var TFL = center + new Vector3(-extent.X, +extent.Y, -extent.Z);
            var TFR = center + new Vector3(+extent.X, +extent.Y, -extent.Z);
            var TBL = center + new Vector3(-extent.X, +extent.Y, +extent.Z);
            var TBR = center + new Vector3(+extent.X, +extent.Y, +extent.Z);
            var BFL = center + new Vector3(-extent.X, -extent.Y, -extent.Z);
            var BFR = center + new Vector3(+extent.X, -extent.Y, -extent.Z);
            var BBL = center + new Vector3(-extent.X, -extent.Y, +extent.Z);
            var BBR = center + new Vector3(+extent.X, -extent.Y, +extent.Z);

            Children.Add(new BvhNode(BoundsFromPos(TFL, center)));
            Children.Add(new BvhNode(BoundsFromPos(TFR, center)));
            Children.Add(new BvhNode(BoundsFromPos(TBL, center)));
            Children.Add(new BvhNode(BoundsFromPos(TBR, center)));
            Children.Add(new BvhNode(BoundsFromPos(BFL, center)));
            Children.Add(new BvhNode(BoundsFromPos(BFR, center)));
            Children.Add(new BvhNode(BoundsFromPos(BBL, center)));
            Children.Add(new BvhNode(BoundsFromPos(BBR, center)));
        }

        // Successfully split - we have children and are no longer a leaf node
        // Assign triangles to children and clear them from this node
        if (Triangles != null && Children != null)
        {
            foreach (var node in Children)
            {
                foreach (var triangle in Triangles)
                {
                    if (node.Bounds.Intersects(triangle))
                        node.Triangles.Add(triangle);
                }
            }
            Triangles.Clear();
            Triangles = null;
        }

        if (Children != null)
        {
            foreach (var node in Children)
            {
                node.Split(depth+1);
            }
        }
        
    }
    
    private static Aabb BoundsFromPos(Vector3 p1, Vector3 p2)
    {
        var bounds = new Aabb
        {
            Position = p1,
            End = p2
        };

        if (bounds.Size != p2 - p1) GD.Print($"{bounds.Size} - {p1 - p2}");
        
        return bounds.Abs();
    }
    
}

public class Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
{
    public Vector3 V0 = v0, V1 = v1, V2 = v2;
    public Vector3 Centroid = (v0 + v1 + v2) / 3;
    public readonly float Distance =  ((v0 + v1 + v2) / 3).Length();
    public Vector3 Normal = (v1 - v0).Cross(v2 - v1).Normalized();

    public bool Intersects(Ray ray, out float t)
    {
        var hit = Geometry3D.RayIntersectsTriangle(ray.Origin, ray.Normal, V0, V1, V2);

        if (hit.VariantType == Variant.Type.Nil)
        {
            t = -1;
            return false;
        }

        t = hit.AsSingle();
        return true;
    }
}

public static class Collisions
{
    // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter3/raycast_aabb.html
    public static bool Intersects(this Aabb bounds, Ray ray, out float t)
    {
        float t1 = (bounds.Position.X - ray.Origin.X) / ray.Normal.X;
        float t2 = (bounds.End.X - ray.Origin.X) / ray.Normal.X;
        float t3 = (bounds.Position.Y - ray.Origin.Y) / ray.Normal.Y;
        float t4 = (bounds.End.Y - ray.Origin.Y) / ray.Normal.Y;
        float t5 = (bounds.Position.Z - ray.Origin.Z) / ray.Normal.Z;
        float t6 = (bounds.End.Z - ray.Origin.Z) / ray.Normal.Z;

        float tmin = Mathf.Max(Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4)), Mathf.Min(t5, t6));
        float tmax = Mathf.Min(Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4)), Mathf.Max(t5, t6));

        t = -1;
        
        // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
        if (tmax < 0) {
            return false;
        }

        // if tmin > tmax, ray doesn't intersect AABB
        if (tmin > tmax)
        {
            return false;
        }

        if (tmin < 0f)
        {
            t = tmax;
            return true;
        }

        t = tmin;
        return true;
    }
    
    // https://gdbooks.gitbooks.io/3dcollisions/content/Chapter4/aabb-triangle.html
    public static bool Intersects(this Aabb bounds, Triangle tri)
    {
        Vector3 c = bounds.GetCenter();
        Vector3 e = bounds.Size;
        
        tri.V0 -= c;
        tri.V1 -= c;
        tri.V2 -= c;

        // Triangle edges
        Vector3 f0 = tri.V1 - tri.V0;
        Vector3 f1 = tri.V2 - tri.V1;
        Vector3 f2 = tri.V0 - tri.V2;
        
        // aabb face normals
        Vector3 u0 = new Vector3(1, 0, 0);
        Vector3 u1 = new Vector3(0, 1, 0);
        Vector3 u2 = new Vector3(0, 0, 1);
        
        // oh boy math - 9 axes we have to test

        Vector3[] axes =
        [
            u0.Cross(f0), u0.Cross(f1), u0.Cross(f2),
            u1.Cross(f0), u1.Cross(f1), u2.Cross(f2),
            u2.Cross(f0), u2.Cross(f1), u2.Cross(f2)
        ];
        
        // Testing axis: axis_u0_f0
        foreach (var a in axes)
        {
            if (CheckAxis(a, tri, e, u0, u1, u2))
                return false;
        }
        
        if (CheckAxis(u0, tri, e, u0, u1, u2))
            return false;
        
        if (CheckAxis(u1, tri, e, u0, u1, u2))
            return false;
        
        if (CheckAxis(u2, tri, e, u0, u1, u2))
            return false;
        
        // Triangle normal
        if (CheckAxis(f0.Cross(f1), tri, e, u0, u1, u2))
            return false;

        return true; // intersection!
    }

    private static bool CheckAxis(Vector3 axis, Triangle t, Vector3 e, Vector3 u0, Vector3 u1, Vector3 u2)
    {
        // Project all 3 vertices of the triangle onto the Seperating axis
        float p0 = t.V0.Dot(axis);
        float p1 = t.V1.Dot(axis);
        float p2 = t.V2.Dot(axis);
        
        float r = e.X * Mathf.Abs(u0.Dot(axis)) +
                  e.Y * Mathf.Abs(u1.Dot(axis)) +
                  e.Z * Mathf.Abs(u2.Dot(axis));

        return Mathf.Max(-Max(p0, p1, p2), Min(p0, p1, p2)) > r;
    }

    private static float Min(float a, float b, float c)
    {
        return Mathf.Min(a, Mathf.Min(b, c));
    }
    
    private static float Max(float a, float b, float c)
    {
        return Mathf.Max(a, Mathf.Max(b, c));
    }
}


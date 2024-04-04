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

    public BoundingVolumeHierarchy(Vector3[] tris, Basis modelBasis)
    {

        Aabb bounds = new Aabb();
        foreach (var p in tris)
        {
            bounds = bounds.Expand(p);
        }
        
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
            var extent = Bounds.Size;

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

    public Vector3[] Vertices => new[] { V0, V1, V1 };

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
    
    public static bool Intersects(this Aabb box, Triangle triangle)
    {
        float triangleMin, triangleMax;
        float boxMin, boxMax;

        // Test the box normals (x-, y- and z-axes)
        var boxNormals = new Vector3[] {
            new Vector3(1,0,0),
            new Vector3(0,1,0),
            new Vector3(0,0,1)
        };
        for (int i = 0; i < 3; i++)
        {
            Project(triangle.Vertices, boxNormals[i], out triangleMin, out triangleMax);
            if (triangleMax < box.Position[i] || triangleMin > box.End[i])
                return false; // No intersection possible.
        }

        // Test the triangle normal
        float triangleOffset = triangle.Normal.Dot(triangle.V0);
        Project(box.Vertices(), triangle.Normal, out boxMin, out boxMax);
        if (boxMax < triangleOffset || boxMin > triangleOffset)
            return false; // No intersection possible.

        // Test the nine edge cross-products
        Vector3[] triangleEdges = new Vector3[] {
            triangle.V0 - triangle.V1,
            triangle.V1 - triangle.V2,
            triangle.V2 - triangle.V0
        };
        for (int i = 0; i < 3; i++)
        for (int j = 0; j < 3; j++)
        {
            // The box normals are the same as it's edge tangents
            Vector3 axis = triangleEdges[i].Cross(boxNormals[j]);
            Project(box.Vertices(), axis, out boxMin, out boxMax);
            Project(triangle.Vertices, axis, out triangleMin, out triangleMax);
            if (boxMax <= triangleMin || boxMin >= triangleMax)
                return false; // No intersection possible
        }

        // No separating axis found.
        return true;
    }

    static void Project(Vector3[] points, Vector3 axis, out float min, out float max)
    {
        min = float.PositiveInfinity;
        max = float.NegativeInfinity;
        
        foreach (var p in points)
        {
            float val = axis.Dot(p);
            if (val < min) min = val;
            if (val > max) max = val;
        }
    }

    static Vector3[] Vertices(this Aabb aabb)
    {
        var v = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            v[i] = aabb.GetEndpoint(i);
        }

        return v;
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


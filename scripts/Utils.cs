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
}
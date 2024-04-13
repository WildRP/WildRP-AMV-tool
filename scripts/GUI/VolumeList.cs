using System;
using Godot;

namespace WildRP.AMVTool.GUI;

public partial class VolumeList : ItemList
{
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Right } mouseButton)
        {
            var pos = mouseButton.Position;
            var idx = GetItemAtPosition(pos, true);
            if (idx == -1) return;
            
            OnRightClickItem?.Invoke(GetItemText(idx), GlobalPosition + pos);
        }
    }
    
    public event Action<string, Vector2> OnRightClickItem;
}
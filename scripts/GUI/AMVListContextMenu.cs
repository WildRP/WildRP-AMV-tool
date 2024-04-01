using Godot;
using System;
using WildRP.AMVTool;

public partial class AMVListContextMenu : PopupMenu
{
	private AmbientMaskVolume _selectedVolume;

	public override void _Ready()
	{
		this.IndexPressed += ItemPressed;
	}

	public void Select(string name)
	{
		_selectedVolume = AmvBaker.Instance.GetVolume(name);
		SetItemChecked(1, _selectedVolume.IncludeInFullBake);
	}

	void ItemPressed(long idx)
	{
		switch (idx)
		{
			case 0: // Trigger bake
				// NOT IMPLEMENTED
				break;
			case 1: // Include in full bake
				_selectedVolume.IncludeInFullBake = !_selectedVolume.IncludeInFullBake;
				break;
			// Index 4 is a separator item and not clickable
			case 3: // Rename
				// NOT IMPLEMENTED
				break;
			case 4: // Delete
				_selectedVolume.Delete();
				break;
		}
	}
}

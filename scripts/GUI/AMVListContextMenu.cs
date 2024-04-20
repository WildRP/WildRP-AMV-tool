using Godot;
using WildRP.AMVTool;
using WildRP.AMVTool.GUI;

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
			case 0: // Include in full bake
				_selectedVolume.IncludeInFullBake = !_selectedVolume.IncludeInFullBake;
				break;
			// Index 1 is a separator item and not clickable
			case 2: // Rename
				RenamePopup.Instance.Trigger(_selectedVolume);
				break;
			case 3: // Delete
				_selectedVolume.Delete();
				break;
		}
	}
}

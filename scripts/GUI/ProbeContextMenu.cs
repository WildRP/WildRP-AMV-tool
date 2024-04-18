using Godot;

namespace WildRP.AMVTool.GUI;

public partial class ProbeContextMenu : PopupMenu
{
	private DeferredProbe _selectedProbe;

	public override void _Ready()
	{
		this.IndexPressed += ItemPressed;
	}

	public void Select(string name)
	{
		_selectedProbe = DeferredProbeBaker.Instance.GetProbe(name);
	}

	void ItemPressed(long idx)
	{
		switch (idx)
		{
			case 0: // Rename
				RenamePopup.Instance.Trigger(_selectedProbe);
				break;
			case 1: // Delete
				_selectedProbe.Delete();
				break;
		}
	}
}

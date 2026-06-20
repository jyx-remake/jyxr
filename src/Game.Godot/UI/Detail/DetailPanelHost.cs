using Game.Core.Model.Skills;
using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class DetailPanelHost : Control
{
	[Export]
	public PackedScene DetailPanelScene { get; set; } = null!;

	private Control? _currentPanel;

	public Control ShowSkill(SkillInstance skill)
	{
		ArgumentNullException.ThrowIfNull(skill);
		return Show(DetailPanelContentFactory.CreateSkill(skill));
	}

	public Control ShowInventoryEntry(InventoryEntry entry, DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(entry);
		return Show(DetailPanelContentFactory.CreateInventoryEntry(entry, action));
	}

	public Control ShowShopProduct(ShopProductView product, DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(product);
		return Show(DetailPanelContentFactory.CreateShopProduct(product, action));
	}

	public Control Show(DetailPanelContent content)
	{
		ArgumentNullException.ThrowIfNull(content);

		Close();
		if (DetailPanelScene is null)
		{
			throw new InvalidOperationException("DetailPanelHost.DetailPanelScene is not assigned.");
		}

		var instance = DetailPanelScene.Instantiate();
		if (instance is not DetailPanel panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Detail panel scene root must be DetailPanel.");
		}

		panel.Configure(content);
		AddChild(panel);
		MoveToFront();
		_currentPanel = panel;
		panel.TreeExited += () => ClearPanelReference(panel);
		return panel;
	}

	public void Close()
	{
		if (_currentPanel is not null && GodotObject.IsInstanceValid(_currentPanel))
		{
			_currentPanel.QueueFree();
		}

		_currentPanel = null;
	}

	private void ClearPanelReference(Control panel)
	{
		if (ReferenceEquals(_currentPanel, panel))
		{
			_currentPanel = null;
		}
	}
}

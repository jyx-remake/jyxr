using Game.Core.Model.Skills;
using Game.Application;
using Game.Core.Model;
using Godot;

namespace Game.Godot.UI;

public partial class DetailPanelHost : Control, IPanelBackTargetProvider
{
	[Export]
	public PackedScene DetailPanelScene { get; set; } = null!;

	private PanelHost _panels = null!;

	public Control? BackTarget => _panels.BackTarget;

	public override void _Ready()
	{
		_panels = new PanelHost(this);
	}

	public Control ShowSkill(SkillInstance skill, DetailPanelAction? action = null)
	{
		ArgumentNullException.ThrowIfNull(skill);
		return Show(DetailPanelContentFactory.CreateSkill(skill, action));
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
		_panels.ShowMain(panel);
		MoveToFront();
		return panel;
	}

	public void Close() => _panels.CloseAll();
}

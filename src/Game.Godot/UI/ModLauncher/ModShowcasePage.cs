using Game.Application.Mods;
using Godot;

namespace Game.Godot.UI.ModLauncher;

public partial class ModShowcasePage : Control
{
	[Export]
	public PackedScene ModItemCardScene { get; set; } = null!;

	private VBoxContainer _cardList = null!;
	private Control _emptyState = null!;

	public event Action<ModContext>? StartRequested;

	public override void _Ready()
	{
		_cardList = GetNode<VBoxContainer>("%CardList");
		_emptyState = GetNode<Control>("%EmptyState");
		ClearCards();
	}

	public void Configure(IReadOnlyList<ModContext> mods)
	{
		ArgumentNullException.ThrowIfNull(mods);
		ClearCards();
		_emptyState.Visible = mods.Count == 0;

		foreach (var mod in mods)
		{
			var card = CreateCard();
			card.StartRequested += context => StartRequested?.Invoke(context);
			_cardList.AddChild(card);
			card.Configure(mod);
		}
	}

	private ModItemCard CreateCard()
	{
		var instance = ModItemCardScene.Instantiate();
		if (instance is ModItemCard card)
		{
			return card;
		}

		instance.QueueFree();
		throw new InvalidOperationException("Mod item card scene root must be ModItemCard.");
	}

	private void ClearCards()
	{
		foreach (var child in _cardList.GetChildren())
		{
			if (child == _emptyState)
			{
				continue;
			}

			child.QueueFree();
		}
	}
}

using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class PartyPanel : JyPanel
{
	[Export]
	public PackedScene PartyCharacterBoxScene { get; set; } = null!;

	private GridContainer _gridContainer = null!;
	private Label _emptyLabel = null!;
	private readonly List<IDisposable> _subscriptions = [];
	private readonly Dictionary<string, PartyCharacterBox> _characterBoxes = [];

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_subscriptions.Add(Game.Session.Events.Subscribe<PartyChangedEvent>(OnPartyChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<CharacterChangedEvent>(OnCharacterChanged));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(OnSaveLoaded));
		Refresh();
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private void Refresh()
	{
		ClearGrid();
		_characterBoxes.Clear();

		var party = Game.State.Party;
		if (party.Members.Count == 0)
		{
			_emptyLabel.Visible = true;
			return;
		}

		_emptyLabel.Visible = false;
		for (var index = 0; index < party.Members.Count; index += 1)
		{
			var characterBox = CreateCharacterBox(party.Members[index], index);
			_characterBoxes[party.Members[index].Id] = characterBox;
			_gridContainer.AddChild(characterBox);
		}
	}

	private PartyCharacterBox CreateCharacterBox(CharacterInstance character, int partyIndex)
	{
		if (PartyCharacterBoxScene is null)
		{
			throw new InvalidOperationException("PartyCharacterBoxScene is not assigned.");
		}

		var instance = PartyCharacterBoxScene.Instantiate();
		if (instance is not PartyCharacterBox characterBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("PartyCharacterBox scene root must be PartyCharacterBox.");
		}

		characterBox.Setup(character, partyIndex);
		characterBox.CharacterSelected += OnCharacterSelected;
		characterBox.CharacterMoveRequested += OnCharacterMoveRequested;
		return characterBox;
	}

	private void OnCharacterSelected(string characterId)
	{
		UIRoot.Instance.ShowCharacterPanel(characterId);
	}

	private void OnCharacterMoveRequested(string characterId, int targetIndex)
	{
		Game.PartyService.MoveMember(characterId, targetIndex);
	}

	private void OnPartyChanged(PartyChangedEvent _) => Refresh();

	private void OnCharacterChanged(CharacterChangedEvent sessionEvent)
	{
		if (_characterBoxes.TryGetValue(sessionEvent.CharacterId, out var characterBox))
		{
			characterBox.RefreshView();
		}
	}

	private void OnSaveLoaded(SaveLoadedEvent _) => Refresh();

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			child.QueueFree();
		}
	}
}

using Game.Application;
using Game.Core.Model;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

/// <summary>
/// Team-style recall panel (legacy REJOIN_MENU replacement). Lists every
/// kicked companion as a party character card without dragging; clicking a
/// card asks for confirmation and recalls that companion into the party.
/// </summary>
public partial class RecallPanel : JyPanel
{
	[Export]
	public PackedScene RecallCharacterBoxScene { get; set; } = null!;

	private GridContainer _gridContainer = null!;
	private Label _hintLabel = null!;
	private Label _emptyLabel = null!;
	private readonly List<IDisposable> _subscriptions = [];
	private bool _isRecallInProgress;

	public override void _Ready()
	{
		base._Ready();
		_gridContainer = GetNode<GridContainer>("%GridContainer");
		_hintLabel = GetNode<Label>("%HintLabel");
		_emptyLabel = GetNode<Label>("%EmptyLabel");
		_subscriptions.Add(Game.Session.Events.Subscribe<PartyChangedEvent>(_ => Render()));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => Render()));
		Render();
	}

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
		base._ExitTree();
	}

	private void Render()
	{
		if (!IsInsideTree())
		{
			return;
		}

		ClearGrid();
		var kicked = Game.State.Party.GetAllCharacters()
			.Where(character => character.LeaveState == CharacterLeaveState.Kicked)
			.ToList();
		_emptyLabel.Visible = kicked.Count == 0;
		_hintLabel.Visible = kicked.Count > 0;
		for (var index = 0; index < kicked.Count; index += 1)
		{
			_gridContainer.AddChild(CreateCharacterBox(kicked[index], index));
		}
	}

	private PartyCharacterBox CreateCharacterBox(CharacterInstance character, int index)
	{
		if (RecallCharacterBoxScene is null)
		{
			throw new InvalidOperationException("RecallCharacterBoxScene is not assigned.");
		}

		var instance = RecallCharacterBoxScene.Instantiate();
		if (instance is not PartyCharacterBox characterBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Recall character box scene root must be PartyCharacterBox.");
		}

		characterBox.Setup(character, index, null);
		characterBox.CharacterSelected += OnCharacterSelected;
		return characterBox;
	}

	private async void OnCharacterSelected(string characterId)
	{
		if (_isRecallInProgress)
		{
			return;
		}

		if (!Game.State.Party.TryGetCharacter(characterId, out var character) ||
			character is null ||
			character.LeaveState != CharacterLeaveState.Kicked)
		{
			return;
		}

		_isRecallInProgress = true;
		try
		{
			var confirmed = await UIRoot.Instance.ShowConfirmAsync($"是否让该角色【{character.Name}】归队？");
			if (!confirmed)
			{
				return;
			}

			Game.PartyService.RecallKicked(characterId);
			UIRoot.Instance.ShowToast($"【{character.Name}】归队了。");
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Recalling kicked companion failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
		finally
		{
			_isRecallInProgress = false;
		}
	}

	private void ClearGrid()
	{
		foreach (var child in _gridContainer.GetChildren())
		{
			_gridContainer.RemoveChild(child);
			child.QueueFree();
		}
	}
}

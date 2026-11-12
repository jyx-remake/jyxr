using Game.Application;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

public partial class CharacterRosterPanel : Control, IPanelBackHandler
{
	[Export]
	public PackedScene CharacterAvatarButtonScene { get; set; } = null!;

	public string CharacterId
	{
		get;
		set
		{
			field = value;
			if (IsInsideTree())
			{
				Render();
			}
		}
	} = string.Empty;

	private CharacterPanel _characterPanel = null!;
	private VirtualGrid _avatarList = null!;
	private readonly List<IDisposable> _subscriptions = [];

	public override void _Ready()
	{
		_characterPanel = GetNode<CharacterPanel>("%CharacterPanel");
		_avatarList = GetNode<VirtualGrid>("%AvatarList");
		_characterPanel.ClosePanelRequested += QueueFree;
		_subscriptions.Add(Game.Session.Events.Subscribe<PartyChangedEvent>(_ => Render()));
		_subscriptions.Add(Game.Session.Events.Subscribe<CharacterChangedEvent>(_ => Render()));
		_subscriptions.Add(Game.Session.Events.Subscribe<SaveLoadedEvent>(_ => Render()));

		if (!string.IsNullOrWhiteSpace(CharacterId))
		{
			Render();
		}
	}

	public void HandleBack() => _characterPanel.HandleBack();

	public override void _ExitTree()
	{
		foreach (var subscription in _subscriptions)
		{
			subscription.Dispose();
		}

		_subscriptions.Clear();
	}

	private void Render()
	{
		var members = Game.State.Party.Members;
		if (members.Count == 0)
		{
			QueueFree();
			return;
		}

		if (string.IsNullOrWhiteSpace(CharacterId) ||
			!Game.State.Party.TryGetMember(CharacterId, out _))
		{
			CharacterId = members[0].Id;
			return;
		}

		_characterPanel.CharacterId = CharacterId;
		RenderAvatarList(members);
	}

	private void RenderAvatarList(IReadOnlyList<CharacterInstance> members)
	{
		_avatarList.SetItems(members, CreateAvatarButton, (button, member, _) =>
			button.Setup(member, string.Equals(member.Id, CharacterId, StringComparison.Ordinal)));
	}

	private CharacterRosterAvatarButton CreateAvatarButton()
	{
		if (CharacterAvatarButtonScene.Instantiate() is not CharacterRosterAvatarButton button)
		{
			throw new InvalidOperationException("Character avatar button scene root must be CharacterRosterAvatarButton.");
		}

		button.CharacterSelected += SelectCharacter;
		return button;
	}

	private void SelectCharacter(string characterId)
	{
		if (string.Equals(CharacterId, characterId, StringComparison.Ordinal))
		{
			return;
		}

		CharacterId = characterId;
	}
}

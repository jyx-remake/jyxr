using Game.Application;
using Game.Core.Model.Character;
using Godot;

namespace Game.Godot.UI;

/// <summary>
/// The title page inside the character panel status area. Replicates the
/// legacy title-select menu: a hint line naming the equipped title plus one
/// checkbox card per unlocked title. Toggling a card equips (exclusively)
/// or unequips that title through CharacterService.EquipTitle.
/// </summary>
public partial class CharacterTitleTab : Control
{
	[Export]
	public PackedScene TitleBoxScene { get; set; } = null!;

	public event Action<CharacterTitleInstance>? TitleDetailRequested;

	private RichTextLabel _hintLabel = null!;
	private GridContainer _titleContainer = null!;

	private string _characterId = string.Empty;
	private bool _isReadOnly;

	public bool IsReadOnly
	{
		get => _isReadOnly;
		set
		{
			_isReadOnly = value;
			RefreshToggles();
		}
	}

	public override void _Ready()
	{
		_hintLabel = GetNode<RichTextLabel>("%HintLabel");
		_titleContainer = GetNode<GridContainer>("%TitleContainer");
	}

	public void Setup(CharacterInstance character)
	{
		ArgumentNullException.ThrowIfNull(character);
		_characterId = character.Id;

		var equippedTitle = character.Titles.FirstOrDefault(title => title.Equipped);
		_hintLabel.Text = equippedTitle is null
			? "你可以勾选称号来决定是否在战斗中使用，当前没有称号"
			: $"你可以勾选称号来决定是否在战斗中使用，当前称号[color=magenta]【{equippedTitle.Definition.Name}】[/color]";

		foreach (var child in _titleContainer.GetChildren())
		{
			child.QueueFree();
		}

		foreach (var title in character.Titles)
		{
			_titleContainer.AddChild(CreateTitleBox(title));
		}
	}

	private CharacterTitleBox CreateTitleBox(CharacterTitleInstance title)
	{
		if (TitleBoxScene is null)
		{
			throw new InvalidOperationException("TitleBoxScene is not assigned.");
		}

		var instance = TitleBoxScene.Instantiate();
		if (instance is not CharacterTitleBox titleBox)
		{
			instance.QueueFree();
			throw new InvalidOperationException("Title box scene root must be CharacterTitleBox.");
		}

		titleBox.Setup(title, !_isReadOnly);
		titleBox.ToggleRequested += OnTitleToggleRequested;
		titleBox.DetailRequested += OnTitleDetailRequested;
		return titleBox;
	}

	private void OnTitleDetailRequested(CharacterTitleInstance title) =>
		TitleDetailRequested?.Invoke(title);

	private void OnTitleToggleRequested(CharacterTitleInstance title)
	{
		if (_isReadOnly || string.IsNullOrWhiteSpace(_characterId))
		{
			return;
		}

		// Titles are switch-only: pressing the equipped title is a no-op,
		// pressing another one exclusively equips it.
		if (title.Equipped)
		{
			return;
		}

		try
		{
			Game.CharacterService.EquipTitle(_characterId, title.Definition.Id);
		}
		catch (Exception exception)
		{
			Game.Logger.Error("Toggling character title failed.", exception);
			UIRoot.Instance.ShowSuggestion(exception.Message);
		}
	}

	private void RefreshToggles()
	{
		if (!IsInsideTree())
		{
			return;
		}

		foreach (var child in _titleContainer.GetChildren())
		{
			if (child is CharacterTitleBox titleBox)
			{
				titleBox.Disabled = _isReadOnly;
			}
		}
	}
}

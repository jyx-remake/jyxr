using Game.Core.Definitions;
using Game.Godot.Assets;
using Godot;

namespace Game.Godot.UI;

public partial class SelectSectScreen : Control
{
	private readonly TaskCompletionSource<SectDefinition> _selectionSource = new();
	private IReadOnlyList<SectDefinition> _sects = [];
	private int _activeSectIndex;

	private TextureRect _background = null!;
	private AvatarBox _masterAvatarBox = null!;
	private Label _sectNameLabel = null!;
	private Label _masterLabel = null!;
	private Label _topSkillLabel = null!;
	private Label _majorLabel = null!;
	private Label _featureLabel = null!;
	private Label _descLabel = null!;
	private TextureButton _prevButton = null!;
	private TextureButton _nextButton = null!;
	private TextureButton _ackButton = null!;

	public override void _Ready()
	{
		_background = GetNode<TextureRect>("%Background");
		_masterAvatarBox = GetNode<AvatarBox>("%MasterAvatarBox");
		_sectNameLabel = GetNode<Label>("%SectNameLabel");
		_masterLabel = GetNode<Label>("%MasterLabel");
		_topSkillLabel = GetNode<Label>("%TopSkillLabel");
		_majorLabel = GetNode<Label>("%MajorLabel");
		_featureLabel = GetNode<Label>("%FeatureLabel");
		_descLabel = GetNode<Label>("%DescLabel");
		_prevButton = GetNode<TextureButton>("%PrevButton");
		_nextButton = GetNode<TextureButton>("%NextButton");
		_ackButton = GetNode<TextureButton>("%AckButton");

		_prevButton.Pressed += ShowPreviousSect;
		_nextButton.Pressed += ShowNextSect;
		_ackButton.Pressed += ConfirmSelection;

		InitializeSects();
	}

	public async Task<SectDefinition> AwaitSelectionAsync(CancellationToken cancellationToken = default)
	{
		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (_selectionSource.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(this))
				{
					QueueFree();
				}
			})
			: default;

		return await _selectionSource.Task;
	}

	public override void _ExitTree()
	{
		if (!_selectionSource.Task.IsCompleted)
		{
			_selectionSource.TrySetCanceled();
		}
	}

	private void InitializeSects()
	{
		_sects = Game.ContentRepository.Sects.Values
			.Where(static sect => !string.IsNullOrWhiteSpace(sect.StoryId))
			.ToArray();
		if (_sects.Count == 0)
		{
			throw new InvalidOperationException("No selectable sect definitions are available.");
		}

		_activeSectIndex = 0;
		Refresh();
	}

	private void ShowPreviousSect()
	{
		_activeSectIndex = (_activeSectIndex - 1 + _sects.Count) % _sects.Count;
		Refresh();
	}

	private void ShowNextSect()
	{
		_activeSectIndex = (_activeSectIndex + 1) % _sects.Count;
		Refresh();
	}

	private void ConfirmSelection()
	{
		if (_selectionSource.TrySetResult(_sects[_activeSectIndex]))
		{
			QueueFree();
		}
	}

	private void Refresh()
	{
		var sect = _sects[_activeSectIndex];
		_sectNameLabel.Text = sect.Name;
		_masterLabel.Text = FormatList(sect.MasterNames, "无");
		_topSkillLabel.Text = FormatList(sect.SignatureSkillNames, "无");
		_majorLabel.Text = string.IsNullOrWhiteSpace(sect.PrimaryFocus) ? "未知" : sect.PrimaryFocus;
		_featureLabel.Text = FormatList(sect.TraitTags, "无");
		_descLabel.Text = sect.Description;
		_background.Texture = AssetResolver.LoadTextureResource(sect.Background);
		_masterAvatarBox.SetAvatarTexture(ResolveMasterPortrait(sect));
	}

	private static Texture2D? ResolveMasterPortrait(SectDefinition sect)
	{
		var portrait = AssetResolver.LoadTextureResource(sect.Portrait);
		if (portrait is not null)
		{
			return portrait;
		}

		return Game.State.Party.Members.FirstOrDefault() is { } hero
			? AssetResolver.LoadCharacterPortrait(hero)
			: null;
	}

	private static string FormatList(IReadOnlyList<string> values, string fallback)
	{
		var text = string.Join("、", values.Where(static value => !string.IsNullOrWhiteSpace(value)));
		return string.IsNullOrWhiteSpace(text) ? fallback : text;
	}
}

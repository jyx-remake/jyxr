using Godot;

namespace Game.Godot.UI;

public sealed record DetailPanelAction(
	string Label,
	bool IsEnabled,
	Func<Task> ExecuteAsync,
	bool CloseAfterExecute = true);

public sealed record DetailPanelContent(
	string Title,
	string Category,
	Texture2D? Icon,
	string BbCode,
	Color TitleColor,
	DetailPanelAction? Action = null);

public partial class DetailPanel : JyPanel
{
	private TextureRect _iconRect = null!;
	private Label _titleLabel = null!;
	private Label _categoryLabel = null!;
	private RichTextLabel _contentLabel = null!;
	private Button _actionButton = null!;

	private DetailPanelContent? _content;

	public override void _Ready()
	{
		base._Ready();
		_iconRect = GetNode<TextureRect>("%IconRect");
		_titleLabel = GetNode<Label>("%TitleLabel");
		_categoryLabel = GetNode<Label>("%CategoryLabel");
		_contentLabel = GetNode<RichTextLabel>("%ContentLabel");
		_actionButton = GetNode<Button>("%ActionButton");
		_actionButton.Pressed += OnActionButtonPressed;
		Refresh();
	}

	public void Configure(DetailPanelContent content)
	{
		ArgumentNullException.ThrowIfNull(content);
		_content = content;
		Refresh();
	}

	private void Refresh()
	{
		if (!IsInsideTree() || _content is null)
		{
			return;
		}

		_iconRect.Texture = _content.Icon;
		_titleLabel.Text = _content.Title;
		_categoryLabel.Text = _content.Category;
		_contentLabel.Text = _content.BbCode;
		RefreshActionButton(_content.Action);
		ApplyLabelColor(_titleLabel, _content.TitleColor);
	}

	private void RefreshActionButton(DetailPanelAction? action)
	{
		_actionButton.Visible = action is not null;
		if (action is null)
		{
			return;
		}

		_actionButton.Text = action.Label;
		_actionButton.Disabled = !action.IsEnabled;
	}

	private async void OnActionButtonPressed()
	{
		var action = _content?.Action;
		if (action is null || !action.IsEnabled)
		{
			return;
		}

		global::Game.Godot.Game.Audio.PlaySfx("音效.UI.点击");
		await action.ExecuteAsync();
		if (action.CloseAfterExecute && GodotObject.IsInstanceValid(this))
		{
			QueueFree();
		}
	}

	private static void ApplyLabelColor(Label label, Color color)
	{
		label.AddThemeColorOverride("font_color", color);

		if (label.LabelSettings is null)
		{
			return;
		}

		var labelSettings = (LabelSettings)label.LabelSettings.Duplicate();
		labelSettings.FontColor = color;
		label.LabelSettings = labelSettings;
	}
}

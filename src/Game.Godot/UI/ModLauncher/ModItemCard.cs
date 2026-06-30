using Game.Application.Mods;
using Godot;

namespace Game.Godot.UI.ModLauncher;

public partial class ModItemCard : PanelContainer
{
	private static readonly string[] PosterFileNames =
	[
		"poster.png",
		"poster.jpg",
		"poster.jpeg",
		"poster.webp",
	];

	private ModContext? _context;
	private TextureRect _poster = null!;
	private Texture2D? _defaultPoster;
	private Label _nameLabel = null!;
	private Label _descriptionLabel = null!;
	private Button _startButton = null!;
	private Button _downloadButton = null!;
	private Button _deleteButton = null!;

	public event Action<ModContext>? StartRequested;

	public override void _Ready()
	{
		_poster = GetNode<TextureRect>("%Poster");
		_defaultPoster = _poster.Texture;
		_nameLabel = GetNode<Label>("%NameLabel");
		_descriptionLabel = GetNode<Label>("%DescriptionLabel");
		_startButton = GetNode<Button>("%StartButton");
		_downloadButton = GetNode<Button>("%DownloadButton");
		_deleteButton = GetNode<Button>("%DeleteButton");

		_startButton.Pressed += OnStartPressed;
		_downloadButton.Disabled = true;
		_deleteButton.Disabled = true;
	}

	public void Configure(ModContext context)
	{
		ArgumentNullException.ThrowIfNull(context);
		_context = context;
		_nameLabel.Text = context.Manifest.Name;
		_descriptionLabel.Text = FormatDescription(context.Manifest);
		var posterPath = FindPosterPath(context.ModDirectoryPath);
		_poster.Texture = posterPath is null ? _defaultPoster : LoadPosterTexture(posterPath) ?? _defaultPoster;
		_poster.TooltipText = posterPath ?? context.ModDirectoryPath;
	}

	private void OnStartPressed()
	{
		if (_context is not null)
		{
			StartRequested?.Invoke(_context);
		}
	}

	private static string FormatDescription(ModManifest manifest)
	{
		var parts = new List<string>();
		var metaParts = new List<string>();
		if (!string.IsNullOrWhiteSpace(manifest.Author))
		{
			metaParts.Add($"作者：{manifest.Author.Trim()}");
		}

		metaParts.Add($"版本：{manifest.Version.Trim()}");
		if (!string.IsNullOrWhiteSpace(manifest.Date))
		{
			metaParts.Add($"时间：{manifest.Date.Trim()}");
		}

		parts.Add(string.Join("  ", metaParts));
		if (!string.IsNullOrWhiteSpace(manifest.Description))
		{
			parts.Add(manifest.Description.Trim());
		}

		return string.Join("\n", parts);
	}

	private static string? FindPosterPath(string modDirectoryPath)
	{
		foreach (var fileName in PosterFileNames)
		{
			var posterPath = Path.Combine(modDirectoryPath, fileName);
			if (File.Exists(posterPath))
			{
				return posterPath;
			}
		}

		return null;
	}

	private static Texture2D? LoadPosterTexture(string posterPath)
	{
		var image = new Image();
		var error = image.Load(posterPath);
		if (error != Error.Ok)
		{
			GD.PushWarning($"Failed to load mod poster '{posterPath}': {error}");
			return null;
		}

		return ImageTexture.CreateFromImage(image);
	}
}

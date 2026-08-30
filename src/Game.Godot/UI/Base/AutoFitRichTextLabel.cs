using Godot;

namespace Game.Godot.UI;

[GlobalClass]
public partial class AutoFitRichTextLabel : RichTextLabel
{
	private const int DefaultMaximumFontSize = 50;
	private const int DefaultMinimumFontSize = 30;

	private int _maximumFontSize = DefaultMaximumFontSize;
	private int _minimumFontSize = DefaultMinimumFontSize;
	private int _appliedFontSize = -1;
	private bool _fitScheduled;
	private bool _isApplyingFontSize;

	[Export(PropertyHint.Range, "1,256,1")]
	public int MaximumFontSize
	{
		get => _maximumFontSize;
		set
		{
			_maximumFontSize = Math.Max(1, value);
			ScheduleFit();
		}
	}

	[Export(PropertyHint.Range, "1,256,1")]
	public int MinimumFontSize
	{
		get => _minimumFontSize;
		set
		{
			_minimumFontSize = Math.Max(1, value);
			ScheduleFit();
		}
	}

	public override void _Ready()
	{
		Threaded = false;
		BbcodeEnabled = true;
		FitContent = false;
		// Dialogue and choice text must remain inside the panel without exposing
		// a scrollbar. Word wrapping lets the binary-fit pass reduce the font
		// size based on the actual number of rendered lines.
		AutowrapMode = TextServer.AutowrapMode.WordSmart;
		ScrollActive = false;
		ClipContents = true;

		Resized += ScheduleFit;
		SetProcess(false);
		ScheduleFit();
	}

	public override void _Process(double delta)
	{
		if (!_fitScheduled)
		{
			SetProcess(false);
			return;
		}

		_fitScheduled = false;
		SetProcess(false);
		ApplyBestFit();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationThemeChanged && IsNodeReady() && !_isApplyingFontSize)
		{
			ScheduleFit();
		}
	}

	public void SetContent(string? content)
	{
		Text = content ?? string.Empty;
		ScheduleFit();
	}

	private void ScheduleFit()
	{
		if (!IsInsideTree())
		{
			return;
		}

		_fitScheduled = true;
		SetProcess(true);
	}

	private void ApplyBestFit()
	{
		if (Size.X <= 0 || Size.Y <= 0)
		{
			return;
		}

		var minimum = MinimumFontSize;
		var maximum = Math.Max(minimum, MaximumFontSize);
		var bestFit = minimum;
		var lowerBound = minimum;
		var upperBound = maximum;

		while (lowerBound <= upperBound)
		{
			var candidate = lowerBound + ((upperBound - lowerBound) / 2);
			ApplyFontSize(candidate);

			if (GetContentWidth() <= Size.X && GetContentHeight() <= Size.Y)
			{
				bestFit = candidate;
				lowerBound = candidate + 1;
			}
			else
			{
				upperBound = candidate - 1;
			}
		}

		ApplyFontSize(bestFit);
	}

	private void ApplyFontSize(int fontSize)
	{
		if (_appliedFontSize == fontSize)
		{
			return;
		}

		_isApplyingFontSize = true;
		try
		{
			AddThemeFontSizeOverride("normal_font_size", fontSize);
			AddThemeFontSizeOverride("bold_font_size", fontSize);
			AddThemeFontSizeOverride("italics_font_size", fontSize);
			AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
			AddThemeFontSizeOverride("mono_font_size", fontSize);
			_appliedFontSize = fontSize;
		}
		finally
		{
			_isApplyingFontSize = false;
		}
	}
}

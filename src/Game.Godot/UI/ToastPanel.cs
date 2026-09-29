using Game.Application;
using Godot;

namespace Game.Godot.UI;

public partial class ToastPanel : Control
{
	private const int MaxVisibleMessages = 3;
	private const int MaxPendingMessages = 5;
	private const double FadeInDuration = 0.14d;
	private const double HoldDuration = 2.5d;
	private const double FadeOutDuration = 0.26d;

	[Export]
	public PackedScene ToastItemScene { get; set; } = null!;

	[Export]
	public float StackTop { get; set; }

	[Export]
	public float StackSpacing { get; set; }

	[Export]
	public float BottomMargin { get; set; }

	private readonly List<ToastEntry> _pendingMessages = [];
	private readonly List<ToastView> _visibleMessages = [];

	public override void _Ready()
	{
		Modulate = Colors.White;
		SetProcess(false);
	}

	public override void _Process(double delta)
	{
		ActivatePendingMessages();
		UpdateVisibleMessages(delta);
		ActivatePendingMessages();

		if (_pendingMessages.Count == 0 && _visibleMessages.Count == 0)
		{
			SetProcess(false);
			Hide();
		}
	}

	public void Enqueue(string text, ToastTone tone = ToastTone.Normal)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		var normalizedText = text.Trim();
		if (TryMergeVisibleMessage(normalizedText, tone) || TryMergePendingMessage(normalizedText, tone))
		{
			Show();
			SetProcess(true);
			return;
		}

		_pendingMessages.Add(new ToastEntry(normalizedText, tone));
		TrimPendingMessages();
		Show();
		SetProcess(true);
	}

	public void Clear()
	{
		_pendingMessages.Clear();
		foreach (var view in _visibleMessages)
		{
			view.Node.QueueFree();
		}

		_visibleMessages.Clear();
		SetProcess(false);
		Hide();
	}

	private void ActivatePendingMessages()
	{
		while (_visibleMessages.Count < MaxVisibleMessages && _pendingMessages.Count > 0)
		{
			var entry = _pendingMessages[0];
			_pendingMessages.RemoveAt(0);
			_visibleMessages.Add(CreateToastView(entry));
			ReflowVisibleMessages();
		}
	}

	private void UpdateVisibleMessages(double delta)
	{
		for (var index = _visibleMessages.Count - 1; index >= 0; index--)
		{
			var view = _visibleMessages[index];
			UpdateToastView(view, delta);
			if (view.Phase != ToastPhase.Done)
			{
				continue;
			}

			view.Node.QueueFree();
			_visibleMessages.RemoveAt(index);
			ReflowVisibleMessages();
		}
	}

	private ToastView CreateToastView(ToastEntry entry)
	{
		if (ToastItemScene.Instantiate() is not ToastItem node)
		{
			throw new InvalidOperationException("Toast item scene root must be ToastItem.");
		}

		var view = new ToastView(entry, node);
		AddChild(node);
		node.Show();
		node.Configure(entry.Tone);
		SetToastAlpha(view, 0f);
		RenderToastText(view);
		return view;
	}

	private static void UpdateToastView(ToastView view, double delta)
	{
		switch (view.Phase)
		{
			case ToastPhase.FadingIn:
				view.Elapsed += delta;
				SetToastAlpha(view, Math.Clamp((float)(view.Elapsed / FadeInDuration), 0f, 1f));
				if (view.Elapsed >= FadeInDuration)
				{
					view.Phase = ToastPhase.Holding;
					view.Elapsed = 0d;
					view.HoldRemaining = HoldDuration;
					SetToastAlpha(view, 1f);
				}
				break;
			case ToastPhase.Holding:
				view.HoldRemaining -= delta;
				SetToastAlpha(view, 1f);
				if (view.HoldRemaining <= 0d)
				{
					view.Phase = ToastPhase.FadingOut;
					view.Elapsed = 0d;
				}
				break;
			case ToastPhase.FadingOut:
				view.Elapsed += delta;
				SetToastAlpha(view, 1f - Math.Clamp((float)(view.Elapsed / FadeOutDuration), 0f, 1f));
				if (view.Elapsed >= FadeOutDuration)
				{
					view.Phase = ToastPhase.Done;
				}
				break;
		}
	}

	private bool TryMergeVisibleMessage(string text, ToastTone tone)
	{
		var view = _visibleMessages.FirstOrDefault(candidate =>
			candidate.Entry.Tone == tone &&
			string.Equals(candidate.Entry.Text, text, StringComparison.Ordinal));
		if (view is null)
		{
			return false;
		}

		view.Entry.Count++;
		view.Phase = ToastPhase.Holding;
		view.Elapsed = 0d;
		view.HoldRemaining = HoldDuration;
		RenderToastText(view);
		SetToastAlpha(view, 1f);
		ReflowVisibleMessages();
		return true;
	}

	private bool TryMergePendingMessage(string text, ToastTone tone)
	{
		var entry = _pendingMessages.FirstOrDefault(candidate =>
			candidate.Tone == tone &&
			string.Equals(candidate.Text, text, StringComparison.Ordinal));
		if (entry is null)
		{
			return false;
		}

		entry.Count++;
		return true;
	}

	private void TrimPendingMessages()
	{
		while (_pendingMessages.Count > MaxPendingMessages)
		{
			_pendingMessages.RemoveAt(0);
		}
	}

	private void ReflowVisibleMessages()
	{
		var totalHeight = _visibleMessages.Sum(view => view.Height) +
			Math.Max(0, _visibleMessages.Count - 1) * StackSpacing;
		var availableBottom = Size.Y > 0f
			? Size.Y - BottomMargin
			: StackTop + totalHeight;
		var baseTop = Math.Min(StackTop, Math.Max(0f, availableBottom - totalHeight));

		var top = baseTop;
		foreach (var view in _visibleMessages)
		{
			view.Node.OffsetTop = top;
			view.Node.OffsetBottom = top + view.Height;
			top += view.Height + StackSpacing;
		}
	}

	private static void RenderToastText(ToastView view)
	{
		view.Height = view.Node.SetMessage(view.Entry.Text, view.Entry.Count);
	}

	private static void SetToastAlpha(ToastView view, float alpha)
	{
		view.Node.SetAlpha(alpha);
	}

	private sealed class ToastEntry(string text, ToastTone tone)
	{
		public string Text { get; } = text;

		public ToastTone Tone { get; } = tone;

		public int Count { get; set; } = 1;
	}

	private sealed class ToastView(ToastEntry entry, ToastItem node)
	{
		public ToastEntry Entry { get; } = entry;

		public ToastItem Node { get; } = node;

		public ToastPhase Phase { get; set; } = ToastPhase.FadingIn;

		public double Elapsed { get; set; }

		public double HoldRemaining { get; set; }

		public float Height { get; set; }
	}

	private enum ToastPhase
	{
		FadingIn,
		Holding,
		FadingOut,
		Done,
	}
}

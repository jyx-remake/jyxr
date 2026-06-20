using Godot;

namespace Game.Godot.UI;

public partial class ToastPanel : Control
{
	private const int MaxVisibleMessages = 3;
	private const int MaxPendingMessages = 5;
	private const double FadeInDuration = 0.16d;
	private const double HoldDuration = 1d;
	private const double FadeOutDuration = 0.24d;
	private const float StackSpacing = 12f;

	private readonly List<ToastEntry> _pendingMessages = [];
	private readonly List<ToastView> _visibleMessages = [];
	private TextureRect _itemTemplate = null!;
	private float _templateTop;
	private float _templateHeight;

	public override void _Ready()
	{
		_itemTemplate = GetNode<TextureRect>("TextureRect");
		_templateTop = _itemTemplate.OffsetTop;
		_templateHeight = _itemTemplate.OffsetBottom - _itemTemplate.OffsetTop;
		_itemTemplate.Hide();
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

	public void Enqueue(string text)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(text);

		var normalizedText = text.Trim();
		if (TryMergeVisibleMessage(normalizedText) || TryMergePendingMessage(normalizedText))
		{
			Show();
			SetProcess(true);
			return;
		}

		_pendingMessages.Add(new ToastEntry(normalizedText));
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
		if (_itemTemplate.Duplicate() is not TextureRect node)
		{
			throw new InvalidOperationException("Toast item template must duplicate to TextureRect.");
		}

		var label = node.GetNode<Label>("MessageLabel");
		var view = new ToastView(entry, node, label);
		AddChild(node);
		node.Show();
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

	private bool TryMergeVisibleMessage(string text)
	{
		var view = _visibleMessages.FirstOrDefault(candidate =>
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
		return true;
	}

	private bool TryMergePendingMessage(string text)
	{
		var entry = _pendingMessages.FirstOrDefault(candidate =>
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
		var totalHeight = _visibleMessages.Count * _templateHeight +
			Math.Max(0, _visibleMessages.Count - 1) * StackSpacing;
		var availableBottom = Size.Y > 0f
			? Size.Y - 16f
			: _templateTop + totalHeight;
		var baseTop = Math.Min(_templateTop, Math.Max(0f, availableBottom - totalHeight));

		for (var index = 0; index < _visibleMessages.Count; index++)
		{
			var node = _visibleMessages[index].Node;
			var top = baseTop + index * (_templateHeight + StackSpacing);
			node.OffsetTop = top;
			node.OffsetBottom = top + _templateHeight;
		}
	}

	private static void RenderToastText(ToastView view)
	{
		view.Label.Text = view.Entry.Count > 1
			? $"{view.Entry.Text} x{view.Entry.Count}"
			: view.Entry.Text;
	}

	private static void SetToastAlpha(ToastView view, float alpha)
	{
		view.Node.Modulate = new Color(1f, 1f, 1f, alpha);
	}

	private sealed class ToastEntry(string text)
	{
		public string Text { get; } = text;

		public int Count { get; set; } = 1;
	}

	private sealed class ToastView(ToastEntry entry, TextureRect node, Label label)
	{
		public ToastEntry Entry { get; } = entry;

		public TextureRect Node { get; } = node;

		public Label Label { get; } = label;

		public ToastPhase Phase { get; set; } = ToastPhase.FadingIn;

		public double Elapsed { get; set; }

		public double HoldRemaining { get; set; }
	}

	private enum ToastPhase
	{
		FadingIn,
		Holding,
		FadingOut,
		Done,
	}
}

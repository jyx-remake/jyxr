using Godot;

namespace Game.Godot.UI;

public sealed class PanelHost(Node container)
{
	private readonly HashSet<Control> _panels = [];
	private Control? _mainPanel;
	private Control? _popupPanel;

	public bool HasPanels => _panels.Any(panel =>
		GodotObject.IsInstanceValid(panel) && !panel.IsQueuedForDeletion());

	public Control? BackTarget
	{
		get
		{
			for (var index = container.GetChildCount() - 1; index >= 0; index--)
			{
				if (container.GetChild(index) is not Control panel ||
					!panel.IsVisibleInTree() || panel.IsQueuedForDeletion())
				{
					continue;
				}

				var target = panel is IPanelBackTargetProvider provider ? provider.BackTarget : panel;
				if (target is not null && GodotObject.IsInstanceValid(target) &&
					target.IsVisibleInTree() && !target.IsQueuedForDeletion())
				{
					return target;
				}
			}

			return null;
		}
	}

	public void Add(Control panel)
	{
		_panels.Add(panel);
		panel.TreeExited += () => Forget(panel);
		container.AddChild(panel);
	}

	public void ShowMain(Control panel)
	{
		CloseMain();
		_mainPanel = panel;
		Add(panel);
	}

	public void ShowPopup(Control panel)
	{
		ClosePopup();
		_popupPanel = panel;
		Add(panel);
	}

	public void CloseMain()
	{
		ClosePopup();
		Close(_mainPanel);
	}

	public void ClosePopup() => Close(_popupPanel);

	public void CloseAll()
	{
		foreach (var panel in _panels.ToArray())
		{
			Close(panel);
		}
	}

	private void Close(Control? panel)
	{
		if (panel is null || !GodotObject.IsInstanceValid(panel))
		{
			return;
		}

		Forget(panel);
		panel.Hide();
		panel.QueueFree();
	}

	private void Forget(Control panel)
	{
		_panels.Remove(panel);
		if (ReferenceEquals(_mainPanel, panel))
		{
			_mainPanel = null;
		}
		if (ReferenceEquals(_popupPanel, panel))
		{
			_popupPanel = null;
		}
	}

	public static T Create<T>(PackedScene? scene, string description, Action<T>? configure = null) where T : Control
	{
		if (scene is null)
		{
			throw new InvalidOperationException($"Panel scene is not assigned: {description}.");
		}

		var instance = scene.Instantiate();
		if (instance is not T panel)
		{
			instance.QueueFree();
			throw new InvalidOperationException($"Panel scene root must be {typeof(T).Name}: {description}.");
		}

		try
		{
			configure?.Invoke(panel);
		}
		catch
		{
			panel.QueueFree();
			throw;
		}

		return panel;
	}

	public static async Task AwaitClosedAsync(Control panel, CancellationToken cancellationToken)
	{
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		panel.TreeExited += () => completion.TrySetResult();

		using var registration = cancellationToken.CanBeCanceled
			? cancellationToken.Register(() =>
			{
				if (completion.TrySetCanceled(cancellationToken) && GodotObject.IsInstanceValid(panel))
				{
					panel.QueueFree();
				}
			})
			: default;

		await completion.Task;
	}
}

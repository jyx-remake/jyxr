using Godot;

namespace Game.Godot.Map;

internal sealed class LargeMapTransform
{
	private readonly Vector2 _logicalSize;
	private readonly float _minimumZoom;
	private readonly float _maximumZoom;

	public LargeMapTransform(Vector2 logicalSize, float minimumZoom, float maximumZoom)
	{
		if (logicalSize.X <= 0f || logicalSize.Y <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(logicalSize));
		}

		_logicalSize = logicalSize;
		_minimumZoom = minimumZoom;
		_maximumZoom = maximumZoom;
	}

	public Vector2 ViewportSize { get; private set; }
	public float Zoom { get; private set; } = 1f;
	public Vector2 Translation { get; private set; }

	/// <summary>
	/// Uniform "cover" scale: at zoom 1 the map fills the viewport completely
	/// (no black edges) without aspect distortion. With a viewport whose
	/// aspect differs from the logical canvas — for example the safe band
	/// between the HUD strips — the overflowing axis is cropped and stays
	/// reachable through panning.
	/// </summary>
	public Vector2 BaseScale
	{
		get
		{
			var sx = ViewportSize.X > 0f ? ViewportSize.X / _logicalSize.X : 1f;
			var sy = ViewportSize.Y > 0f ? ViewportSize.Y / _logicalSize.Y : 1f;
			var cover = MathF.Max(sx, sy);
			return new Vector2(cover, cover);
		}
	}

	public Vector2 SurfaceScale => BaseScale * Zoom;

	public float MarkerScale => BaseScale.Y * Zoom;

	public void Reset(Vector2 viewportSize, float zoom = 1f, Vector2? centerLogicalPosition = null)
	{
		ViewportSize = viewportSize;
		Zoom = Mathf.Clamp(zoom, _minimumZoom, _maximumZoom);
		var center = centerLogicalPosition ?? _logicalSize * 0.5f;
		Translation = viewportSize * 0.5f - center * SurfaceScale;
		ClampTranslation();
	}

	public void Resize(Vector2 viewportSize)
	{
		if (viewportSize == ViewportSize)
		{
			return;
		}

		if (ViewportSize.X <= 0f || ViewportSize.Y <= 0f)
		{
			// The view was configured before the Control got a real size (for
			// example a map built in the same frame the world was added). Carry
			// the configured zoom over instead of falling back to the Reset
			// default, which is the minimum zoom.
			Reset(viewportSize, Zoom);
			return;
		}

		var centerLogicalPosition = Unproject(ViewportSize * 0.5f);
		ViewportSize = viewportSize;
		Translation = viewportSize * 0.5f - centerLogicalPosition * SurfaceScale;
		ClampTranslation();
	}

	public void Pan(Vector2 screenDelta)
	{
		Translation += screenDelta;
		ClampTranslation();
	}

	public void ZoomAround(float factor, Vector2 previousScreenPosition, Vector2 currentScreenPosition)
	{
		var anchorLogicalPosition = Unproject(previousScreenPosition);
		Zoom = Mathf.Clamp(Zoom * factor, _minimumZoom, _maximumZoom);
		Translation = currentScreenPosition - anchorLogicalPosition * SurfaceScale;
		ClampTranslation();
	}

	public Vector2 Project(Vector2 logicalPosition) =>
		Translation + logicalPosition * SurfaceScale;

	public Vector2 Unproject(Vector2 screenPosition)
	{
		var scale = SurfaceScale;
		return new Vector2(
			scale.X > 0f ? (screenPosition.X - Translation.X) / scale.X : 0f,
			scale.Y > 0f ? (screenPosition.Y - Translation.Y) / scale.Y : 0f);
	}

	private void ClampTranslation()
	{
		var surfaceSize = _logicalSize * SurfaceScale;
		Translation = new Vector2(
			ClampAxis(Translation.X, surfaceSize.X, ViewportSize.X),
			ClampAxis(Translation.Y, surfaceSize.Y, ViewportSize.Y));
	}

	private static float ClampAxis(float translation, float contentLength, float viewportLength)
	{
		if (contentLength <= viewportLength)
		{
			return (viewportLength - contentLength) * 0.5f;
		}

		return Mathf.Clamp(translation, viewportLength - contentLength, 0f);
	}
}

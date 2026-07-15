using Godot;

namespace Game.Godot.Map;

internal sealed class LargeMapTransform
{
	private readonly Vector2 _designSize;
	private readonly float _minimumZoom;
	private readonly float _maximumZoom;

	public LargeMapTransform(Vector2 designSize, float minimumZoom, float maximumZoom)
	{
		if (designSize.X <= 0f || designSize.Y <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(designSize));
		}

		_designSize = designSize;
		_minimumZoom = minimumZoom;
		_maximumZoom = maximumZoom;
	}

	public Vector2 ViewportSize { get; private set; }
	public float Zoom { get; private set; } = 1f;
	public Vector2 Translation { get; private set; }

	public Vector2 BaseScale => new(
		ViewportSize.X > 0f ? ViewportSize.X / _designSize.X : 1f,
		ViewportSize.Y > 0f ? ViewportSize.Y / _designSize.Y : 1f);

	public Vector2 SurfaceScale => BaseScale * Zoom;

	public float MarkerScale => BaseScale.Y * Zoom;

	public void Reset(Vector2 viewportSize)
	{
		ViewportSize = viewportSize;
		Zoom = _minimumZoom;
		Translation = Vector2.Zero;
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
			Reset(viewportSize);
			return;
		}

		var centerDesignPosition = Unproject(ViewportSize * 0.5f);
		ViewportSize = viewportSize;
		Translation = viewportSize * 0.5f - centerDesignPosition * SurfaceScale;
		ClampTranslation();
	}

	public void Pan(Vector2 screenDelta)
	{
		Translation += screenDelta;
		ClampTranslation();
	}

	public void ZoomAround(float factor, Vector2 previousScreenPosition, Vector2 currentScreenPosition)
	{
		var anchorDesignPosition = Unproject(previousScreenPosition);
		Zoom = Mathf.Clamp(Zoom * factor, _minimumZoom, _maximumZoom);
		Translation = currentScreenPosition - anchorDesignPosition * SurfaceScale;
		ClampTranslation();
	}

	public Vector2 Project(Vector2 designPosition) =>
		Translation + designPosition * SurfaceScale;

	public Vector2 Unproject(Vector2 screenPosition)
	{
		var scale = SurfaceScale;
		return new Vector2(
			scale.X > 0f ? (screenPosition.X - Translation.X) / scale.X : 0f,
			scale.Y > 0f ? (screenPosition.Y - Translation.Y) / scale.Y : 0f);
	}

	private void ClampTranslation()
	{
		var surfaceSize = _designSize * SurfaceScale;
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

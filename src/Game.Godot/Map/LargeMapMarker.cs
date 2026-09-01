using Game.Core.Definitions;
using Game.Godot.UI;
using Godot;

namespace Game.Godot.Map;

public partial class LargeMapMarker : Control
{
	[Export]
	public Texture2D? DefaultTexture { get; set; }

	/// <summary>
	/// native 图标（town.native.* / town.city.*）专用的描边材质。
	/// 因 native 图标用原尺寸大图，默认材质的 outline_width_texels=4.5 视觉偏粗，
	/// 这里用更细的描边（outline_width_texels=2.0）。在 map_entity_slot.tscn 中配置。
	/// </summary>
	[Export]
	public Material? NativeOutlineMaterial { get; set; }

	/// <summary>
	/// 图标缩放系数（由 LargeMapView 读取，叠加到 marker.Scale 之上）。
	/// - 标准 / native / 普通城镇 → 1.0
	/// - noevent.png 与 town.waiguo* → 0.7（紧凑方案）
	/// </summary>
	public float IconScaleFactor { get; private set; } = 1f;

	private TextureRect _avatar = null!;
	private Control _visual = null!;
	private OverflowTextureRect _overflowAvatar = null!;
	private Label _nameLabel = null!;
	private TextureRect _notice = null!;
	private Material? _defaultAvatarMaterial;

	/// <summary>Visual 的设计尺寸（map_entity_slot.tscn 中为 80x80）。</summary>
	private Vector2 _visualSize = new(80f, 80f);

	/// <summary>当前图标底边在 Visual 局部坐标中的 Y 值，用于定位地图名。</summary>
	private float _iconBottomLocal = 80f;

	public (string MapId, MapLocationDefinition Location, MapEventDefinition? Event)? Location { get; private set; }

	public Vector2 LogicalPosition { get; private set; }

	public override void _Ready()
	{
		_visual = GetNode<Control>("%Visual");
		_avatar = GetNode<TextureRect>("%Avatar");
		_overflowAvatar = GetNode<OverflowTextureRect>("%OverflowAvatar");
		_nameLabel = GetNode<Label>("%NameLabel");
		_notice = GetNode<TextureRect>("%Notice");
		_defaultAvatarMaterial = _avatar.Material;

		var size = _visual.Size;
		if (size.X > 1f && size.Y > 1f)
		{
			_visualSize = size;
		}

		// 让 Avatar/OverflowAvatar 的缩放以 Visual **底部**为原点。
		// 这样 SetIconScale 中 IconScaleFactor（如 noevent 0.7）缩放时，
		// 图标底部固定在 Visual 底（地图坐标点），向上生长变短，
		// 与 native 图标 ApplyIconLayout 的"底部对齐"语义一致，
		// 不会让紧凑图标向上偏移导致与其他图标底部不对齐。
		_avatar.PivotOffset = new Vector2(_visualSize.X * 0.5f, _visualSize.Y);
		_overflowAvatar.PivotOffset = new Vector2(_visualSize.X * 0.5f, _visualSize.Y);

		Refresh();
	}

	public void Setup(
		(string MapId, MapLocationDefinition Location, MapEventDefinition? Event) location,
		Vector2 logicalPosition)
	{
		Location = location;
		LogicalPosition = logicalPosition;
		Refresh();
	}

	/// <summary>
	/// 图标在屏幕上的实际包围盒（含 native 图标溢出 Visual 的部分）。
	/// 用于点击命中和 tooltip 定位。
	/// </summary>
	public Rect2 GetScreenBounds()
	{
		var target = _avatar.Visible ? (Control)_avatar : _overflowAvatar;
		return new Rect2(
			Position + (_visual.Position + target.Position) * Scale,
			target.Size * Scale);
	}

	/// <summary>
	/// 应用图标缩放档位：仅 Avatar/OverflowAvatar 受 IconScaleFactor 影响，
	/// label 和 notice 保持 markerScale 全局缩放（标准字号/大小）。
	///
	/// 这让 noevent/waiguo 的图标紧凑 0.7x，但地图名和感叹号大小跟其他节点一致。
	/// </summary>
	public void SetIconScale(Vector2 markerScale)
	{
		// Avatar/OverflowAvatar 用 IconScaleFactor 缩放（不反向 markerScale）
		//   全局 = markerScale * IconScaleFactor
		// 标准 1.0 → 屏 80px（按 markerScale 缩放）
		// noevent/waiguo 0.7 → 屏 56px（紧凑，但仍跟 markerScale 缩放）
		_avatar.Scale = new Vector2(IconScaleFactor, IconScaleFactor);
		_overflowAvatar.Scale = new Vector2(IconScaleFactor, IconScaleFactor);
		// label/notice 不动 —— 它们在 Visual/Avatar 内，Scale=1（label）
		// 或 0.4（notice tscn），全局 = tscn_scale * markerScale，
		// 与 noevent 无关 → 字号和感叹号大小跟其他节点一致
	}

	private void Refresh()
	{
		if (Location is not { } location || !IsInsideTree())
		{
			return;
		}

		_nameLabel.Text = MapEntityPresentation.ResolveLocationName(location.Location);
		_notice.Visible = location.Event?.RepeatMode == RepeatMode.Once;
		var avatar = MapEntityPresentation.ResolveAvatar(
			DefaultTexture,
			location.Location,
			location.Event);

		_avatar.Texture = avatar.UseOverflow ? null : avatar.Texture;
		_avatar.Visible = !avatar.UseOverflow;
		_overflowAvatar.Texture = avatar.UseOverflow ? avatar.Texture : null;
		_overflowAvatar.Visible = avatar.UseOverflow;

		// ── 推断图标缩放档位 ──
		// noevent.png 与 town.waiguo* 用 0.7x 紧凑档，其余保持 1.0。
		var imageId = (location.Event is null
			? location.Location.NoEventImage
			: location.Event.Image ?? location.Location.Picture) ?? string.Empty;
		IconScaleFactor = IsExtraCompactImage(imageId) || UsesDefaultNoEventTexture(avatar)
			? 0.7f
			: 1f;

		ApplyIconLayout(avatar.UseOverflow ? _overflowAvatar : _avatar, avatar);

		// 地图名位置（Visual 局部坐标）：
		//   X = -35（tscn 原始 offset_left，水平居中于 80px 宽的 Visual）
		//   Y = 图标底边 - 10（与 tscn 原始 offset_top=70 一致，与图标底重叠 10 屏像素，
		//       抵消 Label 内部 vertical padding + outline 的约 9.5 屏像素间隙，
		//       让文字视觉紧贴图标底）。
		// 因 ApplyIconLayout 已改为底部对齐，图标底边恒在 Visual 底（80），
		// native 图标也是向上生长、底部固定，所以统一用 -10 即可，
		// 不存在 native 盖住 label 的问题。
		_nameLabel.Position = new Vector2(
			-35f,
			_iconBottomLocal - 10f);
	}

	/// <summary>
	/// 施加图标尺寸与材质，对齐魔改引擎 MapLocationUI.SetImageSize：
	/// - town.native.* / town.city.* → 纹理原始像素尺寸（原比例输出）
	/// - 其余 → 统一 Visual 设计尺寸（80x80）
	///
	/// 对齐方式用**底部锚点**（等价 Unity RectTransform 底部 pivot）：
	/// 图标底部固定在 Visual 底边（= 地图坐标点），向上生长。
	/// 这样 native 图标与标准图标的底部、地图名位置完全一致，
	/// 也符合魔改引擎"雕像向上延伸、文字贴底"的观感。
	/// </summary>
	private void ApplyIconLayout(Control target, MapEntityAvatarPresentation avatar)
	{
		var textureSize = avatar.UseNativeSize && avatar.Texture is not null
			? avatar.Texture.GetSize()
			: Vector2.Zero;
		var useNativeSize = textureSize.X > 1f && textureSize.Y > 1f;
		var size = useNativeSize ? textureSize : _visualSize;

		// 所有图标保留描边。native 图标用专用材质（描边更细），
		// 其余用默认描边材质。
		target.Material = useNativeSize
			? NativeOutlineMaterial ?? _defaultAvatarMaterial
			: _defaultAvatarMaterial;

		// 水平居中 + 底部对齐（向上生长）
		var halfX = (size.X - _visualSize.X) * 0.5f;
		target.OffsetLeft = -halfX;
		target.OffsetRight = halfX;
		target.OffsetTop = -(size.Y - _visualSize.Y);  // 顶部上移全部
		target.OffsetBottom = 0f;                       // 底部固定

		// 底部对齐 → 图标底边恒在 Visual 底（_visualSize.Y）
		_iconBottomLocal = _visualSize.Y;
	}

	/// <summary>判断资源 ID 是否为 noevent / town.waiguo* 等需要紧凑显示的图标。</summary>
	private static bool IsExtraCompactImage(string imageId)
	{
		if (string.IsNullOrWhiteSpace(imageId))
			return false;
		var lower = imageId.ToLowerInvariant();
		return lower.Contains("noevent") || lower.Contains("waiguo");
	}

	/// <summary>判断该 marker 是否在使用场景配置的 noevent.png 兜底纹理。</summary>
	private bool UsesDefaultNoEventTexture(MapEntityAvatarPresentation avatar) =>
		DefaultTexture is not null && ReferenceEquals(avatar.Texture, DefaultTexture);
}

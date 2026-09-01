shader_type canvas_item;
render_mode unshaded;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 0.55);
// 描边宽度（纹理像素 / texel）。缩放时描边与图标等比缩放，视觉粗细恒定。
// 典型值：2 ~ 5 texels。旧版 screen-pixel 模式已移除（缩小描边粗、放大描边细的问题）。
uniform float outline_width_texels : hint_range(0.5, 12.0, 0.5) = 3.0;
uniform float alpha_threshold : hint_range(0.0, 1.0, 0.01) = 0.01;

const int OUTLINE_SAMPLES = 32;

varying vec4 canvas_modulate;

void vertex() {
	canvas_modulate = COLOR;

	// 顶点扩展：固定屏幕像素余量，仅确保描边区域被渲染到。
	// 实际描边粗细由 fragment 阶段的纹理相对宽度控制，不受此值影响。
	float geometry_margin_px = 6.0;
	vec2 canvas_direction = sign(UV - vec2(0.5));
	mat4 local_to_canvas_transform = CANVAS_MATRIX * MODEL_MATRIX;
	mat2 local_to_canvas = mat2(
		local_to_canvas_transform[0].xy,
		local_to_canvas_transform[1].xy
	);

	VERTEX += inverse(local_to_canvas) * canvas_direction * geometry_margin_px;
}

float uv_bounds_mask(vec2 uv) {
	vec2 lower_bound = step(vec2(0.0), uv);
	vec2 upper_bound = step(uv, vec2(1.0));
	return lower_bound.x * lower_bound.y * upper_bound.x * upper_bound.y;
}

void fragment() {
	// ── 纹理像素 → UV 空间（缩放不变的核心）──
	// TEXTURE_PIXEL_SIZE 是 Godot 内置 uniform：每个 texel 占多少 UV 单位。
	// 例如 128×128 纹理 → texel_size ≈ (0.0078, 0.0078)
	vec2 texel_size = TEXTURE_PIXEL_SIZE;
	float texel_scale = max(texel_size.x, texel_size.y); // 取长边保证非方形图标也够宽

	float outline_uv_radius = outline_width_texels * texel_scale;
	float geometry_uv_margin = 6.0 * texel_scale;

	vec2 source_uv = (UV - vec2(geometry_uv_margin)) / (vec2(1.0) - 2.0 * vec2(geometry_uv_margin));

	vec4 base = texture(TEXTURE, clamp(source_uv, vec2(0.0), vec2(1.0))) *
		uv_bounds_mask(source_uv);
	float dilated_alpha = base.a;

	// ── 在纯 UV 空间中做方向采样（不再依赖 dFdx/dFdy 做屏幕→UV 转换）──
	for (int i = 0; i < OUTLINE_SAMPLES; i++) {
		float angle = TAU * float(i) / float(OUTLINE_SAMPLES);
		vec2 direction = vec2(cos(angle), sin(angle));
		vec2 sample_uv = source_uv + direction * outline_uv_radius;
		float sample_alpha = texture(
			TEXTURE,
			clamp(sample_uv, vec2(0.0), vec2(1.0))
		).a * uv_bounds_mask(sample_uv);
		dilated_alpha = max(dilated_alpha, sample_alpha);
	}

	float outline_coverage = smoothstep(alpha_threshold, 1.0, dilated_alpha);
	float outline_alpha = outline_coverage * outline_color.a;
	float uncovered_outline_alpha = outline_alpha * (1.0 - base.a);
	float output_alpha = base.a + uncovered_outline_alpha;

	vec3 output_rgb = vec3(0.0);
	if (output_alpha > 0.0) {
		vec3 output_premultiplied_rgb =
			base.rgb * base.a + outline_color.rgb * uncovered_outline_alpha;
		output_rgb = output_premultiplied_rgb / output_alpha;
	}

	COLOR = vec4(output_rgb, output_alpha) * canvas_modulate;
}

shader_type canvas_item;
render_mode unshaded;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 0.55);
uniform float outline_width_px : hint_range(0.5, 4.0, 0.1) = 2.0;
uniform float alpha_threshold : hint_range(0.0, 1.0, 0.01) = 0.01;

const int OUTLINE_SAMPLES = 16;

varying vec4 canvas_modulate;

void vertex() {
	canvas_modulate = COLOR;

	float geometry_margin_px = ceil(outline_width_px) + 1.0;
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
	float geometry_margin_px = ceil(outline_width_px) + 1.0;
	vec2 uv_per_screen_pixel = vec2(
		length(vec2(dFdx(UV.x), dFdy(UV.x))),
		length(vec2(dFdx(UV.y), dFdy(UV.y)))
	);
	vec2 uv_inset = geometry_margin_px * uv_per_screen_pixel;
	vec2 source_uv = (UV - uv_inset) / (vec2(1.0) - 2.0 * uv_inset);

	vec4 base = texture(TEXTURE, clamp(source_uv, vec2(0.0), vec2(1.0))) *
		uv_bounds_mask(source_uv);
	vec2 source_uv_dx = dFdx(source_uv);
	vec2 source_uv_dy = dFdy(source_uv);
	float dilated_alpha = base.a;

	for (int i = 0; i < OUTLINE_SAMPLES; i++) {
		float angle = TAU * float(i) / float(OUTLINE_SAMPLES);
		vec2 direction = vec2(cos(angle), sin(angle));
		vec2 sample_uv = source_uv +
			(source_uv_dx * direction.x + source_uv_dy * direction.y) * outline_width_px;
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

shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;
uniform int distortion_preset = 0;
uniform float distortion_strength : hint_range(0.0, 1.0) = 0.0;
uniform int preset = 0;
uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform vec4 tint_color : source_color = vec4(1.0);
uniform float tint_strength : hint_range(0.0, 1.0) = 0.0;
uniform vec2 screen_offset = vec2(0.0);

vec2 apply_distortion(vec2 uv) {
	if (distortion_preset == 0) {
		vec2 delta = uv - vec2(0.5);
		float distance_from_center = length(delta);
		vec2 direction = delta / max(distance_from_center, 0.0001);
		float falloff = 1.0 - smoothstep(0.12, 0.72, distance_from_center);
		float ripple = sin(distance_from_center * 72.0 - TIME * 8.0);
		return uv + direction * ripple * falloff * 0.012 * distortion_strength;
	}

	if (distortion_preset == 1) {
		vec2 wave = vec2(
			sin(uv.y * 34.0 + TIME * 4.0),
			sin(uv.x * 26.0 + TIME * 3.0)
		);
		return uv + wave * vec2(0.009, 0.005) * distortion_strength;
	}

	if (distortion_preset == 2) {
		float shimmer =
			sin(uv.y * 95.0 + TIME * 11.0) * 0.65 +
			sin(uv.y * 157.0 - TIME * 7.0) * 0.35;
		return uv + vec2(shimmer * 0.006 * distortion_strength, 0.0);
	}

	vec2 centered = uv - vec2(0.5);
	float radius_squared = dot(centered, centered) * 4.0;
	return vec2(0.5) + centered * (1.0 + radius_squared * 0.28 * distortion_strength);
}

vec3 apply_filter(vec3 color) {
	if (preset == 0) {
		float luminance = dot(color, vec3(0.299, 0.587, 0.114));
		return vec3(luminance);
	}

	if (preset == 1) {
		return clamp(vec3(
			dot(color, vec3(0.393, 0.769, 0.189)),
			dot(color, vec3(0.349, 0.686, 0.168)),
			dot(color, vec3(0.272, 0.534, 0.131))
		), 0.0, 1.0);
	}

	if (preset == 2) {
		return clamp(color * vec3(0.82, 0.94, 1.16), 0.0, 1.0);
	}

	if (preset == 3) {
		return clamp(color * vec3(1.14, 1.02, 0.82), 0.0, 1.0);
	}

	if (preset == 4) {
		float luminance = dot(color, vec3(0.299, 0.587, 0.114));
		return clamp(mix(color, vec3(luminance * 0.45, luminance * 1.08, luminance * 0.38), 0.72), 0.0, 1.0);
	}

	return clamp(color * vec3(0.34, 0.48, 0.72), 0.0, 1.0);
}

void fragment() {
	vec2 half_pixel = SCREEN_PIXEL_SIZE * 0.5;
	vec2 offset_uv = SCREEN_UV + screen_offset * SCREEN_PIXEL_SIZE;
	vec2 sample_uv = clamp(
		apply_distortion(offset_uv),
		half_pixel,
		vec2(1.0) - half_pixel
	);
	vec4 source = texture(screen_texture, sample_uv);
	vec3 filtered = mix(source.rgb, apply_filter(source.rgb), strength);
	vec3 tinted = filtered * tint_color.rgb;
	float tint_amount = tint_strength * tint_color.a;
	COLOR = vec4(mix(filtered, tinted, tint_amount), source.a);
}

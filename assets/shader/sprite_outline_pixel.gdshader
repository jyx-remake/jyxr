shader_type canvas_item;
render_mode unshaded;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform bool include_diagonals = true;
uniform float alpha_threshold : hint_range(0.0, 1.0, 0.01) = 0.01;

void fragment() {
	vec4 base = texture(TEXTURE, UV) * COLOR;
	float a = base.a;

	float n = 0.0;
	vec2 px = TEXTURE_PIXEL_SIZE;

	// 4邻域
	n = max(n, texture(TEXTURE, UV + vec2( 1.0,  0.0) * px).a);
	n = max(n, texture(TEXTURE, UV + vec2(-1.0,  0.0) * px).a);
	n = max(n, texture(TEXTURE, UV + vec2( 0.0,  1.0) * px).a);
	n = max(n, texture(TEXTURE, UV + vec2( 0.0, -1.0) * px).a);

	// 8邻域（可选）
	if (include_diagonals) {
		n = max(n, texture(TEXTURE, UV + vec2( 1.0,  1.0) * px).a);
		n = max(n, texture(TEXTURE, UV + vec2( 1.0, -1.0) * px).a);
		n = max(n, texture(TEXTURE, UV + vec2(-1.0,  1.0) * px).a);
		n = max(n, texture(TEXTURE, UV + vec2(-1.0, -1.0) * px).a);
	}

	bool is_outline = (a <= alpha_threshold) && (n > alpha_threshold);
	COLOR = is_outline ? outline_color : base;
}
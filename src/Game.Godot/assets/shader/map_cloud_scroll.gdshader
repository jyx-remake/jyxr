shader_type canvas_item;

uniform float scroll_period_seconds : hint_range(1.0, 600.0, 1.0) = 180.0;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	float period = max(scroll_period_seconds, 1.0);
	float phase = mod(TIME, period) / period;
	vec4 cloud_color = texture(TEXTURE, UV + vec2(phase, 0.0));
	COLOR = cloud_color * vertex_color;
}

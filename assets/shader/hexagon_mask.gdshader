shader_type canvas_item;

uniform float top_y : hint_range(0.0, 1.0) = 0.1;
uniform float middle_y : hint_range(0.0, 1.0) = 0.5;
uniform float bottom_y : hint_range(0.0, 1.0) = 0.9;
uniform float top_left_x : hint_range(-1.0, 2.0) = 0.25;
uniform float top_right_x : hint_range(-1.0, 2.0) = 0.75;
uniform float middle_left_x : hint_range(-1.0, 2.0) = 0.0;
uniform float middle_right_x : hint_range(-1.0, 2.0) = 1.0;

void fragment() {
	vec2 uv = UV;
	if (uv.y < top_y || uv.y > bottom_y) {
		discard;
	}

	float left_x;
	float right_x;
	if (uv.y < middle_y) {
		float weight = (uv.y - top_y) / max(middle_y - top_y, 0.0001);
		left_x = mix(top_left_x, middle_left_x, weight);
		right_x = mix(top_right_x, middle_right_x, weight);
	} else {
		float weight = (uv.y - middle_y) / max(bottom_y - middle_y, 0.0001);
		left_x = mix(middle_left_x, top_left_x, weight);
		right_x = mix(middle_right_x, top_right_x, weight);
	}

	if (uv.x < left_x || uv.x > right_x) {
		discard;
	}

	COLOR = texture(TEXTURE, uv);
}

shader_type canvas_item;

/*
  四周淡出（像素单位）
  - 设为 0 表示该边不淡出
  - 底部可单独加重（bottom_power）
*/
uniform float fade_left_px   : hint_range(0.0, 1024.0) = 16.0;
uniform float fade_right_px  : hint_range(0.0, 1024.0) = 16.0;
uniform float fade_top_px    : hint_range(0.0, 1024.0) = 8.0;
uniform float fade_bottom_px : hint_range(0.0, 2048.0) = 96.0;

// 总透明度倍率（方便整体微调）
uniform float alpha_mul      : hint_range(0.0, 1.0) = 1.0;

float edge_fade(float dist_to_edge, float width_uv) {
	if (width_uv <= 0.0) {
		return 1.0; // 宽度为0 -> 不做该边淡出
	}
	return smoothstep(0.0, width_uv, dist_to_edge);
}

void fragment() {
	vec4 tex = COLOR;

	float left_w   = fade_left_px   * TEXTURE_PIXEL_SIZE.x;
	float right_w  = fade_right_px  * TEXTURE_PIXEL_SIZE.x;
	float top_w    = fade_top_px    * TEXTURE_PIXEL_SIZE.y;
	float bottom_w = fade_bottom_px * TEXTURE_PIXEL_SIZE.y;

	float left_m   = edge_fade(UV.x,         left_w);
	float right_m  = edge_fade(1.0 - UV.x,   right_w);
	float top_m    = edge_fade(UV.y,         top_w);
	float bottom_m = edge_fade(1.0 - UV.y,   bottom_w);

	float edge_mask = left_m * right_m * top_m * bottom_m;

	tex.a *= edge_mask * alpha_mul;
	COLOR = tex;
}

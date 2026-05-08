shader_type canvas_item;
render_mode unshaded;

uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float width_px : hint_range(1.0, 8.0, 1.0) = 2.0; // 以纹理像素为单位
uniform float alpha_threshold : hint_range(0.0, 1.0, 0.01) = 0.01;

const int SAMPLES = 16;

void fragment() {
	vec4 base = texture(TEXTURE, UV) * COLOR;
	float center_a = base.a;

	float max_a = center_a;

	for (int i = 0; i < SAMPLES; i++) {
		float ang = TAU * float(i) / float(SAMPLES);
		vec2 dir = vec2(cos(ang), sin(ang));
		vec2 uv2 = UV + dir * TEXTURE_PIXEL_SIZE * width_px;
		max_a = max(max_a, texture(TEXTURE, uv2).a);
	}

	// 外描边强度：邻域有alpha，但当前点alpha较小
	float outline = clamp(max_a - center_a, 0.0, 1.0);

	// 过滤掉极弱噪声（图集边缘/压缩误差时有帮助）
	outline = smoothstep(alpha_threshold, 1.0, outline);

	// 只在“主体外部”显示描边
	float outer = outline * (1.0 - center_a);

	vec4 stroke = vec4(outline_color.rgb, outer * outline_color.a);

	// 合成：主体盖在描边上面
	vec4 out_col;
	out_col.a = max(base.a, stroke.a);

	if (out_col.a > 0.0) {
		out_col.rgb = (base.rgb * base.a + stroke.rgb * stroke.a * (1.0 - base.a)) / out_col.a;
	} else {
		out_col.rgb = vec3(0.0);
	}

	COLOR = out_col;
}
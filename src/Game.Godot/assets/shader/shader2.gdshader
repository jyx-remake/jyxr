shader_type canvas_item;

// 定义两个纹理（图片 A 和图片 B）
uniform sampler2D textureA;

// 定义纹理的尺寸
uniform vec2 textureA_size;
uniform vec2 textureB_size;

void fragment() {
	vec4 color = texture(TEXTURE, UV);
    vec2 uv = UV;

    // 计算图片 A 的偏移，使其中心部分与图片 B 对齐
    vec2 offset = (textureA_size - textureB_size) / (2.0 * textureA_size);

    // 调整 UV 坐标以匹配图片 B 的尺寸
    vec2 uvA = uv * (textureB_size / textureA_size) + offset;

    // 采样图片 A 和图片 B 的颜色
    vec4 mask = texture(textureA, uvA);

	color.a *= mask.a;
    // 输出最终颜色
    COLOR = color;
}

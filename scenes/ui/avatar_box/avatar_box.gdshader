shader_type canvas_item;

uniform vec2 center = vec2(0.5, 0.5); // 圆心（UV 空间）
uniform float radius = 0.5;           // 半径（UV 空间 0~0.5 左右）

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);

    // 计算当前像素到圆心的距离
    float dist = distance(UV, center);

    // 超出半径的地方不画（直接丢弃像素）
    if (dist > radius) {
        discard;
    }

    COLOR = tex_color;
}

shader_type canvas_item;

uniform sampler2D mask_tex; // 图片2：用作遮罩
uniform bool mask_use_red_channel = false; // 图片2 如果是黑白图可用 R 通道

void fragment() {
    // 头像本身的颜色
    vec4 base = texture(TEXTURE, UV);

    // 取遮罩贴图的颜色
    vec4 m = texture(mask_tex, UV);

    // 默认用 alpha 通道做遮罩，不够就用 R 通道
    float mask = mask_use_red_channel ? m.r : m.a;

    // 用遮罩控制透明度：遮罩透明的地方就变成透明
    base.a *= mask;

    COLOR = base;
}

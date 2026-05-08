shader_type canvas_item;

uniform float pressed : hint_range(0.0, 1.0) = 0.0;
uniform vec2 press_offset_px = vec2(2.0, 2.0);
uniform float darken : hint_range(0.0, 0.8) = 0.1;

void fragment() {
    vec2 off = press_offset_px * TEXTURE_PIXEL_SIZE * pressed;
    vec4 col = texture(TEXTURE, UV - off);

    col.rgb *= mix(1.0, 1.0 - darken, pressed);
    COLOR = col;
}

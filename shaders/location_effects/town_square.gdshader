shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

float hash21(vec2 p) {
	p = fract(p * vec2(127.1, 311.7));
	p += dot(p, p + 19.19);
	return fract(p.x * p.y);
}

void fragment() {
	vec2 uv = FRAGCOORD.xy * SCREEN_PIXEL_SIZE;
	float t  = TIME;

	vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec3 col = screen.rgb;

	// ── Subtle cold desaturation — the town feels drained ────────────────────
	float lum = dot(col, vec3(0.299, 0.587, 0.114));
	col = mix(col, vec3(lum), 0.18);
	col *= vec3(0.93, 0.94, 1.0);   // faint cool cast

	// ── Fine film grain ───────────────────────────────────────────────────────
	float grain = hash21(uv + fract(t * 0.31)) * 2.0 - 1.0;
	col += grain * 0.025;

	// ── Very gentle vignette — public space, not a dungeon ───────────────────
	vec2  c    = uv - 0.5;
	float vign = 1.0 - smoothstep(0.52, 0.88, length(c) * 1.3);
	col *= mix(0.88, 1.0, vign);

	// ── Occasional slow flicker in peripheral vision (top corners) ───────────
	float flicker_l = smoothstep(0.6, 0.0, length(uv - vec2(0.08, 0.12)));
	float flicker_r = smoothstep(0.6, 0.0, length(uv - vec2(0.92, 0.08)));
	float fl = sin(t * 7.3 + 1.1) * sin(t * 3.7) * 0.5 + 0.5;
	fl = step(0.92, fl) * 0.06;
	col += (flicker_l + flicker_r) * fl * vec3(0.6, 0.65, 0.8);

	COLOR = vec4(col, screen.a);
}

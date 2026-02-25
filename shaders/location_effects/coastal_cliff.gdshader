shader_type canvas_item;

// Coastal Cliff — driving rain, sea swell, pulsing lights from the deep
// Runs at layer 64, below the 1-bit dither. Warps UV and modifies luminance.

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

float hash1(float n) {
	return fract(sin(n) * 43758.5453123);
}

void fragment() {
	// Sea swell — horizontal UV displacement that oscillates with vertical
	// position, making the world appear to rock on a slow, inhuman tide.
	vec2 uv = SCREEN_UV;
	uv.x += sin(uv.y * 4.2 + TIME * 1.6) * 0.007
	      + sin(uv.y * 9.7 + TIME * 0.9) * 0.003;

	vec3 color = texture(screen_texture, uv).rgb;
	float lum = dot(color, vec3(0.299, 0.587, 0.114));

	// Rain streaks — 40 columns of angled falling lines.
	// Each column has its own speed, angle, and streak length.
	float num_cols = 40.0;
	float col_id   = floor(SCREEN_UV.x * num_cols);

	float rain_angle  = 0.06 + hash1(col_id * 3.1)  * 0.04;  // slight diagonal
	float rain_speed  = 0.35 + hash1(col_id * 1.7)  * 0.55;
	float rain_offset = hash1(col_id * 5.3);
	float streak_len  = 0.04 + hash1(col_id * 2.9)  * 0.05;

	// Tilt the stripe by offsetting x proportionally to y
	float col_frac    = fract(SCREEN_UV.x * num_cols) - 0.5;
	float eff_x       = col_frac - rain_angle * (SCREEN_UV.y - 0.5);
	float streak_x    = 1.0 - smoothstep(0.0, 1.3 / num_cols, abs(eff_x));

	float streak_y    = fract(SCREEN_UV.y - rain_speed * TIME - rain_offset);
	float streak_fade = smoothstep(0.0, 0.004, streak_y)
	                  * smoothstep(streak_len + 0.01, streak_len, streak_y);

	lum += streak_x * streak_fade * 0.55;

	// Strange lights from beneath the waves — two interference patterns of
	// slow pulses that brighten the bottom of the screen.
	float depth   = 1.0 - SCREEN_UV.y;   // 0 at bottom, 1 at top
	float light_a = sin(TIME * 1.8 + SCREEN_UV.x * 9.1) * 0.5 + 0.5;
	float light_b = sin(TIME * 2.6 + SCREEN_UV.x * 6.3 + 1.4) * 0.5 + 0.5;
	float sea_glow = (light_a * 0.55 + light_b * 0.45)
	               * smoothstep(0.55, 0.0, depth) * 0.28;
	lum += sea_glow;

	// Wind turbulence — a fast, low-amplitude flicker across the whole image,
	// like the air itself is being torn apart.
	float wind = sin(TIME * 8.3) * sin(TIME * 3.7 + 0.9) * 0.018;
	lum += wind;

	COLOR = vec4(vec3(clamp(lum, 0.0, 1.0)), 1.0);
}

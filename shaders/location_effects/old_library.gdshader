shader_type canvas_item;

// Old Library — dusty tomes, forgotten knowledge, ancient whispers
// Runs at layer 64, below the 1-bit dither. Outputs luminance only.

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

float hash1(float n) {
	return fract(sin(n) * 43758.5453123);
}

float hash2(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	vec3 color = texture(screen_texture, SCREEN_UV).rgb;
	float lum = dot(color, vec3(0.299, 0.587, 0.114));

	// Photographic grain — slow-evolving noise evoking aged paper and film
	float grain = (hash2(FRAGCOORD.xy + floor(TIME * 10.0)) - 0.5) * 0.06;
	lum += grain;

	// Floating dust motes — 16 particles ascending with a gentle side-sway.
	// Each has a unique speed, sway frequency, and brightness that slowly varies.
	for (int i = 0; i < 16; i++) {
		float fi = float(i);
		float x_origin    = hash1(fi * 1.618);
		float y_origin    = hash1(fi * 2.718);
		float drift_speed = 0.018 + hash1(fi * 3.14)  * 0.035;
		float sway_speed  = 0.20  + hash1(fi * 5.77)  * 0.30;
		float sway_amt    = 0.008 + hash1(fi * 7.39)  * 0.012;
		float brightness  = 0.35  + hash1(fi * 11.1 + floor(TIME * 0.4)) * 0.30;

		vec2 pos = vec2(
			x_origin + sin(TIME * sway_speed + fi) * sway_amt,
			fract(y_origin - TIME * drift_speed)   // ascend, wrap at top
		);

		float d = length(SCREEN_UV - pos);
		lum += smoothstep(0.006, 0.001, d) * brightness;
	}

	// The library breathes — a slow dual-frequency luminance pulse, like
	// something vast and patient existing between the shelves.
	float breath = sin(TIME * 0.22) * 0.025 + cos(TIME * 0.11) * 0.015;
	lum += breath;

	COLOR = vec4(vec3(clamp(lum, 0.0, 1.0)), 1.0);
}

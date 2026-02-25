shader_type canvas_item;

// Abandoned Shrine — reality tears, malevolent shadows, altar pulse
// Runs at layer 64, below the 1-bit dither. Warps UV and modifies luminance.

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

float hash2(vec2 p) {
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
}

void fragment() {
	// Reality distortion — layered sine-wave UV warp. The shrine's geometry
	// cannot be trusted; walls lean into impossible angles.
	vec2 uv = SCREEN_UV;
	float t = TIME * 0.45;
	uv.x += sin(uv.y * 6.3  + t)       * 0.009
	      + sin(uv.y * 13.7 + t * 0.6) * 0.004;
	uv.y += cos(uv.x * 4.8  + t * 0.8) * 0.007;

	vec3 color = texture(screen_texture, uv).rgb;
	float lum = dot(color, vec3(0.299, 0.587, 0.114));

	// Altar shadow rings — dark concentric waves radiating outward from the
	// centre of the screen, as if something beneath the altar is pulsing.
	vec2 centred = SCREEN_UV - 0.5;
	float altar_r = length(centred) * 2.2;
	float rings = sin(altar_r * 8.0 - TIME * 2.8) * 0.5 + 0.5;
	lum -= rings * 0.14 * smoothstep(0.2, 0.9, altar_r);

	// Malevolent corner shadows — darkness breathes inward from the edges.
	// Two independent frequencies multiply so the pattern never fully repeats.
	float corner_dist  = max(abs(centred.x), abs(centred.y));
	float shadow_pulse = (sin(TIME * 1.3) * 0.5 + 0.5)
	                   * (sin(TIME * 0.7 + 1.5) * 0.5 + 0.5);
	lum -= smoothstep(0.25, 0.5, corner_dist) * shadow_pulse * 0.4;

	// Corruption tears — random dark horizontal glitch bands that flash
	// briefly and shift the content sideways, like a signal breaking down.
	float row      = floor(FRAGCOORD.y / 3.0);
	float tear_rng = hash2(vec2(row, floor(TIME * 4.0)));
	if (tear_rng > 0.96) {
		float shift   = (hash2(vec2(row + 0.3, floor(TIME * 4.0))) - 0.5) * 0.06;
		float torn    = dot(texture(screen_texture,
		                    vec2(SCREEN_UV.x + shift, SCREEN_UV.y)).rgb,
		                    vec3(0.299, 0.587, 0.114));
		float strength = (tear_rng - 0.96) / 0.04;
		lum = mix(lum, torn * 0.25, strength);
	}

	COLOR = vec4(vec3(clamp(lum, 0.0, 1.0)), 1.0);
}

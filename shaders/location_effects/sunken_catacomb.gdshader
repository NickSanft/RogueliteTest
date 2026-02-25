shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

// ── Caustic helpers ──────────────────────────────────────────────────────────

float hash21(vec2 p) {
	p = fract(p * vec2(127.1, 311.7));
	p += dot(p, p + 19.19);
	return fract(p.x * p.y);
}

// Simple 2-layer caustic: interference rings from two moving point sources
float caustic(vec2 uv, float t) {
	vec2 a = vec2(sin(t * 0.31) * 0.25 + 0.5, cos(t * 0.19) * 0.2 + 0.4);
	vec2 b = vec2(cos(t * 0.23) * 0.25 + 0.5, sin(t * 0.27) * 0.2 + 0.6);
	float da = length(uv - a);
	float db = length(uv - b);
	float rings = sin(da * 38.0 - t * 1.4) * sin(db * 34.0 - t * 1.1);
	return clamp(rings * 0.5 + 0.5, 0.0, 1.0);
}

// ── Drip helpers ─────────────────────────────────────────────────────────────

float drip(vec2 uv, float col_offset, float speed, float t) {
	float x = fract(col_offset);
	float band = 0.012;
	if (abs(uv.x - x) > band) return 0.0;

	float phase  = fract(t * speed + col_offset * 7.3);
	float head_y = phase;
	float trail  = clamp((head_y - uv.y) * 18.0, 0.0, 1.0)
	              * clamp((uv.y - (head_y - 0.18)) * 10.0, 0.0, 1.0);
	float head   = smoothstep(0.016, 0.0, abs(uv.y - head_y));
	return max(head, trail * 0.35);
}

void fragment() {
	vec2 uv = FRAGCOORD.xy * SCREEN_PIXEL_SIZE;
	float t  = TIME;

	// ── Slow breathing vignette (oppressive, tight) ───────────────────────────
	float pulse = sin(t * 0.38) * 0.5 + 0.5;                 // 0..1
	float breathe = mix(0.62, 0.72, pulse);                   // vignette radius
	vec2  center = uv - 0.5;
	float vign = 1.0 - smoothstep(breathe * 0.55, breathe, length(center));
	vign = pow(vign, 2.2);

	// ── Water caustics on ceiling (upper third) ───────────────────────────────
	float caus_mask = smoothstep(0.38, 0.18, uv.y);           // only top ~38%
	float caus = caustic(uv, t) * caus_mask;

	// ── Dripping streaks (9 irregular columns) ────────────────────────────────
	float drips = 0.0;
	drips += drip(uv, 0.09, 0.17, t);
	drips += drip(uv, 0.21, 0.24, t);
	drips += drip(uv, 0.33, 0.13, t);
	drips += drip(uv, 0.44, 0.31, t);
	drips += drip(uv, 0.55, 0.19, t);
	drips += drip(uv, 0.63, 0.27, t);
	drips += drip(uv, 0.73, 0.15, t);
	drips += drip(uv, 0.82, 0.22, t);
	drips += drip(uv, 0.91, 0.29, t);
	drips = clamp(drips, 0.0, 1.0);

	// ── Cold color grade: shift toward deep teal-black ───────────────────────
	vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);
	vec3 col = screen.rgb;

	// Desaturate slightly then push cold
	float lum = dot(col, vec3(0.299, 0.587, 0.114));
	col = mix(col, vec3(lum), 0.28);
	col *= vec3(0.72, 0.88, 1.0);   // cold tint

	// Apply caustic shimmer (very subtle — adds a pale highlight)
	col += vec3(0.06, 0.1, 0.12) * caus * 0.6;

	// Apply drips (pale water streaks)
	col = mix(col, vec3(0.55, 0.72, 0.8), drips * 0.45);

	// Apply vignette: crush toward near-black at edges
	col *= vign;
	col = mix(col, vec3(0.0, 0.02, 0.04), 1.0 - vign);

	COLOR = vec4(col, screen.a);
}

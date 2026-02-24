shader_type canvas_item;

uniform sampler2D screen_texture : hint_screen_texture, filter_nearest;

// Palette
uniform vec3 color_dark  : source_color = vec3(0.0, 0.0, 0.0);
uniform vec3 color_light : source_color = vec3(1.0, 1.0, 1.0);

// Dithering
uniform float dither_strength : hint_range(0.0, 1.0) = 1.0;

// Luminance shaping — applied before the dither threshold
uniform float contrast  : hint_range(0.5, 4.0) = 1.5;  // >1 pushes mid-grays toward extremes
uniform float brightness : hint_range(-0.5, 0.5) = 0.0; // shifts the overall exposure

// Atmosphere
uniform float vignette_strength  : hint_range(0.0, 2.0) = 0.6;  // darkens edges in luminance space
uniform float scanline_strength  : hint_range(0.0, 0.5) = 0.12; // darkens every other row

// Animation — cycles the Bayer pattern through its 4 quadrant offsets each second,
// creating a subtle "living static" shimmer. Set to 0 to disable.
uniform float animation_speed : hint_range(0.0, 16.0) = 4.0;

// Optional low-res pixel look (1 = off, 2–4 = progressively chunkier)
uniform float pixel_size : hint_range(1.0, 4.0, 1.0) = 1.0;

// 8x8 Bayer ordered dithering matrix (values normalised to [0, 1])
const float bayer_matrix[64] = float[](
	 0.0/64.0, 32.0/64.0,  8.0/64.0, 40.0/64.0,  2.0/64.0, 34.0/64.0, 10.0/64.0, 42.0/64.0,
	48.0/64.0, 16.0/64.0, 56.0/64.0, 24.0/64.0, 50.0/64.0, 18.0/64.0, 58.0/64.0, 26.0/64.0,
	12.0/64.0, 44.0/64.0,  4.0/64.0, 36.0/64.0, 14.0/64.0, 46.0/64.0,  6.0/64.0, 38.0/64.0,
	60.0/64.0, 28.0/64.0, 52.0/64.0, 20.0/64.0, 62.0/64.0, 30.0/64.0, 54.0/64.0, 22.0/64.0,
	 3.0/64.0, 35.0/64.0, 11.0/64.0, 43.0/64.0,  1.0/64.0, 33.0/64.0,  9.0/64.0, 41.0/64.0,
	51.0/64.0, 19.0/64.0, 59.0/64.0, 27.0/64.0, 49.0/64.0, 17.0/64.0, 57.0/64.0, 25.0/64.0,
	15.0/64.0, 47.0/64.0,  7.0/64.0, 39.0/64.0, 13.0/64.0, 45.0/64.0,  5.0/64.0, 37.0/64.0,
	63.0/64.0, 31.0/64.0, 55.0/64.0, 23.0/64.0, 61.0/64.0, 29.0/64.0, 53.0/64.0, 21.0/64.0
);

// Look up the Bayer threshold for the current pixel.
// The temporal offset cycles through the matrix's four 4x4 quadrants to create
// a low-frequency shimmer without disrupting the ordered pattern.
float get_bayer_value(vec2 frag_pos) {
	int frame   = int(TIME * animation_speed);
	int offset_x = (frame % 2) * 4;
	int offset_y = ((frame / 2) % 2) * 4;

	ivec2 pixel = ivec2(frag_pos);
	int x = (pixel.x + offset_x) % 8;
	int y = (pixel.y + offset_y) % 8;
	return bayer_matrix[y * 8 + x];
}

void fragment() {
	// --- 1. Sample the screen (with optional pixel-art downsampling) ---
	vec2 snapped_uv = floor(SCREEN_UV / (SCREEN_PIXEL_SIZE * pixel_size))
	                  * (SCREEN_PIXEL_SIZE * pixel_size);
	vec3 color = texture(screen_texture, snapped_uv).rgb;

	// --- 2. Perceptual luminance ---
	float lum = dot(color, vec3(0.299, 0.587, 0.114));

	// --- 3. Brightness & contrast (centred at 0.5 so neither shifts the midpoint alone) ---
	lum = clamp(0.5 + (lum + brightness - 0.5) * contrast, 0.0, 1.0);

	// --- 4. Vignette (in luminance space so it pushes edge pixels toward black in the dither) ---
	vec2 centered = SCREEN_UV - 0.5;
	float vignette = 1.0 - smoothstep(0.2, 0.7, length(centered * vec2(1.4, 1.0))) * vignette_strength;
	lum *= vignette;

	// --- 5. Scanlines (darken every other row before dithering, not after) ---
	float scanline_darken = step(1.0, mod(FRAGCOORD.y, 2.0)) * scanline_strength;
	lum = max(0.0, lum - scanline_darken);

	// --- 6. Bayer dithering ---
	// mix(0.5, bayer, dither_strength): at strength=0 every pixel uses a hard 0.5 cutoff;
	// at strength=1 the full Bayer pattern is used.
	float threshold = mix(0.5, get_bayer_value(FRAGCOORD.xy), dither_strength);

	// --- 7. 1-bit output ---
	COLOR = vec4((lum >= threshold) ? color_light : color_dark, 1.0);
}

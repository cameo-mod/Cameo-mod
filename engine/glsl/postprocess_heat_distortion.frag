#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_DISTORTIONS 16

uniform sampler2D WorldTexture;
uniform vec2 DistortionCenters[MAX_DISTORTIONS];
uniform float DistortionRadii[MAX_DISTORTIONS];
uniform float DistortionStrengths[MAX_DISTORTIONS];
uniform float DistortionCount;
uniform float Time;

out vec4 fragColor;

void main()
{
	vec2 fc = gl_FragCoord.xy;
	vec2 sz = vec2(textureSize(WorldTexture, 0));
	int count = int(DistortionCount);

	vec2 offset = vec2(0.0);
	for (int i = 0; i < MAX_DISTORTIONS; ++i)
	{
		if (i >= count)
			break;

		float r = DistortionRadii[i];
		float d = distance(fc, DistortionCenters[i]);
		float falloff = exp(-d * d / (r * r));
		float s = DistortionStrengths[i] * falloff;

		// Heat-haze on a DIAGONAL axis to match OpenRA's isometric terrain grid (a pure horizontal
		// axis fights the grid and reads unnaturally). The fc.x term tilts the ripple bands diagonally,
		// and the combined x (full) + y (partial) offset gives a bottom-left<->top-right shimmer that
		// scrolls over time. Flip the Time sign to reverse the crawl direction.
		float phase = fc.y * 0.06 - Time * 4.0;
		offset.x += sin(phase + fc.x * 0.02) * s;
		offset.y += cos(phase) * s * 0.35;
	}

	vec2 maxCoord = sz - vec2(1.0);
	ivec2 sampleCoord = ivec2(clamp(fc + offset, vec2(0.0), maxCoord));
	fragColor = texelFetch(WorldTexture, sampleCoord, 0);
}

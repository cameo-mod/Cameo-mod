#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_BEAMS 16

uniform sampler2D WorldTexture;
uniform vec2 BeamStarts[MAX_BEAMS];
uniform vec2 BeamEnds[MAX_BEAMS];
uniform vec3 GlowColors[MAX_BEAMS];
uniform float GlowIntensities[MAX_BEAMS];
uniform float GlowRadii[MAX_BEAMS];
uniform float BeamCount;

out vec4 fragColor;

float segmentDist(vec2 p, vec2 a, vec2 b)
{
	vec2 ab = b - a;
	float t = clamp(dot(p - a, ab) / max(dot(ab, ab), 0.0001), 0.0, 1.0);
	return length(p - (a + t * ab));
}

void main()
{
	vec4 c = texelFetch(WorldTexture, ivec2(gl_FragCoord.xy), 0);
	vec3 rgb = c.rgb;
	int count = int(BeamCount);

	for (int i = 0; i < MAX_BEAMS; ++i)
	{
		if (i >= count)
			break;

		float d = segmentDist(gl_FragCoord.xy, BeamStarts[i], BeamEnds[i]);
		float r = GlowRadii[i];
		float glow = GlowIntensities[i] * exp(-d * d / (r * r));
		vec3 contrib = GlowColors[i] * glow;
		rgb = rgb + contrib * (1.0 - rgb);
	}

	fragColor = vec4(rgb, c.a);
}

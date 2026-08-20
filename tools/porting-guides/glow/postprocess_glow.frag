#version {VERSION}
#ifdef GL_ES
precision mediump float;
#endif

#define MAX_BEAMS 20

uniform sampler2D WorldTexture;
uniform vec2 BeamStarts[MAX_BEAMS];
uniform vec2 BeamEnds[MAX_BEAMS];
uniform vec3 GlowColors[MAX_BEAMS];
uniform float GlowIntensities[MAX_BEAMS];
uniform float GlowRadii[MAX_BEAMS];
uniform float GlowRadiiEnd[MAX_BEAMS];
uniform float EndpointBoosts[MAX_BEAMS];
uniform float SelfBrightens[MAX_BEAMS];
uniform float GlowFadeEnds[MAX_BEAMS];
uniform float EdgeExponentStarts[MAX_BEAMS];
uniform float EdgeExponentEnds[MAX_BEAMS];
uniform float EndpointSquashes[MAX_BEAMS];
uniform float PoolRadii[MAX_BEAMS];
uniform float BeamCount;

out vec4 fragColor;

// Returns (distance, t) where t is the clamped projection parameter along [a, b].
vec2 segmentDistT(vec2 p, vec2 a, vec2 b)
{
	vec2 ab = b - a;
	float t = clamp(dot(p - a, ab) / max(dot(ab, ab), 0.0001), 0.0, 1.0);
	return vec2(length(p - (a + t * ab)), t);
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

		// Beam body: radius tapers from GlowRadii (source) to GlowRadiiEnd (wide end), the edge
		// exponent (2 = Gaussian) tapers from EdgeExponentStarts to EdgeExponentEnds so the cross
		// section can be crisp near the source and soften further out, and brightness fades from
		// full at the source to GlowFadeEnds at the wide end (a plain linear fade, as if scattering
		// into fog). This is the whole effect for a uniform beam and for every non-searchlight glow.
		vec2 dt = segmentDistT(gl_FragCoord.xy, BeamStarts[i], BeamEnds[i]);
		float d = dt.x;
		float t = dt.y;

		float r = mix(GlowRadii[i], GlowRadiiEnd[i], t);
		float edgeExponent = mix(EdgeExponentStarts[i], EdgeExponentEnds[i], t);
		float bodyFade = mix(1.0, GlowFadeEnds[i], t);
		float falloff = bodyFade * exp(-pow(d / max(r, 0.0001), edgeExponent));

		// Endpoint pool: a separate glow centred on BeamEnds, brightness EndpointBoosts (peak,
		// values > 1 punch through brighter than the body), radius PoolRadii independent of the body
		// width, flattened vertically by EndpointSquashes into a ground-hugging ellipse. The pool and
		// body are combined with a p-norm soft-max (pow(a^P + b^P, 1/P)): it rounds the crossover so
		// there is no hard-max crease/ring, yet unlike a polynomial smooth-max it returns 0 where both
		// are 0 (no screen-wide brightness floor). EndpointBoosts == 0 (every non-searchlight glow, and
		// the uniform Beam shape) skips the pool entirely, leaving the body falloff byte-identical.
		if (EndpointBoosts[i] > 0.0)
		{
			vec2 toEnd = gl_FragCoord.xy - BeamEnds[i];
			float poolD = length(vec2(toEnd.x, toEnd.y / max(EndpointSquashes[i], 0.0001)));
			float pool = EndpointBoosts[i] * exp(-pow(poolD / max(PoolRadii[i], 0.0001), EdgeExponentEnds[i]));
			float P = 3.0;
			falloff = pow(pow(falloff, P) + pow(pool, P), 1.0 / P);
		}

		// Colored additive glow (screen blend, asymptotic to white).
		vec3 contrib = GlowColors[i] * (GlowIntensities[i] * falloff);
		rgb = rgb + contrib * (1.0 - rgb);

		// Self-brighten: a radial gamma lift on the scene's own pixels (no added color). Gamma < 1
		// raises shadows and midtones strongly while leaving highlights at white (no blow-out) and
		// black at black, so the sprite under the muzzle reads as lit rather than washed out.
		float gamma = 1.0 + SelfBrightens[i] * falloff;
		rgb = pow(max(rgb, 0.0), vec3(1.0 / gamma));
	}

	fragColor = vec4(rgb, c.a);
}

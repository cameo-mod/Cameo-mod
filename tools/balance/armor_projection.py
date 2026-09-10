"""Comparison arithmetic for reviewed per-source armor curves; no stat writeback.

Callers must first select applicable channels, target state and damage basis.
Interpolation is linear at equally spaced class positions, a diagnostic assumption.
This module does not infer missing armor mappings or battlefield hit probabilities.
"""
from fractions import Fraction
from math import exp, log

VEHICLE_ARMORS = ('scout', 'light', 'medium', 'heavy', 'superheavy')


def interpolate_vehicle_endpoints(light, heavy):
    """Aedis mapping: source Light -> Scout, Heavy -> Superheavy."""
    a, b = Fraction(str(light)), Fraction(str(heavy))
    return {armor: a + (b - a) * Fraction(i, 4)
            for i, armor in enumerate(VEHICLE_ARMORS)}


def combined_flat_curve(channels, axes):
    """Sum selected flat channels WITHIN one source before source averaging.

Each channel supplies damage and a complete reviewed versus map. Missing values
are not silently treated as 100 or zero. Output is nominal center-impact damage;
engine rounding, bursts, modifiers and target eligibility belong to the caller.
"""
    if not channels:
        raise ValueError('no selected channels')
    return {axis: sum((Fraction(str(c['damage'])) * Fraction(str(c['versus'][axis])) / 100
                       for c in channels), Fraction()) for axis in axes}


def four_voice_means(curves, required_sources, axis, *, blend=None):
    """Exactly four unique voices, each 25%; no missing-source renormalization.

Optional blend is the explicit arithmetic share in [0,1]. There is no default
policy for mixing arithmetic and geometric results. Zero remains a real zero.
"""
    required = set(required_sources)
    if len(required) != 4 or set(curves) != required:
        raise ValueError('exactly the four named source curves are required')
    values = [float(curves[source][axis]) for source in sorted(required)]
    if any(v < 0 for v in values):
        raise ValueError('negative damage cannot use this comparison')
    arithmetic = sum(values) / 4
    geometric = 0.0 if 0 in values else exp(sum(log(v) for v in values) / 4)
    result = {'arithmetic': arithmetic, 'geometric': geometric}
    if blend is not None:
        if not 0 <= blend <= 1:
            raise ValueError('arithmetic share must be within [0,1]')
        result['blend'] = blend * arithmetic + (1 - blend) * geometric
    return result

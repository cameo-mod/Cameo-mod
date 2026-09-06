# Paired effect+sound templates — one inherit carries both

**Maintainer order, 2026-09-07.** Status: **SPECIFIED, NOT BUILT.** This document is the
spec; nothing has been implemented yet.

---

## The order, in the maintainer's words

> *"What I wanted for the effects was to make a new inherit list for all the different
> effects we have from each game and then name them like the file name and map a fitting
> sound together with the visual effect so the two things are always linked as a single
> inherit. For example let's say the effect is called big explosion and is from D2K then
> name the effect `^d2k_big_explosion` and map the sound effect that usually goes with
> that effect."*

Prompted by a concrete symptom: **the Dune rocket trooper's inherited effect sounds
wrong** — the visual and the audio do not belong together, because they are chosen
independently today.

## The problem

A `CreateEffect` warhead carries the visual (`Explosions:` / `Image:` / `Sequence:`) and
the audio (`ImpactSounds:`) as **separate fields that nothing keeps in step**. Two
weapons using the same explosion sprite can carry different sounds, and a weapon can
inherit a visual from one template and a sound from another — or declare one locally and
inherit the other. `audit_weapon_shape` W6 counts **694 weapons declaring an effect
warhead locally**, so this is the common case, not the exception.

There is no place where "this sprite goes with this sound" is written down once.

## Measured, 2026-09-07 — the problem is real and sized

```
CreateEffect warheads on concrete weapons     6987
   with a visual  5010     with a sound  5985     with BOTH  4008
distinct (visual, sound) pairings              331
visuals used with MORE THAN ONE sound           68   <- the mismatches
```

Only **4008 of 6987** effect warheads carry both halves; the rest are half-specified.
And 68 sprites are each paired with several different sounds, e.g.

```
small_poof           -> expnew13.aud / kaboom12.aud / kaboom15.aud
small_frag           -> expnew09.aud / expnew12.aud / expnew14.aud
piffs                -> EXPLSML1.WAV / kaboom12.aud / kaboom15.aud
small_explosion_air  -> kaboom25.aud / xplos.aud
```

### The reported symptom, located

The Dune rocket trooper is not a vague complaint — it is one line:

```
D2K_Rocket_Trooper          Warhead@Effect  visual d2k_tiny_explosion
                                            sound  xplobig4.aud      <- BIG sound,
                                                                        TINY sprite
D2K_Rocket_Trooper2         Warhead@Effect  visual d2k_small_napalm  sound kaboom12.aud
D2K_Rocket_Trooper_AA       Warhead@Effect  visual d2k_small_explosion
                                            sound  EXPLSML1.WAV      <- a RA/TD sound
                                                                        on a D2k visual
```

Four variants of one unit, four different pairings, one of them a big-explosion sound on
a tiny-explosion sprite and another borrowing a sound from a different game. Nothing is
enforcing that the two halves belong together, which is exactly what this spec fixes.

**Start here when building:** these five weapons are the acceptance test.

## The design

**One template per real effect, named after the source game and the effect's own file
name, carrying BOTH halves.** The name is the contract:

```
^d2k_big_explosion          the D2k large-explosion sprite + the sound that ships with it
^ra1_napalm_burst
^ts_ion_impact
```

* `^<game>_<effect_file_stem>` — no invented names. **Read the actual sprite/sequence
  names and use them**; interpret only where a filename is unreadable, and record the
  interpretation in this file.
* Every template sets the visual **and** `ImpactSounds` together. Neither half is ever
  set on a weapon.
* This slots straight into the ONE-WARHEAD / THREE-INHERIT law (DESIGN §11b.1): the
  effect inherit is the third of the three, so **a weapon gets its effect exactly once,
  as `Inherits@fx: ^<game>_<effect>`**, and W6 falls as the templates land.

## Why the naming rule matters more than it looks

Naming a template after the **file** rather than after a role (`^Effect_Big`,
`^Effect_Medium`) means two people cannot disagree about which sprite it is, and a
missing pairing is visible as a missing file. Role-named effect templates are what
allowed a Dune rocket trooper to end up with a mismatched sound in the first place: the
role said "medium explosion", and the sound that got attached was whatever the nearest
template happened to carry.

## Build order

1. **Inventory.** Enumerate every effect sprite actually referenced by a `CreateEffect`
   warhead, grouped by source game, with the sounds each is currently paired with. Where
   one sprite already has a dominant sound, that is the pairing — **measure it, do not
   invent it.**
2. **Report the conflicts.** Any sprite used with more than one sound is a design
   decision, not a merge: list them for the maintainer with the usage counts.
3. **Generate the templates** into the shared effect file, `^<game>_<stem>`, each with
   the visual and `ImpactSounds` together.
4. **Convert weapons in batches**, `Inherits@fx:` only, deleting the local effect
   warhead. Boot-gate each batch. W6 must fall and never rise.
5. **Guard it**: an audit that fails when a weapon declares an effect visual or an
   `ImpactSounds` locally instead of inheriting a paired template.

⚠ **Start with the Dune rocket trooper**, since that is the reported symptom and it will
show immediately whether the pairing is right.

⛔ **This touches warheads.** `CreateEffect` warheads are warheads, and DESIGN forbids
changing a warhead without explicit permission — this document IS that permission for
the effect half, and for nothing else. Damage warheads, `Burst` and `BurstDelays` are
untouched.

## Open questions for the maintainer

1. Where do the templates live — one shared file per game, or one global effect file?
2. What happens to an effect with **no** sound today: pick a neighbour's, or leave
   `ImpactSounds` unset and record it as a gap?
3. Cross-game reuse — may an RA2 weapon inherit `^d2k_big_explosion` if that is genuinely
   the right sprite, or does each game get its own copy?

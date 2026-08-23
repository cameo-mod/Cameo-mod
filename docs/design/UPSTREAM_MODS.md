# Tracking the upstream mods

**Owns:** how Cameo absorbs work from the other OpenRA mods it descends from or wants to follow —
Combined Arms, Crystallized Nexus, Romanov's Vengeance and Shattered Paradise — what may be
adopted automatically, and what may never be. Measured, not assumed — every number below comes from
`python tools/audit/audit_ca_drift.py` and from the two git histories.

> Maintainer, 2026-08-23: *"Cameo is like the Frankenstein Monster of all the OpenRA mods, it
> tries to combine everything into one single mod."* That is the goal this document serves —
> absorb CA's mechanics without losing what Cameo already has.

---

## 1. The lineage, and the one fact that decides the strategy

Cameo began as a fork of CA, which is why `OpenRA.Mods.CA/` sits at the repo ROOT (it is mod
code, exactly like `OpenRA.Mods.Cameo/`, and is **not** part of `engine/`). Cameo's ENGINE later
moved to the Romanov's Vengeance / Shattered Paradise line, which brings `OpenRA.Mods.AS`.
That line is no longer maintained, which is what raises the question of returning to CA.

**Measured against the two engine repositories:**

| | |
|---|---|
| common ancestor | `0eb173e046`, **2024-11-16** (both are forks of upstream OpenRA) |
| Cameo engine ahead of it | **2 581 commits** (`cameo-mod/OpenRA`, branch `cameo-engine`) |
| CA engine ahead of it | **111 commits** (`Inq8/OpenRA`, branch `ca-engine/1.09`) |
| `OpenRA.Mods.AS` in CA's engine | **absent** — CA ships only `Cnc`, `Common`, `D2k` |

⛔ **Therefore: do NOT move Cameo's engine onto `ca-engine`.** It is not an upgrade, it is a
23-fold regression — it would discard 2 581 commits and delete the entire `OpenRA.Mods.AS`
assembly, which Cameo depends on (`mod.yaml` loads it FIRST in the assembly order, ahead of CA).
"Getting back to CA" has to mean the opposite direction of travel:

> **Bring CA's MOD code forward onto Cameo's engine. Never push Cameo's engine back to CA's.**

That also explains the shape of the existing drift. CA's code targets an engine only 111 commits
past the 2024 base; Cameo's is 2 581 past it. So a vendored file usually differs because someone
**forward-ported** it — not because it went stale. A blind `cp -r` from CA would REVERT those
adaptations and break the build. Some files are the other way round (CA fixed a bug after we
copied it), which is exactly why this needs a per-file three-way merge and not a sync script.

## 2. Where we stand today

Run `python tools/audit/audit_ca_drift.py` (set `CA_ROOT` if the clone is not at
`../CAmod`). As of 2026-08-23:

| | files |
|---|--:|
| vendored here | 181 |
| upstream | 471 |
| identical to upstream | 41 |
| drifted | 108 (9 781 lines) |
| ours only | 32 |
| **upstream, never adopted** | **322** |

Of the 108 drifted, **30 differ by ≤6 lines** (a rename or a small fix — cheap to adopt) and
**31 by more than 50** (a different implementation — port by hand or leave alone). The largest
are the bot modules (`BaseBuilderBotModuleCA` 743 lines, `SquadManagerBotModuleCA` 559,
`UnitBuilderBotModuleCA` 542): Cameo's AI has been reworked and those must NOT be overwritten.

⚠ **Adoption is not the bottleneck — usage is.** Of **142** CA trait types already vendored,
only **56** appear in Cameo's rules. Pulling 322 more files would take the catalogue past 400
while leaving ~86 existing ones unused. Wiring traits into yaml is the scarce work, not copying
C#, and §5 puts it first for that reason.

## 3. What to adopt, in order

**Phase 1 — make it measurable and repeatable.** ✅ done: `audit_ca_drift.py` exists and is in
the suite. Nothing else can be judged until the drift is a number anyone can re-derive.

**Phase 2 — the 30 cheap files.** Diffs of ≤6 lines. For each: read the diff, take it if it is
an upstream fix, keep ours if it is our forward-port. Build after each batch of ten; boot-gate
the batch. This is where CA bugfixes we are missing actually live.

**Phase 3 — adopt by CAPABILITY, never by directory.** The 322 unadopted files are not a backlog
to burn down; they are a menu. Order by what Cameo wants:

| area | files | why it is interesting |
|---|--:|---|
| `Traits/SupportPowers` | 26 | the largest single block of mechanics Cameo does not have |
| `Widgets/Logic` | 24 | UI; low gameplay risk, and the observer work already proved the pattern |
| `Traits/Conditions` | 22 | small, self-contained, composes with everything |
| `Traits/Player` | 19 | economy/tech mechanics |
| `Traits/Render` | 17 | visual only; safe to trial |

For each capability: copy the file(s), fix the namespace, build, add a Cameo-only field to prove
the type resolves to OUR assembly, wire one concrete actor, boot-gate. That is the same
procedure the CA observer widgets went through, and it caught real problems each time
(`OpenRA.Player` shadowed by a nested namespace; `TooltipContainerWidget` implicit under CA's
`namespace OpenRA.Mods.Common.Widgets`).

**Phase 4 — the 31 heavily diverged files.** Case by case, and the default answer is NO. The AI
modules in particular carry Cameo-specific behaviour.

## 4. Staying current, permanently

Fully automatic adoption is not safe — the engines differ by 2 581 commits, so any CA change can
fail to compile here. What can be automated is **noticing**, and that is the part that decays:

1. **`audit_ca_drift` runs in the suite** (`tools/audit/run_all.sh`), so every full run reports
   the counts and every new upstream file appears in `docs/audit/latest/ca_drift.md`. It is
   INFORMATIONAL and never fails a build — adopting CA code is a maintainer decision.
2. **Record provenance.** When a file is adopted, note the CA commit it came from in its header,
   the way the ported observer widgets do. Without that, a future three-way merge has no base
   and every re-sync is guesswork.
3. **Refresh the clone before judging.** `git -C ../CAmod pull` first; a stale checkout reports
   stale drift. This has already burned one session — a `~/Downloads` copy of CA was old enough
   to be missing a whole tab, and produced a confident, wrong "CA doesn't have this".
4. **Compatibility is proven by BUILD + BOOT, not by reading.** `dotnet build -c Release
   -p:TargetPlatform=win-x64` then `launch-game.cmd` to the main menu. A file can compile and
   still not resolve to the intended assembly — `ObjectCreator.FindType` takes the FIRST match in
   `mod.yaml`'s `Assemblies:` order, which is **AS, CA, Cameo, Cnc, D2k, Common**. A Cameo type
   cannot shadow an AS one; a CA type cannot shadow an AS one either.

## 5. The honest bottleneck

The scarce resource is not C# — it is deciding which mechanics Cameo wants and wiring them into
yaml. 86 of the 142 vendored CA trait types are already unused. Before adopting another 322,
the higher-value pass is over what is already here: pick the unused traits worth having, wire
them, and let that tell us what kind of CA mechanics are actually wanted.

**Related:** `docs/design/PROJECTILE_AND_EFFECT_LAYER.md` (weapon layer),
`docs/LESSONS_LEARNED.md` (the engine pipeline and the assembly-order trap).


---

## 6. The other upstreams (2026-08-23)

> Maintainer: *"we not only want RV and CA but basically ALL the OpenRA mods included … Cameo is
> like the Frankenstein Monster of all the OpenRA mods."*

All four are cloned beside this repo under `~/Documents/GitHub/`, so every number here is
re-derivable. **`git -C <clone> pull` before trusting any of it** — a stale checkout produced a
confident, wrong "CA doesn't have this" earlier in the same programme.

| mod | clone | mod assembly | .cs | engine it pins | last push |
|---|---|---|--:|---|---|
| Combined Arms | `CAmod` | `OpenRA.Mods.CA` | 471 | `Inq8/OpenRA` `ca-engine/1.09` | 2026-07-30 **live** |
| Crystallized Nexus | `crystallized-nexus` | `.modsdk/OpenRA.Mods.CN` | 134 (+21 launcher) | `DoGyAUT/crystallized-nexus-engine` `cn-20260820` | 2026-08-19 **live** |
| Shattered Paradise | `Shattered-Paradise-SDK` | `OpenRA.Mods.Sp` | 50 | `MustaphaTR/OpenRA` `ab187a38c2` | 2025-09-27 dormant |
| Romanov's Vengeance | `Romanovs-Vengeance` | `OpenRA.Mods.RA2` | 32 | `MustaphaTR/OpenRA` `ac7864a16d` | 2025-07-26 dormant |

⭐ **RV and SP need no engine work at all, and that is measured, not assumed.** Both pin commits
of `MustaphaTR/OpenRA`, and **both are ANCESTORS of `cameo-engine`** — checked with
`git merge-base --is-ancestor`. Cameo's engine already contains everything their engines had; it
is 2 581 commits past the 2024 base while they are frozen at points behind it. So for RV and SP
the entire question is their MOD assemblies, and those are small: 32 and 50 files.

That is also the answer to *"follow CA without losing what we got from RV"* — there is nothing to
lose. The RV/SP inheritance lives in Cameo's ENGINE (`OpenRA.Mods.AS` above all), which CA does
not have and which adopting CA mod code does not touch.

**Two live upstreams, two frozen ones.** CA and CN are still moving, so they need the recurring
drift check of §4. RV and SP are dormant: mine them ONCE, record what was taken, and stop
watching them.

### Crystallized Nexus — the one that needs real research

CN is the newest and the most active, and unlike the others it **ships its own feature list**:
`crystallized-nexus/FEATURES.txt` enumerates what it adds over the stock TS mod, which makes it
the cheapest upstream to evaluate — read the list, pick, then look at the code. Its opening
section alone offers `VoxelDynamics` (spring-based impact/recoil/roll tilt on voxel units, with a
graphics toggle), drop-in `CNWithVoxelBody`/`Turret`/`Barrel`/`WalkerBody` replacements,
`AlphaGradientPalette`, `CharredPalette`, `DamageSmoke`, `PeriodicSpriteEffect` and a full-screen
`AtmosphericGradingRenderer`.

⚠ **CN pins its OWN engine fork** (`DoGyAUT/crystallized-nexus-engine`), and `FEATURES.txt`
explicitly refers to "ENGINE PATCHES". So some CN features are NOT mod-side and cannot simply be
copied into `OpenRA.Mods.Cameo`. **This is the one unmeasured thing in this document** — the next
step is to establish whether CN's engine shares an ancestor with `cameo-engine`, exactly as was
done for RV and SP:

```sh
cd ~/Documents/GitHub/cameo-engine
git remote add cn https://github.com/DoGyAUT/crystallized-nexus-engine.git
git fetch cn --no-tags
git merge-base cameo-engine cn/<branch>        # is there a shared base at all?
git rev-list --left-right --count cameo-engine...cn/<branch>
```

Until that is run, treat every CN feature as *possibly* engine-side. A mod-side port is a copy; an
engine-side one goes through the `cameo-engine` pipeline in `docs/LESSONS_LEARNED.md`.

### Order of work

1. **RV + SP first** — 82 files total, frozen, and no engine risk. Smallest job, and it closes
   two upstreams permanently.
2. **CN's mod-side features** — after the engine-lineage measurement above says which they are.
3. **CA by capability** — §3, the largest and slowest, and the one where usage rather than
   adoption is the bottleneck.

⚠ The same trap applies to all four: `ObjectCreator.FindType` takes the FIRST assembly in
`mod.yaml`'s `Assemblies:` order — **AS, CA, Cameo, Cnc, D2k, Common**. A ported type placed in
`OpenRA.Mods.Cameo` cannot shadow one that already exists in AS or CA. Prove a port resolves to
the assembly you intended by giving it a field the other one lacks and booting with that field
set; `--docs` lists both types and proves nothing.

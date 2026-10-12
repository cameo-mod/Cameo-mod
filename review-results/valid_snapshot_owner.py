from pathlib import Path
r=Path(r'C:/cameo-wt/codex-rereview-econ-a')
p=r/'review-results/probe/Program.cs'
s=p.read_text().replace('blocker.AddTrait(new DoesNotBlock(null,new DoesNotBlockInfo()));', 'var enemy=Blank<Player>();Set(blocker,"<Owner>k__BackingField",enemy);blocker.AddTrait(new DoesNotBlock(null,new DoesNotBlockInfo()));').replace('Set(frozen,"<Owner>k__BackingField",player)', 'Set(frozen,"<Owner>k__BackingField",enemy)')
p.write_text(s)

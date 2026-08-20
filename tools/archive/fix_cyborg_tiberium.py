import re

fpath = 'mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/infantry.yaml'
with open(fpath, encoding='utf-8') as f:
    lines = f.readlines()

cyborg_actors = {'cabal_cyborgcommandov2', 'cabal_cyborgcommando', 'cabal_cyborginfantry', 'cabal_dissolver', 'cabal_engineer'}

current_actor = None
result = []
modified = False

for i, line in enumerate(lines):
    m = re.match(r'^(\w+):', line)
    if m and not line.startswith('\t'):
        current_actor = m.group(1)

    if line.strip() == '-DamagedByTerrain:' and current_actor and current_actor not in cyborg_actors:
        result.append('\t-DamagedByTerrain:\n')
        result.append('\tDamagedByTerrain@TiberiumHeal:\n')
        result.append('\t\tTerrain: Tiberium, BlueTiberium, RedTiberium, GoldTiberium\n')
        result.append('\t\tDamage: -20\n')
        result.append('\t\tDamageInterval: 1\n')
        modified = True
        print(f'Replaced -DamagedByTerrain: at line {i+1} in {current_actor}')
    else:
        result.append(line)

with open(fpath, 'w', encoding='utf-8') as f:
    f.writelines(result)

print('Done' if modified else 'No changes')

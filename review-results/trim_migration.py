from pathlib import Path
for n in ['mods/cameo/ContentPacks/RedAlert/Japan/yaml/vehicles.yaml','mods/cameo/ContentPacks/RedAlert2Mod/Naxis/yaml/naval.yaml','mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml']:
 p=Path(n);p.write_text(p.read_text(encoding='utf-8').rstrip()+'\n',encoding='utf-8')

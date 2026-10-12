from pathlib import Path
import zipfile,re,io
root=Path('mods/cameo');changed=[]
for p in root.rglob('*.oramap'):
 if not zipfile.is_zipfile(p):continue
 with zipfile.ZipFile(p) as a:
  entries=[];any_change=False;count=0
  for info in a.infolist():
   data=a.read(info.filename)
   if info.filename.lower().endswith(('.yaml','.yml')):
    text=data.decode('utf-8-sig');lines=text.splitlines();out=[]
    for i,line in enumerate(lines):
     out.append(line);m=re.match(r'^(\s+)AutoTarget(?:@[^:]+)?:\s*(?:#.*)?$',line)
     if m:
      indent=m[1];j=i+1;present=False
      while j<len(lines) and (not lines[j].strip() or len(lines[j])-len(lines[j].lstrip())>len(indent)):
       present |= lines[j].strip().startswith('DynamicWeaponPriority:');j+=1
      if not present:out.append(indent+'\tDynamicWeaponPriority: true');count+=1;any_change=True
    if count:data=('\n'.join(out)+'\n').encode('utf-8')
   entries.append((info,data))
  comment=a.comment
 if any_change:
  b=io.BytesIO()
  with zipfile.ZipFile(b,'w') as a:
   a.comment=comment
   for info,data in entries:a.writestr(info,data)
  p.write_bytes(b.getvalue());changed.append((str(p),count))
print('Packed AutoTarget migration:',changed)

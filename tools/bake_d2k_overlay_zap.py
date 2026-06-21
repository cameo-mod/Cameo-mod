from PIL import Image
from PIL.PngImagePlugin import PngInfo
import numpy as np, sys

NATIVE=r"C:\temp\zap_native.png"
OUT=r"C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo\bits\d2k\power_atreides_zaps.png"
N=10; SCALE=1.5
WRITE="--write" in sys.argv

im=Image.open(NATIVE).convert("RGBA"); W,H=im.size; fw=W//N; fh=H
arr=np.asarray(im).astype(np.int32)
frames=[arr[:, i*fw:(i+1)*fw,:].copy() for i in range(N)]

# GLOW MASK: keep teal/cyan energy only. Drop tan shell (R-dominant), brown (dark),
# green player-color slots (low blue). Teal = blue prominent + bright + not red-led.
def glowmask(f):
    R,G,B,A=f[:,:,0],f[:,:,1],f[:,:,2],f[:,:,3]
    return (A>0)&(B>R+15)&(G>R)&(B>80)

trimmed=[]; kept=[]
for f in frames:
    m=glowmask(f)
    g=f.copy(); g[~m,3]=0
    trimmed.append(g.astype(np.uint8)); kept.append(int(m.sum()))
print("kept glow px per frame:", kept)

# uniform 1.5x nearest (same cell/offset as the tuned 7,-2 full-overlay version)
sW,sH=round(fw*SCALE),round(fh*SCALE)
scaled=[np.asarray(Image.fromarray(t,"RGBA").resize((sW,sH),Image.NEAREST)) for t in trimmed]

def up(a,k): return np.kron(a,np.ones((k,k,1),dtype=a.dtype))
def over(s,bg):
    base=np.full((sH,sW,3),bg,np.float64); a=s[:,:,3:4]/255.0
    return (s[:,:,:3]*a+base*(1-a)).astype(np.uint8)
def strip(bg):
    ps=[up(over(scaled[i],bg),4) for i in range(5)]  # 5 active frames
    H2,W2=ps[0].shape[:2]; c=np.full((H2,5*(W2+2),3),bg,np.uint8)
    for j,p in enumerate(ps): c[:,j*(W2+2):j*(W2+2)+W2]=p
    return c
Image.fromarray(np.vstack([strip(150),strip(60)]),"RGB").save(r"C:\temp\glow_preview.png")

if WRITE:
    sheet=np.zeros((sH,sW*N,4),dtype=np.uint8)
    for i,s in enumerate(scaled): sheet[:,i*sW:(i+1)*sW]=s
    meta=PngInfo(); meta.add_text("FrameSize",f"{sW},{sH}"); meta.add_text("FrameAmount",str(N))
    Image.fromarray(sheet,"RGBA").save(OUT,pnginfo=meta)
    print(f"WROTE {OUT} FrameSize={sW},{sH}")

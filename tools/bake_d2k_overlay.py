from PIL import Image
from PIL.PngImagePlugin import PngInfo
import numpy as np
import argparse
import warnings

parser = argparse.ArgumentParser(description="Trim and uniformly scale a D2k building overlay.")
parser.add_argument("threshold", nargs="?", type=int, default=30, help="Static-trim threshold.")
parser.add_argument("--input", default=r"C:\temp\dish_native.png")
parser.add_argument(
    "--output",
    default=r"C:\Users\Blackrobe\repo\Cameo-mod\mods\cameo\bits\d2k\ixian_outpost_atreides_dish.png")
parser.add_argument("--preview", default=r"C:\temp\trim_preview.png")
parser.add_argument("--frames", type=int, default=30)
parser.add_argument("--scale", type=float, default=1.5)
parser.add_argument("--offset-x", type=float, default=10)
parser.add_argument("--offset-y", type=float, default=-3)
parser.add_argument("--write", action="store_true")
args = parser.parse_args()

NATIVE = args.input
OUT = args.output
N = args.frames
SCALE = args.scale
T = args.threshold
WRITE = args.write

im = Image.open(NATIVE).convert("RGBA")
W,H = im.size; fw=W//N; fh=H
arr = np.asarray(im).astype(np.int32)
frames = [arr[:, i*fw:(i+1)*fw, :].copy() for i in range(N)]

# --- find the STATIC structure (the bracket/cradle) for alignment ---
alpha = np.stack([(f[:,:,3]>0).astype(np.float64) for f in frames])
lum   = np.stack([f[:,:,:3].mean(2) for f in frames])
present = alpha.mean(0)
std = (lum*alpha).std(0)
static = (present > 0.8) & (std < 30)
ref = np.median(lum, axis=0)
def cost(l,dx,dy):
    s=np.roll(np.roll(l,dy,0),dx,1); d=(s-ref)[static]; return (d*d).mean() if d.size else 1e18

# --- bracket-lock: nudge each frame by integer px ---
PAD=3; shifts=[]
for i in range(N):
    best=None
    for dy in range(-PAD,PAD+1):
        for dx in range(-PAD,PAD+1):
            c=cost(lum[i],dx,dy)
            if best is None or c<best[0]: best=(c,dx,dy)
    shifts.append((best[1],best[2]))

cW,cH=fw+2*PAD,fh+2*PAD
aligned=[]
for i,(dx,dy) in enumerate(shifts):
    cv=np.zeros((cH,cW,4),dtype=np.int32)
    y0,x0=PAD+dy,PAD+dx
    yi0,xi0=max(0,y0),max(0,x0); yi1,xi1=min(cH,y0+fh),min(cW,x0+fw)
    cv[yi0:yi1,xi0:xi1]=frames[i][yi0-y0:yi1-y0,xi0-x0:xi1-x0]
    aligned.append(cv)

# --- TRIM STATIC by SILHOUETTE: drop only the static background that is connected to the
#     outer border; keep everything inside the dish outline SOLID (interior holes filled). ---
A=np.stack(aligned).astype(np.float64)              # N,cH,cW,4
op=A[...,3]>0
rgb=A[...,:3]
masked=np.where(op[...,None],rgb,np.nan)
with warnings.catch_warnings():
    warnings.simplefilter("ignore", category=RuntimeWarning)
    med=np.nan_to_num(np.nanmedian(masked,axis=0),nan=0.0)   # cH,cW,3 = static background
diff=np.abs(rgb-med[None]).max(axis=-1)             # N,cH,cW  (how different from static bg)

def dil(m):  # 3x3 binary dilation
    o=m.copy()
    o[1:,:]|=m[:-1,:]; o[:-1,:]|=m[1:,:]; o[:,1:]|=m[:,:-1]; o[:,:-1]|=m[:,1:]
    return o

def outside_of(free):  # flood from the cell border through `free` pixels
    out=np.zeros_like(free)
    out[0,:]|=free[0,:]; out[-1,:]|=free[-1,:]; out[:,0]|=free[:,0]; out[:,-1]|=free[:,-1]
    while True:
        nxt=(out|dil(out))&free
        if nxt.sum()==out.sum(): return nxt
        out=nxt

trimmed=[]; kept_counts=[]
for i in range(N):
    M=op[i] & (diff[i]>T)        # confident dish pixels
    M=dil(M)                     # grow silhouette by 1px so dish edges aren't eaten
    outside=outside_of(~M)       # static bg reachable from the border (the cradle/box)
    drop=outside                 # everything NOT outside = dish silhouette + enclosed holes
    f=aligned[i].copy()
    f[drop,3]=0                  # keep original colour everywhere inside the outline (solid)
    trimmed.append(f.astype(np.uint8))
    kept_counts.append(int((f[...,3]>0).sum()))
print(f"T={T}: kept px per frame min/med/max = {min(kept_counts)}/{int(np.median(kept_counts))}/{max(kept_counts)}  (cell {cW*cH})")

# --- uniform upscale ---
sW,sH=round(cW*SCALE),round(cH*SCALE)
scaled=[np.asarray(Image.fromarray(t,"RGBA").resize((sW,sH),Image.NEAREST)) for t in trimmed]

# preview: composite trimmed frames over neutral gray, montage 10-wide
def up(a,k): return np.kron(a,np.ones((k,k,1),dtype=a.dtype))
def over_gray(s):
    g=np.full((sH,sW,3),110,np.float64); a=s[:,:,3:4]/255.0
    return (s[:,:,:3]*a+g*(1-a)).astype(np.uint8)
prev=[up(over_gray(s),5) for s in scaled]
H2,W2=prev[0].shape[:2]; cols=10; rows=(N+cols-1)//cols
canvas=np.full((rows*(H2+2),cols*(W2+2),3),25,np.uint8)
for i,p in enumerate(prev):
    r=i//cols;c=i%cols; canvas[r*(H2+2):r*(H2+2)+H2,c*(W2+2):c*(W2+2)+W2]=p
Image.fromarray(canvas,"RGB").save(args.preview)

if WRITE:
    sheet=np.zeros((sH,sW*N,4),dtype=np.uint8)
    for i,s in enumerate(scaled): sheet[:,i*sW:(i+1)*sW]=s
    meta=PngInfo(); meta.add_text("FrameSize",f"{sW},{sH}"); meta.add_text("FrameAmount",str(N))
    Image.fromarray(sheet,"RGBA").save(OUT,pnginfo=meta)
    print(
        f"WROTE {OUT}  FrameSize={sW},{sH}  "
        f"Offset={round(args.offset_x*SCALE)},{round(args.offset_y*SCALE)}")

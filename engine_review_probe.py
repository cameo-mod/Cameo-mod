"""Offline exact-source C# differential harness; no game/engine checkout mutation."""
import pathlib, subprocess
engine = pathlib.Path(r'C:\cameo-wt\sol-mcv-deploy-engine')
out = pathlib.Path(__file__).parent / 'engine-review-results'
out.mkdir(exist_ok=True)
def blob(ref, path):
    return subprocess.check_output(['git','show',ref+':'+path],cwd=engine,text=True)
def segment(s, start, end): return s[s.index(start):s.index(end)]
old='4f69df005f'; new='ad061ebf79029da4dcd3a841a9b2540e39cbf968'
code='''using System;
record struct int2(int X, int Y) {
 public int LengthSquared => X*X+Y*Y;
 public int Length => Exts.ISqrt(LengthSquared);
 public static int Dot(int2 a,int2 b)=>a.X*b.X+a.Y*b.Y;
 public static int2 operator -(int2 a,int2 b)=>new(a.X-b.X,a.Y-b.Y);
 public static int2 operator +(int2 a,int2 b)=>new(a.X+b.X,a.Y+b.Y);
}
record struct WVec(int X,int Y,int Z);
record struct WDist(int Length);
static class Exts {
'''
exts=blob(new,'OpenRA.Game/Exts.cs')
code+=segment(exts,'\t\tpublic enum ISqrtRoundMode','\t\tpublic static int MultiplyBySqrtTwo')+'}\n'
for ref,label in [(old,'Old'),(new,'New')]:
    poly=blob(ref,'OpenRA.Mods.Common/HitShapes/Polygon.cs')
    begin='\t\tstatic long LengthSquaredLong' if label=='New' else '\t\tstatic int DistanceSquaredFromLineSegment'
    body=segment(poly,begin,'\t\tpublic WDist DistanceFromEdge')
    body=body.replace('static int DistanceSquaredFromLineSegment','public static int DistanceSquaredFromLineSegment').replace('static long DistanceSquaredFromLineSegment','public static long DistanceSquaredFromLineSegment')
    code+='static class '+label+'Polygon {\n'+body+'}\n'
    cap=blob(ref,'OpenRA.Mods.Common/HitShapes/Capsule.cs')
    body=segment(cap,'\t\tpublic WDist DistanceFromEdge(in WVec','\t\tpublic WDist DistanceFromEdge(WPos')
    helper=segment(cap,'\t\tstatic long LengthSquaredLong','\t\tpublic void Initialize') if label=='New' else ''
    code+='class '+label+'Capsule { int2 PointA,PointB,ab; WDist Radius; '+('long' if label=='New' else 'int')+' abLenSq;\n'
    code+='public '+label+'Capsule(int2 a,int2 b) { PointA=a;PointB=b;ab=b-a;Radius=new(426);abLenSq='+('LengthSquaredLong(ab)' if label=='New' else 'ab.LengthSquared')+'/1024; if(abLenSq==0)throw new ArgumentException(); }\n'+helper+body+'}\n'
code+='''class Program {
 static void Main() {
 var rng=new Random(1337); int polygons=0,capsules=0;
 for(int i=0;i<100000;i++) {
  int2 a=new(rng.Next(-10000,10001),rng.Next(-10000,10001)),b=new(rng.Next(-10000,10001),rng.Next(-10000,10001)),c=new(rng.Next(-15000,15001),rng.Next(-15000,15001));
  try {int ab2=(b-a).LengthSquared;if(ab2==0)continue;var prev=OldPolygon.DistanceSquaredFromLineSegment(c,a,b,ab2);var next=NewPolygon.DistanceSquaredFromLineSegment(c,a,b,ab2);if(prev!=next)throw new Exception("polygon difference");if(Exts.ISqrt(prev)!=Exts.ISqrt(next))throw new Exception("sqrt difference");polygons++;}catch(OverflowException){}
  try {var prev=new OldCapsule(a,b).DistanceFromEdge(new(c.X,c.Y,0));var next=new NewCapsule(a,b).DistanceFromEdge(new(c.X,c.Y,0));if(prev!=next)throw new Exception("capsule difference");capsules++;}catch(OverflowException){}catch(ArgumentException){}
 }
 Console.WriteLine($"Fully nonoverflowing prior-domain differential: polygon={polygons}, capsule={capsules}, mismatches=0");
 var far=new WVec(50000,0,0); var aa=new int2(0,0);var bb=new int2(1024,0);
 Console.WriteLine($"Extended-domain capsule(50000,0)={new NewCapsule(aa,bb).DistanceFromEdge(far).Length}; polygon segment={Exts.ISqrt(NewPolygon.DistanceSquaredFromLineSegment(new(50000,0),aa,bb,1048576))}");
 }
}
'''
(out/'Program.cs').write_text(code)
(out/'Probe.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup></Project>')
subprocess.run(['dotnet','run','--project',str(out/'Probe.csproj'),'-c','Release'],check=True)

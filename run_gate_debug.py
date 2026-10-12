import pathlib
import sys
sys.path.insert(0, 'tools/tests')
import ai_b1b7_gate as g

mod, eng = g.load_config()
print("mod:", mod, "engine:", eng)
support = g.support_directory(eng)
args = [
    f"Game.Mod={mod}",
    "Sound.Device=none",
    "Engine.EngineDir=..",
    f"Engine.ModSearchPaths={g.REPO_ROOT / 'mods'},{eng / 'mods'}",
    f"Engine.SupportDir={support}",
    "Launch.Map=ai_b1b7_gate_20261009",
    "Launch.Benchmark=ai-b1b7-gate-",
    "Graphics.VSync=False",
    "Graphics.Mode=Windowed",
    "Graphics.WindowedSize=640,480",
]
code, out = g.run_openra(eng / "bin" / "OpenRA.exe", args, eng)
print("EXIT:", code)
pathlib.Path("gate_debug_output.txt").write_text(out, encoding="utf-8", errors="replace")
print("---- OUTPUT TAIL ----")
print(out[-6000:])

import subprocess, sys
d = subprocess.run(["git", "diff", "origin/master", "devin/tier1-fitter", "--",
                    "docs/design/AI_ARCHITECTURE.md", "docs/design/TIER1_FITTER_SPEC.md", "tools/"],
                   capture_output=True, text=True).stdout
print("NON-DEVLOG DIFF:", "IDENTICAL" if not d.strip() else "\n" + d[:1500])
m = subprocess.run(["git", "merge-tree", "--write-tree", "origin/master", "devin/tier1-fitter"],
                   capture_output=True, text=True)
print("MERGE-TREE rc:", m.returncode)
print(m.stdout[:300] if m.stdout else "", m.stderr[:300] if m.stderr else "")

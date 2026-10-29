from pathlib import Path
import re

res = Path(__file__).resolve().parent
res = res.parent
FM = re.compile(r"\A---\n(.*?)\n---\n?", re.S)
BAK = res / "result_backup_pre_s6"

def read_docs(root):
    docs = {}
    for path in sorted(root.rglob("*.md")):
        if re.match(r"^\d+_", path.name):
            docs[path.relative_to(root).as_posix()] = True
    return docs

b = read_docs(BAK)
a = read_docs(res)
print("BAK docs:", len(b))
print("RES docs:", len(a))
only_bak = set(b) - set(a)
only_res = set(a) - set(b)
print("Only in BAK:", sorted(only_bak)[:10], "... total:", len(only_bak))
print("Only in RES:", sorted(only_res)[:10], "... total:", len(only_res))

# Check if build/ is included
print("\nBAK build/ exists:", (BAK / "build").exists())
print("BAK has VERSION.md:", (BAK / "VERSION.md").exists())
res_topics = [f for f in res.rglob("*.md") if re.match(r"^\d+_", f.name) and "build" not in f.relative_to(res).parts]
bak_topics = [f for f in BAK.rglob("*.md") if re.match(r"^\d+_", f.name) and "build" not in f.relative_to(BAK).parts]
print("RES topics (excluding build):", len(res_topics))
print("BAK topics (excluding build):", len(bak_topics))

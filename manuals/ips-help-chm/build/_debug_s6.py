from pathlib import Path
import re, json

BASE = Path(__file__).resolve().parent  # build/
ROOT = BASE.parent                      # ips-help-chm/
BAK = ROOT / "result_backup_pre_s6"

FM = re.compile(r"\A---\n(.*?)\n---\n?", re.S)

def read_docs(root):
    docs = {}
    for path in sorted(root.rglob("*.md")):
        if re.match(r"^\d+_", path.name):
            docs[path.relative_to(root).as_posix()] = True
    return docs

b = read_docs(BAK)
a = read_docs(ROOT)
print("BAK docs:", len(b), "RES docs:", len(a))
only_bak = set(b) - set(a)
only_res = set(a) - set(b)
print("Only in BAK:", sorted(only_bak)[:5], "total:", len(only_bak))
print("Only in RES:", sorted(only_res)[:5], "total:", len(only_res))

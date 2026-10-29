from pathlib import Path
res = Path(__file__).resolve().parent
res = res.parent
bak_pre = res / "result_backup_pre_s6"
if bak_pre.exists():
    md_files = sorted([f.relative_to(res) for f in bak_pre.rglob("*.md")])
    print("result_backup_pre_s6 md files:", len(md_files))
    for f in md_files[:10]:
        print("  ", f)
else:
    print("result_backup_pre_s6 does not exist")
    rb2 = res / "result_backup2"
    if rb2.exists():
        print("result_backup2 exists, files:", sum(1 for _ in rb2.rglob('*.md')))
    else:
        print("result_backup2 does not exist")

# Check what read_docs finds
import re
RES = res
docs = {}
for path in sorted(RES.rglob("*.md")):
    if re.match(r"^\d+_", path.name):
        docs[path.relative_to(RES).as_posix()] = True
print("read_docs(RES) topic files:", len(docs))
# Also check result_backup3
bak3 = res / "result_backup3"
if bak3.exists():
    docs_bak = {}
    for path in sorted(bak3.rglob("*.md")):
        if re.match(r"^\d+_", path.name):
            docs_bak[path.relative_to(bak3).as_posix()] = True
    print("read_docs(backup3) topic files:", len(docs_bak))

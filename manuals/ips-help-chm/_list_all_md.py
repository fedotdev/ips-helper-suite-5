from pathlib import Path
import re
res = Path(__file__).resolve().parent
res = res.parent  # ips-help-chm/

all_md = list(res.rglob("*.md"))
print("Total MD files:", len(all_md))
topic = [f for f in all_md if re.match(r"^\d+_", f.name)]
print("Topic files:", len(topic))
topic_nb = [f for f in all_md if re.match(r"^\d+_", f.name) and not any(p.startswith("result_backup") for p in f.relative_to(res).parts)]
print("Topic files (non-backup):", len(topic_nb))

dirs_with_md = sorted(set(f.parent for f in all_md), key=lambda x: len(str(x)))
print("Dirs with MD files:", len(dirs_with_md))
for d in dirs_with_md:
    cnt = sum(1 for f in d.rglob("*.md") if f.is_file())
    print(f"  {str(d.relative_to(res))}: {cnt} md files")

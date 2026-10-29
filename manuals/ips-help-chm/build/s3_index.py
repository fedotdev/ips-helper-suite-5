import json, re, sys
from pathlib import Path
from collections import Counter, defaultdict
sys.stdout.reconfigure(encoding="utf-8")

BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES = ROOT
EXCL = {"INDEX.md", "REPORT.md"}
docs = [f for f in sorted(RES.rglob("*.md")) if f.name not in EXCL and not f.name.startswith("VERIFY") and not any(p in {"result_backup", "result_backup2", "result_backup3", "result_backup_pre_s6"} for p in f.relative_to(RES).parts)]

FM = re.compile(r"^---\n(.*?)\n---\n", re.S)
def fm(f):
    m = FM.match(f.read_text(encoding="utf-8"))
    kv = {}
    for line in m.group(1).splitlines():
        k, _, v = line.partition(":")
        kv[k.strip()] = v.strip()
    kv["title"] = kv["title"].strip('"')
    kv["section"] = kv["section"].strip('"')
    kv["path"] = kv["path"].strip('"')
    kv["topic_id"] = kv["topic_id"].strip('"')
    kv["parent_id"] = None if kv.get("parent_id") == "null" else kv.get("parent_id", "").strip('"')
    return kv

meta = {f: fm(f) for f in docs}
# canonical file per topic (split parts -p1/-p2 share a topic)
canon = {}
for f in docs:
    tid = meta[f]["topic_id"]
    if tid not in canon or "-p" not in f.name:
        canon[tid] = f

rep = (RES / "REPORT.md").read_text(encoding="utf-8")
stubs = set(x.strip() for x in re.search(r"Пустое/почти пустое тело: 78: (.+?)\n", rep).group(1).split(","))
stubs.discard("")
print("stub ids from REPORT:", len(stubs))

sections = defaultdict(list)
for tid, f in canon.items():
    sections[meta[f]["section"]].append(tid)

stats = Counter()
for sec, tids in sorted(sections.items()):
    ids = set(tids)
    title_count = Counter(meta[canon[t]]["title"] for t in tids)
    children = defaultdict(list)
    roots = []
    for t in tids:
        p = meta[canon[t]]["parent_id"]
        if p and p in ids and p != t:
            children[p].append(t)
        else:
            roots.append(t)
    for k in children:
        children[k].sort(key=lambda t: meta[canon[t]]["title"].casefold())
    roots.sort(key=lambda t: meta[canon[t]]["title"].casefold())

    lines = ["# " + sec, ""]
    seen = 0
    def walk(t, lvl):
        global seen
        m = meta[canon[t]]
        has_dup = title_count[m["title"]] > 1
        suffix = " (только оглавление)" if t in stubs else ""
        extra = (" — " + m["path"]) if has_dup else ""
        lines.append("  " * lvl + f"- [{m['title']}]({canon[t].name})" + suffix + extra)
        seen += 1
        for c in children.get(t, []):
            walk(c, lvl + 1)
    for r in roots:
        walk(r, 0)
    lines.append("")
    (RES / (canon[next(iter(tids))].relative_to(RES)).parent / "INDEX.md").write_text("\n".join(lines), encoding="utf-8")
    nested = sum(1 for l in lines if re.match(r"^ {2,}- \[", l))
    stats["sections"] += 1
    stats["entries"] += seen
    stats["nested"] += nested
    stats["stubs"] += sum(1 for t in tids if t in stubs)
    print(f"{sec}: topics={len(tids)} listed={seen} nested={nested} stubs={sum(1 for t in tids if t in stubs)} roots={len(roots)}")

root = ["# Справка IPS", ""]
for sec, tids in sorted(sections.items()):
    d = canon[next(iter(tids))].relative_to(RES).parent.name
    root.append(f"- [{sec}]({d}/INDEX.md) — {len(tids)} тем")
root.append("")
(RES / "INDEX.md").write_text("\n".join(root), encoding="utf-8")
print("root sections:", len(sections), "total topics:", sum(len(v) for v in sections.values()))
print("stats:", dict(stats))
(BASE / "_s3_stats.json").write_text(json.dumps({"sections": {k: len(v) for k, v in sections.items()},
    "stats": dict(stats), "stub_ids": len(stubs)}, ensure_ascii=False, indent=1), encoding="utf-8")

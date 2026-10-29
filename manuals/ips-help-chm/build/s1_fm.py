import json, re, sys
from pathlib import Path
from collections import Counter
sys.stdout.reconfigure(encoding="utf-8")

SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES = ROOT

STOP = {"для", "при", "через", "или", "без", "как", "над", "все"}
BANNER = re.compile(r"^!\[\]\(\.\./images/[^)]*\)\s*\|\s*(.+)$")
LINK = re.compile(r"\[([^\]]*)\]\((\d+)\.md(?:\s+\"[^\"]*\")?\)")

def segs(tail):
    out, buf, d = [], "", 0
    for ch in tail:
        if ch == "[": d += 1
        if ch == "]": d -= 1
        if ch == ":" and d == 0: out.append(buf.strip()); buf = ""
        else: buf += ch
    if buf.strip(): out.append(buf.strip())
    return out

meta = {}
for p in sorted(DATA.glob("*.md")):
    tail = None
    for line in p.read_text(encoding="utf-8", errors="replace").splitlines()[:6]:
        m = BANNER.match(line.strip())
        if m: tail = m.group(1).strip(); break
    names, ids = [], []
    for s in segs(tail or ""):
        lm = LINK.fullmatch(s)
        if lm: names.append(lm.group(1).strip()); ids.append(lm.group(2))
        else: names.append(LINK.sub(lambda m: m.group(1), s).strip()); ids.append(None)
    if not names: continue
    parent = None
    if len(names) >= 3:
        parent = ids[-2] or next((x for x in reversed(ids[:-1]) if x), None)
    meta[p.stem] = {"path": " > ".join(names), "parent_id": parent, "depth": len(names)}

ver = re.search(r"version:\s*([A-Za-z0-9._-]+)", (ROOT / "VERSION.md").read_text(encoding="utf-8")).group(1)
print("version from VERSION.md:", ver)

files = [f for f in RES.rglob("*.md") if f.name not in ("INDEX.md", "REPORT.md") and not f.name.startswith("VERIFY") and not any(part.startswith("result_backup") for part in f.relative_to(RES).parts)]
FM = re.compile(r"^---\n(.*?)\n---\n?", re.S)
stats = Counter()
no_meta = []

for f in files:
    t = f.read_text(encoding="utf-8", errors="replace")
    m = FM.match(t)
    if not m: stats["no_frontmatter"] += 1; continue
    kv, order = {}, []
    for line in m.group(1).splitlines():
        k, _, v = line.partition(":")
        kv[k.strip()] = v.strip(); order.append(k.strip())
    tid = kv.get("topic_id", "").strip('"')
    info = meta.get(tid)
    if not info:
        no_meta.append(tid)
        info = {"path": kv.get("section", "").strip('"') + " > " + kv.get("title", "").strip('"'), "parent_id": None, "depth": 2}
    # keywords
    try:
        kws = json.loads(kv.get("keywords", "[]"))
    except Exception:
        kws = []
    before = len(kws)
    kws = [k for k in kws if k.strip() and k.strip().lower() not in STOP]
    stats["kw_removed"] += before - len(kws)
    kws = list(dict.fromkeys(kws))
    body = t[m.end():]
    head = ["---",
            "title: " + kv.get("title", '""'),
            "source: " + kv.get("source", '""'),
            "version: " + json.dumps(ver, ensure_ascii=False),
            "topic_id: " + kv.get("topic_id", '""'),
            "section: " + kv.get("section", '""'),
            "path: " + json.dumps(info["path"], ensure_ascii=False),
            "parent_id: " + ("null" if not info["parent_id"] else json.dumps(info["parent_id"], ensure_ascii=False)),
            "keywords: " + json.dumps(kws, ensure_ascii=False),
            "---", ""]
    f.write_text("\n".join(head) + body, encoding="utf-8")
    stats["files"] += 1
    stats["with_path"] += 1
    stats["parent_null" if not info["parent_id"] else "parent_id"] += 1
    stats["depth" + str(info["depth"])] += 1

print("stats:", dict(stats))
print("topics without breadcrumb:", Counter(no_meta).most_common(10), "count:", len(no_meta))
(ROOT / "_s1_stats.json").write_text(json.dumps({"stats": dict(stats), "version": ver, "no_meta": no_meta}, ensure_ascii=False, indent=1), encoding="utf-8")

import json, re, sys
from pathlib import Path
from collections import Counter, defaultdict
sys.stdout.reconfigure(encoding="utf-8")

BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES, BAK = ROOT, ROOT / "result_backup"
STOP = {"для", "при", "через", "или", "без", "как", "над", "все"}
FM = re.compile(r"^---\n(.*?)\n---\n", re.S)
SEP = re.compile(r"^\s*\|(?:\s*:?-{3,}:?\s*\|)+\s*$")

def read(root):
    out = []
    for f in sorted(root.rglob("*.md")):
        if f.name in ("INDEX.md", "REPORT.md") or f.name.startswith("VERIFY"):
            continue
        if any(p in {"result_backup", "result_backup2", "result_backup3", "result_backup_pre_s6"} for p in f.relative_to(root).parts):
            continue
        t = f.read_text(encoding="utf-8")
        m = FM.match(t)
        kv = {}
        if m:
            for line in m.group(1).splitlines():
                k, _, v = line.partition(":")
                kv[k.strip()] = v.strip()
        body = t[m.end():] if m else t
        body = re.sub(r"^# .*$\n?", "", body, count=1, flags=re.M).strip()
        out.append((f, kv, body, t))
    return out

def unq(v):
    return v.strip('"') if v else v

before, after = read(BAK), read(RES)

# ---------- hhc ----------
raw = Path(r"D:\parsing\html\IPSHelp.hhc").read_bytes().decode("cp1251", errors="replace")
pat = re.compile(r"<(?P<ul>/UL|UL)\b[^>]*>|<OBJECT\b(?P<body>.*?)</OBJECT>", re.S | re.I)
stack, last, hhc = [], None, {}
for m in pat.finditer(raw):
    if m.group("ul"):
        if m.group("ul").lower() == "ul":
            stack.append(last)
        elif stack:
            stack.pop()
        continue
    body = m.group("body")
    nm = re.search(r'name="Name"\s+value="([^"]*)"', body, re.I)
    lc = re.search(r'name="Local"\s+value="([^"]*)"', body, re.I)
    tid = re.search(r"Manual/(\d+)\.htm", lc.group(1), re.I).group(1) if lc else None
    par = re.search(r"Manual/(\d+)\.htm", stack[-1]["local"], re.I).group(1) if stack and stack[-1] and stack[-1]["local"] else None
    obj = {"name": re.sub(r"\s+", " ", nm.group(1) if nm else "").strip(), "parent": par}
    last = {**obj, "local": lc.group(1) if lc else None}
    if tid:
        hhc[tid] = obj

canon = {}
for f, kv, body, t in after:
    tid = unq(kv.get("topic_id"))
    if tid not in canon or "-p" not in f.name:
        canon[tid] = (f, kv, body)

# ---------- comparisons ----------
c = Counter()
mismatch, path_bad, samples = [], [], []
for tid, (f, kv, body) in canon.items():
    c["topics"] += 1
    par = None if kv.get("parent_id") == "null" else unq(kv.get("parent_id"))
    c["null" if par is None else "parent"] += 1
    if tid not in hhc:
        c["not_in_hhc"] += 1
        continue
    c["in_hhc"] += 1
    if par != hhc[tid]["parent"]:
        mismatch.append((f.name, unq(kv.get("title")), par, hhc[tid]["parent"]))
    if unq(kv.get("title")) != hhc[tid]["name"]:
        c["title_diff_hhc"] += 1
    names, cur = [], par
    while cur and cur in hhc:
        names.append(hhc[cur]["name"])
        cur = hhc[cur]["parent"]
    want = [unq(kv.get("section"))] + names[::-1] + [unq(kv.get("title"))]
    got = [x.strip() for x in unq(kv.get("path")).split(" > ")]
    if got != want:
        path_bad.append((f.name, " > ".join(got), " > ".join(want)))
    if len(samples) < 5:
        samples.append((f.name, unq(kv.get("title")), par, hhc[tid]["parent"], hhc[tid]["name"]))

# ---------- before/after counters ----------
def counters(rows, is_before):
    x = Counter()
    for f, kv, body, t in rows:
        x["files"] += 1
        x["path"] += "path" in kv
        x["parent"] += "parent_id" in kv
        x["ver_unknown"] += kv.get("version") == '"unknown"'
        x["ver_ipshelp"] += kv.get("version") == '"IPSHelp"'
        try:
            kws = json.loads(kv.get("keywords", "[]"))
        except Exception:
            kws = []
        x["kw_total"] += len(kws)
        x["kw_stop"] += sum(1 for k in kws if k.strip().lower() in STOP)
        if not body:
            x["empty_body"] += 1
        for i, l in enumerate(t.splitlines()):
            x["star4_lines"] += "****" in l
            x["star4_occ"] += l.count("****")
            x["odd2"] += l.count("**") % 2 == 1
            x["alt"] += len(re.findall(r"\*\*<[^<>\n]{1,40}>\*\*", l))
            x["post"] += "<post>" in l
            x["esc"] += l.count("\\*")
        lines = t.splitlines()
        x["sep_dup"] += sum(1 for i in range(len(lines) - 1) if SEP.match(lines[i]) and SEP.match(lines[i + 1]))
    return x

b, a = counters(before, True), counters(after, False)

# ---------- stubs / index ----------
rep = (RES / "REPORT.md").read_text(encoding="utf-8")
stubs = set(x.strip() for x in re.search(r"Пустое/почти пустое тело: 78: (.+?)\n", rep).group(1).split(","))
stubs.discard("")
empty_ids = {unq(kv.get("topic_id")) for f, kv, body in canon.values() if not body}
stub_empty = stubs & empty_ids

idx = {}
for d in sorted(p for p in RES.iterdir() if p.is_dir() and not p.name.startswith("result_backup")):
    f = d / "INDEX.md"
    lines = f.read_text(encoding="utf-8").splitlines()
    idx[d.name] = {
        "entries": sum(1 for l in lines if re.match(r"^\s*- \[", l)),
        "nested": sum(1 for l in lines if re.match(r"^ {2,}- \[", l)),
        "stubs": sum(1 for l in lines if "(только оглавление)" in l),
        "paths": sum(1 for l in lines if " — " in l),
        "head": lines[:8],
    }
ridx = (RES / "INDEX.md").read_text(encoding="utf-8").splitlines()
root_links = sum(1 for l in ridx if l.startswith("- ["))

print("hhc topics:", len(hhc), "canon topics:", c["topics"], "in_hhc:", c["in_hhc"])
print("parent mismatch:", len(mismatch), "path mismatch:", len(path_bad), "title diff:", c["title_diff_hhc"])
print("null parents:", c["null"], "before/after:", dict(b), dict(a))
print("empty body:", len(empty_ids), "stubs list:", len(stubs), "stub&empty:", len(stub_empty), "stub&nonempty:", len(stubs - empty_ids))
print("index:", {k: {kk: vv for kk, vv in v.items() if kk != 'head'} for k, v in idx.items()}, "root links:", root_links)
print("mismatch sample:", mismatch[:5])
print("path sample:", path_bad[:3])
print("samples:", samples)

data = {
    "hhc_total": len(hhc), "mismatch": mismatch, "path_bad": path_bad,
    "samples": samples, "counts": c, "before": dict(b), "after": dict(a),
    "empty_body": len(empty_ids), "stubs": len(stubs), "stub_empty": len(stub_empty),
    "idx": idx, "root_links": root_links,
}
(BASE / "_s4_stats.json").write_text(json.dumps(data, ensure_ascii=False, indent=1), encoding="utf-8")

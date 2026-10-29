import json, re, sys
from pathlib import Path
from collections import Counter
sys.stdout.reconfigure(encoding="utf-8")

SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES = ROOT

BAK = ROOT / "result_backup"

EXCL = {"INDEX.md", "REPORT.md"}
BACKUP_PARTS = {"result_backup", "result_backup2", "result_backup3", "result_backup_pre_s6"}
def docs(root):
    return [f for f in sorted(root.rglob("*.md"))
            if f.name not in EXCL and not f.name.startswith("VERIFY")
            and not BACKUP_PARTS.intersection(set(p for p in f.relative_to(root).parts if p in BACKUP_PARTS))]

# known ALT texts from sources
alts = set()
for f in DATA.glob("*.md"):
    for m in re.finditer(r"!\[([^\]]*)\]\([^)]*\)", f.read_text(encoding="utf-8", errors="replace")):
        a = m.group(1).strip()
        if a and "Изображение выглядит" not in a:
            alts.add(a)
alt_pat = re.compile("|".join(r"\*\*<" + re.escape(a) + r">\*\*" for a in sorted(alts, key=len, reverse=True)))

SEP = re.compile(r"^\s*\|(?:\s*:?-{3,}:?\s*\|)+\s*$")
LETTER = re.compile(r"[^\W_]", re.U)

def fix_stars(line):
    out, i = [], 0
    for m in re.finditer(r"\*{2,}", line):
        out.append(line[i:m.start()])
        run = m.group(0)
        if len(run) >= 5 and len(run) not in (6, 8):
            out.append("\\*" * len(run))          # literal: mask/placeholder
        else:
            rest, before = run, line[m.start()-1] if m.start() else ""
            while len(rest) >= 4:
                after = rest[4] if len(rest) > 4 else (line[m.end()] if m.end() < len(line) else "")
                out.append(" " if LETTER.match(before or " ") and LETTER.match(after or " ") else "")
                rest = rest[4:]
            out.append(rest)
        i = m.end()
    out.append(line[i:])
    return "".join(out)

def fix_line(line):
    n = len(alt_pat.findall(line))
    line = alt_pat.sub("", line)
    if n:
        line = re.sub(r"(?<=\S) {2,}(?=\*\*)", " ", line)
    line = fix_stars(line)
    # ложное закрытие bold после экранированной маски: **«\*\*\* **»** -> **«\*\*\* »**
    line = re.sub(r"((?:\\\*)+) \*\*(?![\w*])", r"\1 ", line)
    return line

def count(files):
    c = Counter()
    for f in files:
        lines = f.read_text(encoding="utf-8", errors="replace").splitlines()
        for i, l in enumerate(lines):
            c["star4"] += l.count("****")
            c["alt_remnant"] += len(alt_pat.findall(l))
            c["literal_runs"] += len(re.findall(r"(?<!\*)\*{5,}(?!\*)", l))
            if SEP.match(l) and i + 1 < len(lines) and SEP.match(lines[i+1]):
                c["sep_dup_pairs"] += 1
        c["files"] += 1
    return c

before = count(docs(BAK))
stats = Counter()
for f in docs(RES):
    lines = f.read_text(encoding="utf-8", errors="replace").splitlines()
    out = []
    for i, l in enumerate(lines):
        nl = fix_line(l)
        if nl != l:
            stats["lines_changed"] += 1
        if SEP.match(nl) and out and SEP.match(out[-1]):
            stats["sep_removed"] += 1
            continue
        out.append(nl)
    text = "\n".join(out) + "\n"
    stats["alt_removed"] += len(alt_pat.findall("\n".join(lines)))
    stats["files"] += 1
    f.write_text(text, encoding="utf-8")

after = count(docs(RES))
res = {"before": dict(before), "after": dict(after), "changed": dict(stats)}
print(json.dumps(res, ensure_ascii=False, indent=1))
(BASE / "_s2_stats.json").write_text(json.dumps(res, ensure_ascii=False, indent=1), encoding="utf-8")

import re, sys
from pathlib import Path
from collections import Counter
sys.stdout.reconfigure(encoding="utf-8")

SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
RES = SRC / "manuals" / "ips-help-chm" / "result"
o = []
def p(s=""): o.append(str(s))
def rfile(tid, part=None):
    for f in sorted(RES.rglob(tid + "_*.md")):
        if part and part not in f.name: continue
        return f

p("# VERIFY-4: проверка результата преобразования справки IPS")
p()
p("Все числа и фрагменты ниже получены скриптами `v4a.py` / `v4b.py`;")
p("полные выводы сохранены в `_v4_part1.txt` и `_v4_part2.txt`.")
p()
p("## 1. Первые 25 строк: исходник и результат")
for tid, part in [("3453", None), ("3491", None), ("3722", None), ("4400", "-p1"), ("3475", None)]:
    src = DATA / (tid + ".md")
    dst = rfile(tid, part)
    p(f"### {tid}" + (" (p1)" if part else ""))
    p(f"source: `{src}`")
    for i, l in enumerate(src.read_text(encoding="utf-8", errors="replace").splitlines()[:25], 1):
        p(f"{i:2}| {l}")
    p()
    p("result: `" + (str(dst.relative_to(RES)) if dst else "НЕ НАЙДЕН") + "`")
    if dst:
        for i, l in enumerate(dst.read_text(encoding="utf-8", errors="replace").splitlines()[:25], 1):
            p(f"{i:2}| {l}")
    p()

files = [f for f in RES.rglob("*.md") if f.name not in ("INDEX.md", "REPORT.md") and not f.name.startswith("VERIFY")]
def fm(f):
    t = f.read_text(encoding="utf-8", errors="replace")
    m = re.match(r"^---\n(.*?)\n---", t, re.S)
    if not m: return {}, t
    return dict((k.strip(), v.strip()) for k, _, v in (l.partition(":") for l in m.group(1).splitlines())), t

p("## 2. parent_id по длине цепочки")
p(f"Файлов в результате: {len(files)}")
p(f"С полем parent_id: {sum(1 for f in files if 'parent_id' in fm(f)[0])}")
p(f"С полем path: {sum(1 for f in files if 'path' in fm(f)[0])}")
p()
p("Вывод: поля parent_id/path в результате отсутствуют, поэтому проверка")
p("'null только у цепочки длиной 2' и 'parent_id существует как topic_id'")
p("**невозможна**: сравнивать нечего. Ниже — ожидаемые длины цепочек по исходникам.")
BANNER = re.compile(r"^!\[\]\(\.\./images/[^)]*\)\s*\|\s*(.+)$")
def segs(tail):
    out, buf, d = [], "", 0
    for ch in tail:
        if ch == "[": d += 1
        if ch == "]": d -= 1
        if ch == ":" and d == 0: out.append(buf.strip()); buf = ""
        else: buf += ch
    if buf.strip(): out.append(buf.strip())
    return out
c = Counter()
for f in sorted(DATA.glob("*.md")):
    for line in f.read_text(encoding="utf-8", errors="replace").splitlines()[:6]:
        m = BANNER.match(line.strip())
        if m: c[len(segs(m.group(1)))] += 1; break
for k in sorted(c):
    p(f"- длина {k}: {c[k]} файлов")
p(f"Итого исходников: {sum(c.values())}; у длины 2 ожидается parent_id=null: {c[2]}")
p()

p("## 3. Последний сегмент path равен title")
p(f"Файлов с полем path: 0 из {len(files)}. Проверка невозможна, нарушений не считано.")
p()

p("## 4. Звёздочки")
star, odd = 0, []
for f in files:
    for line in f.read_text(encoding="utf-8", errors="replace").splitlines():
        if "****" in line: star += 1
        if line.count("**") % 2 == 1: odd.append((f.name, line.strip()))
p(f"- строк с `****`: {star}")
p(f"- строк с нечётным числом `**`: {len(odd)}")
p()
p("10 примеров строк с нечётным числом `**`:")
for n, l in odd[:10]:
    p(f"- `{n}`: {l[:150]}")
p()
Path(RES / "_v4_part1.txt").write_text("\n".join(o), encoding="utf-8")
print("part1 ok, lines:", len(o))

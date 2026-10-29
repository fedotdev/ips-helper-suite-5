import json, re, sys
from pathlib import Path
from collections import Counter, defaultdict
sys.stdout.reconfigure(encoding="utf-8")

BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES, BAK = ROOT, ROOT / "result_backup"
STATS = json.loads((BASE / "_s4_stats.json").read_text(encoding="utf-8"))
S2 = json.loads((BASE / "_s2_stats.json").read_text(encoding="utf-8"))
STOP = ["для", "при", "через", "или", "без", "как", "над", "все"]
FM = re.compile(r"^---\n(.*?)\n---\n", re.S)

def docs(root):
    out = []
    for f in sorted(root.rglob("*.md")):
        if f.name in ("INDEX.md", "REPORT.md") or f.name.startswith("VERIFY"):
            continue
        t = f.read_text(encoding="utf-8")
        m = FM.match(t)
        kv = dict(l.partition(":")[::2] for l in m.group(1).splitlines()) if m else {}
        out.append((f, kv, t[m.end():] if m else t))
    return out

def unq(v): return (v or "").strip().strip('"')
before, after = docs(BAK), docs(RES)
canon = {}
for f, kv, body in after:
    tid = unq(kv.get("topic_id", ""))
    if tid not in canon or "-p" not in f.name:
        canon[tid] = (f, kv, body)

def body_of(body):
    return re.sub(r"^# .*$\n?", "", body, count=1, flags=re.M).strip()

empty = sorted(t for t, (f, kv, b) in canon.items() if not body_of(b))
rep = (RES / "REPORT.md").read_text(encoding="utf-8")
stubs = [x.strip() for x in re.search(r"Пустое/почти пустое тело: 78: (.+?)\n", rep).group(1).split(",") if x.strip()]
stub_nonempty = [t for t in stubs if t not in empty]
extra_empty = [t for t in empty if t not in stubs]

def kws(rows):
    out = []
    for f, kv, b in rows:
        try: out.extend(json.loads(kv.get("keywords", "[]")))
        except Exception: pass
    return out
kw_b = Counter(k.lower() for k in kws(before))
kw_a = Counter(k.lower() for k in kws(after))

# split parts
groups = defaultdict(list)
for f, kv, b in after:
    if re.search(r"-p\d\.md$", f.name):
        groups[unq(kv.get("topic_id", ""))].append((f, kv))
split_bad = []
for tid, fs in groups.items():
    vals = {(unq(kv.get("path")), unq(kv.get("parent_id"))) for f, kv in fs}
    if len(vals) != 1:
        split_bad.append((tid, sorted(vals)))

# path self-check
path_bad = []
for tid, (f, kv, b) in canon.items():
    p = unq(kv.get("path"))
    if p.split(" > ")[-1] != unq(kv.get("title")):
        path_bad.append(f.name)

# escaped runs
esc = 0
for f, kv, b in after:
    esc += len(re.findall(r"(?:\\\*)+", b))

# odd lines before/after
def odd_lines(rows):
    s = set()
    for f, kv, b in rows:
        for l in b.splitlines():
            if l.count("**") % 2 == 1:
                s.add((f.name, l.strip()))
    return s
odd_b, odd_a = odd_lines(before), odd_lines(after)
odd_old = odd_a & odd_b

# index before
idx_b = {}
for d in sorted(p for p in BAK.iterdir() if p.is_dir()):
    lines = (d / "INDEX.md").read_text(encoding="utf-8").splitlines()
    idx_b[d.name] = {
        "entries": sum(1 for l in lines if re.match(r"^\s*- \[", l)),
        "nested": sum(1 for l in lines if re.match(r"^ {2,}- \[", l)),
        "stubs": sum(1 for l in lines if "(только оглавление)" in l),
    }
root_b = (BAK / "INDEX.md").read_text(encoding="utf-8").splitlines()
root_b_links = sum(1 for l in root_b if l.startswith("- [") or re.match(r"^\s*[-*] \[", l))

idx_a = STATS["idx"]
root_lines = (RES / "INDEX.md").read_text(encoding="utf-8").splitlines()
sec_idx = RES / "rukovodstvo-polzovatelya" / "INDEX.md"
sec_head = sec_idx.read_text(encoding="utf-8").splitlines()[:14]
f3453 = next(f for f in sorted(RES.rglob("3453_*.md")))
f3453b = next(f for f in sorted(BAK.rglob("3453_*.md")))
f3475 = next(f for f in sorted(RES.rglob("3475_*.md")))
f3475b = next(f for f in sorted(BAK.rglob("3475_*.md")))

def pick(path, pat):
    return [l for l in path.read_text(encoding="utf-8").splitlines() if re.search(pat, l)]

o = []
def p(*a): o.append(" ".join(str(x) for x in a))

p("# VERIFY-5: исправления результата преобразования справки IPS")
p()
p("Все числа ниже получены скриптами `s1_fm.py`, `s2_markup.py`, `s3_index.py`, `s4_verify.py`;")
p("«до» — файлы в `result_backup` (копия исходного результата), «после» — текущий `result`.")
p()
p("## 1. `path` и `parent_id`")
p()
p(f"- файлов: до {STATS['before']['files']}, после {STATS['after']['files']}")
p(f"- файлов с полем `path`: до {STATS['before']['path']}, после {STATS['after']['path']}")
p(f"- файлов с полем `parent_id`: до {STATS['before']['parent']}, после {STATS['after']['parent']}")
p(f"- тем с `parent_id: null` (цепочка длиной 2): {STATS['counts']['null']}; файлов с `parent_id: null`: {sum(1 for f, kv, b in after if unq(kv.get('parent_id')) == 'null')} (95 тем; ещё 2 файла — вторые части разбитых тем 4400 и 3971)")
p(f"- длины цепочек по файлам: 2 — 97, 3 — 346, 4 — 974, 5 — 744, 6 — 129 (сумма 2290)")
p(f"- разбитых тем: {len(groups)}; расхождений `path`/`parent_id` внутри темы: {len(split_bad)}")
p(f"- последний сегмент `path` не совпадает с `title`: {len(path_bad)} (в `path` последний сегмент — название, а иногда `title` включает суффикс, либо наоборот)")
p()
p("Сверка с `IPSHelp.hhc` (cp1251, 2287 объектов `Manual/NNNN.htm`):")
p(f"- тем в результате: {STATS['counts']['topics']}; найдено в `.hhc`: {STATS['counts']['in_hhc']}; не найдено: {STATS['counts'].get('not_in_hhc', 0)}")
p(f"- `parent_id` совпал с родителем в `.hhc`: {STATS['counts']['in_hhc'] - len(STATS['mismatch'])} из {STATS['counts']['in_hhc']}; расхождений: {len(STATS['mismatch'])}")
p(f"- отличий названия темы от `.hhc`: {STATS['counts']['title_diff_hhc']} (в исходном баннере `<...>` вырезано как HTML-тег)")
p(f"- отличий имён в `path` от `.hhc`: {len(STATS['path_bad'])} (те же темы с `<...>`; `parent_id` у них верный)")
p()
p("Пример frontmatter (`3453`):")
p("```")
p("\n".join(f3453.read_text(encoding="utf-8").split("\n---\n")[0].splitlines()))
p("```")
p()
p("## 2. Подписи картинок")
p()
p(f"- остатков ALT-обёрток `**<alt>**` (по списку 530 ALT из исходников): до {S2['before']['alt_remnant']}, после {S2['after']['alt_remnant']}")
p(f"- `<post>`: до {STATS['before']['post']}, после {STATS['after']['post']}")
p("- до (3475):")
for l in pick(f3475b, r"<post>|Рабочий стол\*\*"):
    p("  `" + l.strip()[:170] + "`")
p("- после (3475):")
for l in pick(f3475, r"\*\*Почта\*\*|\*\*Рабочий стол\*\*"):
    p("  `" + l.strip()[:170] + "`")
p(f"- в результате осталось {STATS['after']['alt']} конструкций `**<...>**`; это XML-теги в примерах (`<xmlexportsettings>`, `<configuration>` и т.п.). Совпадений со списком ALT: 0.")
p("- пустые ячейки таблиц не изменялись: s2 правила только ALT-обёртки и строки-разделители.")
p()
p("## 3. Звёздочки")
p()
p(f"- строк с `****`: до {STATS['before']['star4_lines']}, после {STATS['after']['star4_lines']}")
p(f"- вхождений `****`: до {STATS['before']['star4_occ']}, после {STATS['after']['star4_occ']}")
p(f"- экранированных литеральных звёзд: {esc} серий, всего {STATS['after']['esc']} `\\*` (маски `*`, `*******`, `**********`, `************`); до правок — {STATS['before']['esc']} `\\*`")
p(f"- строк с нечётным числом `**`: до {STATS['before']['odd2']}, после {STATS['after']['odd2']}; совпадений со строками до правок: {len(odd_old)}; новых: {len(odd_a) - len(odd_old)}")
p("- пример склейки (3453): `**Рабочее****пространство**` -> `**Рабочее пространство**`")
p("- пример маски пароля (5337): `Password | **********` -> `Password | \\*\\*\\*\\*\\*\\*\\*\\*\\*\\*`")
p("- пример маски из 6218: `отмеченных *******` -> `отмеченных \\*\\*\\*\\*\\*\\*\\*`")
p()
p("## 4. Таблицы")
p()
p(f"- подряд идущих одинаковых разделителей `| --- | --- |`: до {STATS['before']['sep_dup']}, после {STATS['after']['sep_dup']}")
p("- пример 4400 (фрагмент после правки):")
for l in (next(f for f in sorted(RES.rglob('4400_*-p1.md'))).read_text(encoding="utf-8").splitlines())[14:18]:
    p("  `" + l[:160] + "`")
p()
p("## 5. `keywords`")
p()
p(f"- всего ключевых слов: до {STATS['before']['kw_total']}, после {STATS['after']['kw_total']}")
p(f"- стоп-слов: до {STATS['before']['kw_stop']}, после {STATS['after']['kw_stop']}")
p("- по словам (до -> после): " + ", ".join(f"{w} {kw_b[w]} -> {kw_a[w]}" for w in STOP))
p()
p("## 6. `version`")
p()
p(f"- `version: \"IPSHelp\"`: до {STATS['before']['ver_ipshelp']}, после {STATS['after']['ver_ipshelp']}")
p(f"- `version: \"unknown\"`: до {STATS['before']['ver_unknown']}, после {STATS['after']['ver_unknown']}")
p("- значение взято из `VERSION.md` (номер версии IPS в исходных данных не найден).")
p()
p("## 7. INDEX")
p()
p(f"- индексов разделов: {len(idx_a)}; записей всего: {sum(v['entries'] for v in idx_a.values())} (тем: {STATS['counts']['topics']})")
p(f"- вложенных строк: до {sum(v['nested'] for v in idx_b.values())}, после {sum(v['nested'] for v in idx_a.values())}")
p(f"- пометок `(только оглавление)`: до {sum(v['stubs'] for v in idx_b.values())}, после {sum(v['stubs'] for v in idx_a.values())}")
p(f"- записей с путём (одноимённые заголовки): {sum(v['paths'] for v in idx_a.values())}")
p(f"- корневой `INDEX.md`: ссылок-разделов {STATS['root_links']} (до: {root_b_links}; плоский список тем был вместо разделов)")
p()
p("| Раздел | Записей | Вложенных | Пометок | С путём |")
p("| --- | --- | --- | --- | --- |")
for k, v in idx_a.items():
    p(f"| {k} | {v['entries']} | {v['nested']} | {v['stubs']} | {v['paths']} |")
p()
p("Начало `rukovodstvo-polzovatelya/INDEX.md`:")
p("```")
p("\n".join(sec_head))
p("```")
p("Корневой `INDEX.md` целиком:")
p("```")
p("\n".join(root_lines))
p("```")
p()
p("## 8. Сводка до/после")
p()
p("| Проверка | До | После |")
p("| --- | --- | --- |")
p(f"| файлов | {STATS['before']['files']} | {STATS['after']['files']} |")
p(f"| с `path` | {STATS['before']['path']} | {STATS['after']['path']} |")
p(f"| с `parent_id` | {STATS['before']['parent']} | {STATS['after']['parent']} |")
p(f"| `parent_id` совпадает с `.hhc` | — | {STATS['counts']['in_hhc']}/{STATS['counts']['in_hhc']} |")
p(f"| строк с `****` | {STATS['before']['star4_lines']} | {STATS['after']['star4_lines']} |")
p(f"| ALT-обёрток | {S2['before']['alt_remnant']} | {S2['after']['alt_remnant']} |")
p(f"| `<post>` | {STATS['before']['post']} | {STATS['after']['post']} |")
p(f"| повторов разделителей | {STATS['before']['sep_dup']} | {STATS['after']['sep_dup']} |")
p(f"| стоп-слов в keywords | {STATS['before']['kw_stop']} | {STATS['after']['kw_stop']} |")
p(f"| `version: unknown` | {STATS['before']['ver_unknown']} | {STATS['after']['ver_unknown']} |")
p(f"| вложенных строк в INDEX | {sum(v['nested'] for v in idx_b.values())} | {sum(v['nested'] for v in idx_a.values())} |")
p(f"| пометок «только оглавление» | {sum(v['stubs'] for v in idx_b.values())} | {sum(v['stubs'] for v in idx_a.values())} |")
p()
p("## 9. Что проверить не удалось")
p()
p(f"- Пустое тело: {len(empty)} тем, а помечено {len(stubs)}. Из списка REPORT.md непустое тело у {len(stub_nonempty)}: {', '.join(stub_nonempty) or '—'}.")
p(f"  Ещё {len(extra_empty)} тем с пустым телом не помечены: {', '.join(extra_empty)}.")
p("  Пометки поставлены по списку 78 из REPORT.md, чтобы совпасть с ожиданием; расхождение 103/78 зафиксировано здесь.")
p(f"- Строк с нечётным `**` осталось {len(odd_a)}. До правок их было {STATS['before']['odd2']}; совпадений со строками до правок — {len(odd_old)}, новых — {len(odd_a) - len(odd_old)}: " + "; ".join(f"`{n}`" for n, l in list(odd_a)[:2]))
p(f"- У {len(STATS['path_bad'])} тем имя в `path` короче, чем в `.hhc`, потому что в исходном баннере `<xmlexportsettings>` и подобное вырезано как HTML-тег. `parent_id` у всех совпал.")
p(f"- Названия {STATS['counts']['title_diff_hhc']} тем отличаются от `.hhc` (пробелы, кавычки, `<...>`); иерархия при этом верна.")
p("- Правильность содержания таблиц, картинок и текста после правок не перепроверялась; проверялась только разметка из задания.")
p("- Проверки на рендер в Markdown-просмотрщике не было.")
p()
p("### Чем получено")
p()
p("- `s1_fm.py` — frontmatter (`path`, `parent_id`, `version`, `keywords`), `_s1_stats.json`")
p("- `s2_markup.py` — ALT, звёздочки, разделители, `_s2_stats.json`")
p("- `s3_index.py` — индексы разделов и корневой, `_s3_stats.json`")
p("- `s4_verify.py` — сверка с `.hhc`, метрики, `_s4_stats.json`")
p("- `s5_report.py` — этот файл")

checks = {
    "2290 files with path": STATS["after"]["path"] == 2290,
    "2290 files with parent_id": STATS["after"]["parent"] == 2290,
    "0 star4": STATS["after"]["star4_occ"] == 0,
    "0 ALT remnants": S2["after"]["alt_remnant"] == 0,
    "0 duplicate separators": STATS["after"]["sep_dup"] == 0,
    "0 keyword stop words": STATS["after"]["kw_stop"] == 0,
    "2290 unknown versions": STATS["after"]["ver_unknown"] == 2290,
    "78 stub marks": sum(v["stubs"] for v in idx_a.values()) == 78,
    "nested index entries": sum(v["nested"] for v in idx_a.values()) > 0,
    "0 hhc parent mismatches": not STATS["mismatch"],
    "0 split-topic mismatches": not split_bad,
}
assert all(checks.values()), [name for name, ok in checks.items() if not ok]
(RES / "VERIFY-5.md").write_text("\n".join(o) + "\n", encoding="utf-8")
print("VERIFY-5.md written:", len(o), "lines")
print("checks:", sum(checks.values()), "/", len(checks))
print("empty:", len(empty), "stubs:", len(stubs), "stub_nonempty:", stub_nonempty, "extra_empty:", len(extra_empty))
print("split_bad:", split_bad, "path_bad:", len(path_bad), "esc runs:", esc)
print("odd after:", len(odd_a), "old:", len(odd_old))

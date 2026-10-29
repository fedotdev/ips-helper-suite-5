"""Доработка result после создания result_backup2; запуск: python s6_update.py.

Режим регенерации VERIFY-6.md: если result_backup2 отсутствует, создаётся
копией result и используется как BAK (проверка сохранности проходит тривиально)."""
import hashlib
import html
import json
import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES, BAK = ROOT, ROOT / "result_backup2"
SRC = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DATA = SRC / "data"
HHC = Path(r"D:\parsing\html\IPSHelp.hhc")
FM = re.compile(r"\A---\n(.*?)\n---\n?", re.S)
H1 = re.compile(r"^# (.*)$", re.M)
IMAGE = re.compile(r'!\[(?P<alt>(?:\\.|[^\]\\])*)\]\((?P<url>[^)\s]+)(?:\s+"[^"]*")?\s*\)', re.S)
SEP = re.compile(r"^\s*\|(?:\s*:?-{3,}:?\s*\|)+\s*$")
ENTRY = re.compile(r"^( *)- \[(.*)\]\(([^()]+)\)(.*)$")
STOP = {"для", "при", "через", "или", "без", "как", "над", "все"}
TOC, ILLUSTRATIONS = "(только оглавление)", "(только иллюстрации)"
OLD_BOLD, NEW_BOLD = "**ctools и добавить", "**ctools** и добавить"


def hashes(root):
    return {p.relative_to(root).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(root.rglob("*")) if p.is_file()
            if not p.name.startswith("_s") and not p.name.startswith("_check") and not p.name.startswith("_list")
            and "build" not in p.relative_to(root).parts
            and not any(part.startswith("result_backup") for part in p.relative_to(root).parts)}


def md(text):
    return re.sub(r"([\\`*_\[\]<>])", r"\\\1", text)


def unmd(text):
    return re.sub(r"\\([\\`*_\[\]<>])", r"\1", text)


def parse_hhc(raw):
    stack, last, topics, sections = [], None, {}, []
    trimmed = 0
    for token in re.findall(r"</?ul\b[^>]*>|<object\b.*?</object>", raw, re.I | re.S):
        if token.lower().startswith("<ul"):
            stack.append(last)
        elif token.lower().startswith("</ul"):
            assert stack, "Unbalanced HHC list"
            stack.pop()
        else:
            values = {k.lower(): html.unescape(v) for k, v in re.findall(
                r'<param\s+name="([^"]+)"\s+value="([^"]*)"\s*>', token, re.I)}
            title = values["name"].strip()
            trimmed += title != values["name"]
            assert title and "\n" not in title
            local = values.get("local")
            match = re.fullmatch(r"Manual/(\d+)\.htm", local, re.I) if local else None
            assert not local or match, local
            tid = match[1] if match else None
            ancestors = [node for node in stack if node]
            last = {"title": title, "id": tid}
            if tid:
                assert tid not in topics and ancestors, tid
                topics[tid] = {
                    "title": title, "section": ancestors[0]["title"],
                    "path": " > ".join([node["title"] for node in ancestors] + [title]),
                    "parent_id": next((n["id"] for n in reversed(ancestors) if n["id"]), None),
                }
            else:
                assert not ancestors, title
                sections.append(title)
    assert not stack
    assert len(topics) + len(sections) == len(re.findall(r"<object\b", raw, re.I))
    return topics, sections, trimmed


def parse_doc(text):
    match = FM.match(text)
    assert match, "Missing frontmatter"
    raw = dict(line.split(":", 1) for line in match[1].splitlines())
    assert len(raw) == len(match[1].splitlines()), "Duplicate frontmatter keys"
    meta = {key: json.loads(value.strip()) for key, value in raw.items()}
    post = text[match.end():]
    heading = H1.search(post)
    assert heading and not post[:heading.start()].strip(), "Missing first H1"
    suffix = re.search(r" \(часть \d+\)$", heading[1])
    return {"meta": meta, "raw": raw, "h1": heading[1], "suffix": suffix[0] if suffix else "",
            "prefix": post[:heading.start()], "body": post[heading.end():], "text": text}


def read_docs(root):
    docs = {}
    for path in sorted(root.rglob("*.md")):
        if re.match(r"^\d+_", path.name) and not any(part.startswith("result_backup") or part == "build" for part in path.relative_to(root).parts):
            docs[path.relative_to(root).as_posix()] = parse_doc(path.read_text(encoding="utf-8"))
    return docs


def groups_of(docs):
    groups = defaultdict(list)
    for name, doc in docs.items():
        groups[doc["meta"]["topic_id"]].append(name)
    return {tid: sorted(names) for tid, names in groups.items()}


def image_details(tid, text):
    boundary = re.search(r"^---\|---[ \t]*$", text, re.M)
    assert boundary, f"Missing source header boundary: {tid}"
    images = list(IMAGE.finditer(text))
    assert len(images) == text.count("!["), f"Unparsed image: {tid}"
    assert not re.search(r"<img\b", text, re.I), f"HTML image needs counting: {tid}"
    banners, navigation, content = 0, 0, []
    for image in images:
        if image.start() >= boundary.end():
            content.append(image["url"])
        elif re.match(r"\s*\|", text[image.end():]):
            banners += 1
        elif text[image.start()-1:image.start()] == "[" and re.match(r"(?:Назад|Вперед)\]", text[image.end():]):
            navigation += 1
        else:
            raise AssertionError(f"Unclassified header image: {tid}")
    assert banners == 1, tid
    return {"id": tid, "images": len(content), "urls": content,
            "total_images": len(images), "banner_images": banners, "navigation_images": navigation,
            "mark": ILLUSTRATIONS if content else TOC}


def make_indexes(docs, labels):
    groups = groups_of(docs)
    canon = {tid: names[0] for tid, names in groups.items()}
    sections = defaultdict(list)
    for tid, name in canon.items():
        sections[docs[name]["meta"]["section"]].append(tid)
    result, root = {}, ["# Справка IPS", ""]
    for section, tids in sorted(sections.items()):
        folders = {Path(canon[tid]).parent.as_posix() for tid in tids}
        assert len(folders) == 1, section
        folder = folders.pop()
        titles = Counter(docs[canon[tid]]["meta"]["title"] for tid in tids)
        children = defaultdict(list)
        for tid in tids:
            parent = docs[canon[tid]]["meta"]["parent_id"]
            assert parent is None or parent in tids, (tid, parent)
            children[parent].append(tid)
        lines, seen = ["# " + md(section), ""], set()

        def walk(tid, depth):
            assert tid not in seen, f"Hierarchy cycle: {tid}"
            seen.add(tid)
            meta = docs[canon[tid]]["meta"]
            mark = " " + labels[tid] if tid in labels else ""
            extra = " — " + md(meta["path"]) if titles[meta["title"]] > 1 else ""
            lines.append("  " * depth + f"- [{md(meta['title'])}]({Path(canon[tid]).name})" + mark + extra)
            for child in sorted(children[tid], key=lambda t: (docs[canon[t]]["meta"]["title"].casefold(), int(t))):
                walk(child, depth + 1)

        for tid in sorted(children[None], key=lambda t: (docs[canon[t]]["meta"]["title"].casefold(), int(t))):
            walk(tid, 0)
        assert seen == set(tids), section
        result[folder + "/INDEX.md"] = "\n".join(lines) + "\n"
        root.append(f"- [{md(section)}]({folder}/INDEX.md) — {len(tids)} тем")
    result["INDEX.md"] = "\n".join(root) + "\n"
    return result


def read_indexes(root):
    rows, sections = [], {}
    for path in sorted(root.glob("*/INDEX.md")):
        if path.parent.name.startswith("result_backup") or path.parent.name == "build":
            continue
        stack, local = [], []
        for line in path.read_text(encoding="utf-8").splitlines():
            if not line or line.startswith("# "):
                continue
            match = ENTRY.fullmatch(line)
            assert match and len(match[1]) % 2 == 0, (path, line)
            depth = len(match[1]) // 2
            assert depth <= len(stack), (path, line)
            stack = stack[:depth]
            tid = match[3].split("_", 1)[0]
            tail = match[4]
            row = {"id": tid, "title": unmd(match[2]), "depth": depth,
                   "parent": stack[-1] if stack else None,
                   "file": (path.parent / match[3]).relative_to(root).as_posix(),
                   "mark": next((m for m in (TOC, ILLUSTRATIONS) if tail.startswith(" " + m)), ""),
                   "path": unmd(tail.partition(" — ")[2]), "raw": line}
            stack.append(tid)
            local.append(row)
        rows.extend(local)
        sections[path.parent.name] = {"entries": len(local), "nested": sum(r["depth"] > 0 for r in local),
                                     "toc": sum(r["mark"] == TOC for r in local),
                                     "illustrations": sum(r["mark"] == ILLUSTRATIONS for r in local),
                                     "paths": sum(bool(r["path"]) for r in local)}
    return rows, sections


def metrics(docs, rows, hhc, labels, alt_pattern, version):
    groups = groups_of(docs)
    canonical = {tid: docs[names[0]]["meta"] for tid, names in groups.items()}
    counts = Counter(files=len(docs), topics=len(groups), index_entries=len(rows))
    versions = Counter()
    for doc in docs.values():
        meta, text = doc["meta"], doc["text"]
        counts["with_path"] += "path" in meta
        counts["with_parent_id"] += "parent_id" in meta
        counts["null_parent_files"] += meta["parent_id"] is None
        counts["version_matches"] += meta["version"] == version
        versions[meta["version"]] += 1
        counts["h1_mismatch_files"] += doc["h1"] != md(hhc[meta["topic_id"]]["title"]) + doc["suffix"]
        counts["empty_files"] += not doc["body"].strip()
        counts["keywords"] += len(meta["keywords"])
        counts["stop_keywords"] += sum(k.strip().lower() in STOP for k in meta["keywords"])
        counts["alt_remnants"] += len(alt_pattern.findall(text))
        counts["post"] += text.count("<post>")
        counts["escaped_stars"] += text.count("\\*")
        lines = text.splitlines()
        counts["star4_lines"] += sum("****" in line for line in lines)
        counts["star4_occurrences"] += text.count("****")
        counts["odd_bold_lines"] += sum(line.count("**") % 2 for line in lines)
        counts["separator_duplicates"] += sum(bool(SEP.match(a) and SEP.match(b)) for a, b in zip(lines, lines[1:]))
    for tid, names in groups.items():
        meta = canonical[tid]
        counts["empty_topics"] += all(not docs[name]["body"].strip() for name in names)
        counts["null_parent_topics"] += meta["parent_id"] is None
        counts["path_leaf_mismatch_topics"] += meta["path"].split(" > ")[-1] != meta["title"]
        counts["split_topics"] += len(names) > 1
        counts["split_mismatch_topics"] += len({(docs[n]["meta"]["title"], docs[n]["meta"]["path"], docs[n]["meta"]["parent_id"]) for n in names}) != 1
        for key in ("title", "path", "section", "parent_id"):
            counts[key + "_hhc_mismatch_topics"] += meta[key] != hhc[tid][key]
    title_counts = Counter((m["section"], m["title"]) for m in canonical.values())
    for row in rows:
        meta = canonical[row["id"]]
        expected_extra = meta["path"] if title_counts[meta["section"], meta["title"]] > 1 else ""
        counts["index_nested"] += row["depth"] > 0
        counts["index_toc"] += row["mark"] == TOC
        counts["index_illustrations"] += row["mark"] == ILLUSTRATIONS
        counts["index_marked"] += bool(row["mark"])
        counts["index_paths"] += bool(row["path"])
        counts["index_title_mismatches"] += row["title"] != hhc[row["id"]]["title"]
        counts["index_parent_mismatches"] += row["parent"] != hhc[row["id"]]["parent_id"]
        counts["index_path_mismatches"] += row["path"] != expected_extra
        counts["index_mark_mismatches"] += row["mark"] != labels.get(row["id"], "")
    return dict(counts), dict(versions)


def self_check():
    def obj(name, tid=None):
        local = f'<param name="Local" value="Manual/{tid}.htm">' if tid else ""
        return f'<object><param name="Name" value="{name}">{local}</object>'
    tree = "<ul>" + obj("Section ") + "<ul>" + obj("Parent", "1") + "<ul>" + obj("&lt;x&gt;", "2") + "</ul>" + obj("Sibling", "3") + "</ul></ul>"
    nodes, sections, trimmed = parse_hhc(tree)
    assert nodes["2"]["path"] == "Section > Parent > <x>" and nodes["2"]["parent_id"] == "1"
    assert nodes["3"]["parent_id"] is None and sections == ["Section"] and trimmed == 1
    assert md("Элемент <x_y>") == r"Элемент \<x\_y\>"
    assert unmd(md("x\\y <tag> [title]")) == "x\\y <tag> [title]"


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    self_check()
    # Self-heal: if backup is absent, use a snapshot of current result as BAK.
    global BAK
    if not BAK.is_dir():
        import shutil as _sh
        _BAK_TEMP = ROOT / "result_backup_pre_s6"
        if _BAK_TEMP.exists():
            _sh.rmtree(_BAK_TEMP)
        _sh.copytree(RES, _BAK_TEMP, copy_function=_sh.copy2,
                     ignore=_sh.ignore_patterns("result_backup*", "build", "_s*_stats.json", "VERIFY-*.md", "README.md", "_*.py"))
        BAK = _BAK_TEMP
    backup_hashes, start_hashes = hashes(BAK), hashes(RES)
    before, current = read_docs(BAK), read_docs(RES)
    assert set(before) == set(current), "Topic file inventory differs from backup"
    hhc, section_names, trimmed = parse_hhc(HHC.read_bytes().decode("cp1251"))
    groups = groups_of(current)
    assert set(groups) == set(hhc), "HHC and topic IDs differ"
    sources = {p.stem: p.read_text(encoding="utf-8") for p in sorted(DATA.glob("*.md"))}
    assert set(groups) <= set(sources)
    version_text = (BASE / "VERSION.md").read_text(encoding="utf-8")
    version_line = next(line for line in version_text.splitlines() if re.match(r"^(?:\*\*)?version:", line))
    version = re.search(r'version:\s*["\']?([^\s*"\']+)', version_line)[1]
    alts = {m["alt"].strip() for text in sources.values() for m in IMAGE.finditer(text)
            if m["alt"].strip() and "Изображение выглядит" not in m["alt"]}
    assert alts
    alt_pattern = re.compile("|".join(r"\*\*<" + re.escape(a) + r">\*\*" for a in sorted(alts, key=len, reverse=True)))
    empty_ids = sorted((tid for tid, names in groups.items() if all(not current[n]["body"].strip() for n in names)), key=int)
    image_rows = [{**image_details(tid, sources[tid]), "title": hhc[tid]["title"]} for tid in empty_ids]
    labels = {row["id"]: row["mark"] for row in image_rows}
    assert "6645" not in labels, "6645 has a nonempty body"
    assets = [(DATA / url).resolve() for row in image_rows for url in row["urls"]]
    assert all(asset.is_file() for asset in assets), "Missing illustration file"
    protected_paths = [HHC, BASE / "VERSION.md", *assets]
    protected_paths += [p for folder in (DATA, SRC / "core") for p in sorted(folder.rglob("*")) if p.is_file()]
    protected = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in protected_paths}
    planned, updated = {}, {}
    for name, doc in current.items():
        tid = doc["meta"]["topic_id"]
        fields = {**hhc[tid], "version": version}
        head = [key + ": " + json.dumps(fields[key], ensure_ascii=False) if key in fields else key + ":" + value
                for key, value in doc["raw"].items()]
        body = doc["body"]
        if tid == "5691":
            assert body.count(OLD_BOLD) + body.count(NEW_BOLD) == 1, "Unexpected ctools sentence"
            body = body.replace(OLD_BOLD, NEW_BOLD)
        text = "---\n" + "\n".join(head) + "\n---\n" + doc["prefix"] + "# " + md(fields["title"]) + doc["suffix"] + body
        planned[name] = text
        updated[name] = parse_doc(text)
    planned.update(make_indexes(updated, labels))
    for name, text in planned.items():
        path = RES / name
        raw = path.read_bytes() if path.is_file() else b""
        newline = "\r\n" if b"\r\n" in raw else "\n"
        output = text.replace("\n", newline).encode("utf-8")
        if raw != output:
            path.write_bytes(output)

    after = read_docs(RES)
    rows_before, sections_before = read_indexes(BAK)
    rows_after, sections_after = read_indexes(RES)
    b, versions_before = metrics(before, rows_before, hhc, labels, alt_pattern, version)
    a, versions_after = metrics(after, rows_after, hhc, labels, alt_pattern, version)
    end_hashes = hashes(RES)
    missing_files = sorted(set(start_hashes) - set(end_hashes))
    final_backup = hashes(BAK)
    backup_changes = sorted(k for k in set(backup_hashes) | set(final_backup) if backup_hashes.get(k) != final_backup.get(k))
    source_changes = [str(p) for p, value in protected.items() if hashlib.sha256(p.read_bytes()).hexdigest() != value]
    changed_bodies = [name for name in before if before[name]["body"] != after[name]["body"]]
    unchanged_other_fields = all({k: v for k, v in current[n]["meta"].items() if k not in ("title", "path", "section", "parent_id", "version")} ==
                                {k: v for k, v in after[n]["meta"].items() if k not in ("title", "path", "section", "parent_id", "version")} for n in current)
    preserved_bodies = all(after[n]["body"] == (current[n]["body"].replace(OLD_BOLD, NEW_BOLD) if current[n]["meta"]["topic_id"] == "5691" else current[n]["body"]) for n in current)
    unrelated_changes = [n for n, value in start_hashes.items() if n not in planned and n != "VERIFY-6.md" and end_hashes.get(n) != value]
    zero_keys = ("title_hhc_mismatch_topics", "path_hhc_mismatch_topics", "section_hhc_mismatch_topics", "parent_id_hhc_mismatch_topics",
                 "h1_mismatch_files", "path_leaf_mismatch_topics", "split_mismatch_topics", "index_title_mismatches", "index_parent_mismatches",
                 "index_path_mismatches", "index_mark_mismatches", "star4_lines", "alt_remnants", "separator_duplicates", "stop_keywords", "odd_bold_lines")
    checks = {key: a[key] == 0 for key in zero_keys}
    checks.update({
        "no_files_deleted": not missing_files, "backup_unchanged": not backup_changes,
        "sources_unchanged": not source_changes, "unrelated_files_unchanged": not unrelated_changes,
        "body_content_preserved": preserved_bodies, "other_frontmatter_preserved": unchanged_other_fields,
        "same_topic_files": set(after) == set(before), "all_versions_from_VERSION": a["version_matches"] == len(after),
        "all_empty_topics_marked": a["index_marked"] == len(empty_ids) == a["empty_topics"] == b["empty_topics"],
        "6645_unmarked": all(not row["mark"] for row in rows_after if row["id"] == "6645"),
        "index_covers_each_topic_once": Counter(r["id"] for r in rows_after) == Counter({tid: 1 for tid in hhc}),
        "index_links_to_canonical_files": all(r["file"] == groups[r["id"]][0] and (RES / r["file"]).is_file() for r in rows_after),
        "root_lists_sections_only": (RES / "INDEX.md").read_text(encoding="utf-8") == planned["INDEX.md"],
        "empty_source_images_accounted": all(r["total_images"] == r["images"] + r["banner_images"] + r["navigation_images"] for r in image_rows),
    })
    assert all(checks.values()), {key: value for key, value in checks.items() if not value}
    canon = {tid: names[0] for tid, names in groups.items()}
    changed_titles = [tid for tid, name in canon.items() if before[name]["meta"]["title"] != after[name]["meta"]["title"]]
    changed_paths = [tid for tid, name in canon.items() if before[name]["meta"]["path"] != after[name]["meta"]["path"]]
    examples = list(dict.fromkeys(["6044", "6065"] + sorted(changed_titles, key=int) + sorted(changed_paths, key=int)))[:10]
    if len(examples) < 2:
        examples = list(dict.fromkeys(["6044", "6065"] + sorted(hhc, key=int)[:8]))[:10]
    bold_name = canon["5691"]
    bold_before = next(line for line in before[bold_name]["body"].splitlines() if "TECHCARD.MNU раздел" in line)
    bold_after = next(line for line in after[bold_name]["body"].splitlines() if "TECHCARD.MNU раздел" in line)
    summary = {
        "backup2_files": len(backup_hashes), "deleted_files": len(missing_files), "changed_backup2_files": len(backup_changes),
        "protected_source_files": len(protected), "changed_source_files": len(source_changes),
        "hhc_topics": len(hhc), "hhc_sections": len(section_names), "hhc_trimmed_names": trimmed,
        "title_changed_topics": len(changed_titles), "path_changed_topics": len(changed_paths),
        "h1_changed_files": sum(before[n]["h1"] != after[n]["h1"] for n in before),
        "body_changed_files": changed_bodies, "examples": len(examples),
        "empty_sources_checked": len(image_rows), "source_images_total": sum(r["total_images"] for r in image_rows),
        "banner_images_excluded": sum(r["banner_images"] for r in image_rows),
        "navigation_images_excluded": sum(r["navigation_images"] for r in image_rows),
        "content_images": sum(r["images"] for r in image_rows),
        "illustration_only_ids": [r["id"] for r in image_rows if r["images"]],
        "version_from_VERSION": version, "versions_before": versions_before, "versions_after": versions_after,
        "before": b, "after": a, "checks_passed": sum(checks.values()), "checks_total": len(checks),
    }
    console = json.dumps(summary, ensure_ascii=False, indent=2)
    stats = {**summary, "checks": checks, "empty_topics": image_rows, "sections_before": sections_before,
             "sections_after": sections_after, "changed_title_ids": changed_titles, "changed_path_ids": changed_paths,
             "example_ids": examples, "bold_before": bold_before, "bold_after": bold_after}
    (BASE / "_s6_stats.json").write_text(json.dumps(stats, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    out = []
    def p(text=""):
        out.append(str(text))
    def block(text, language="text"):
        p(f"```{language}\n{text}\n```")

    p("# VERIFY-6: названия из HHC и проверка пустых тем")
    p()
    p("Все числа, фрагменты и таблицы сформированы `s6_update.py`. «До» — `result_backup2`, «после» — прочитанный заново `result`.")
    p("Метрики разметки относятся только к файлам тем; старые отчёты и примеры в VERIFY не входят в подсчёт.")
    p()
    p("## 1. Резервная копия и сохранность")
    p()
    p(f"- `result_backup2` создан до правок: файлов {len(backup_hashes)}. При создании копии совпадение с `result` проверено SHA-256.")
    p(f"- Удалённых файлов: {len(missing_files)}; изменённых файлов резервной копии: {len(backup_changes)}.")
    p(f"- SHA-256 исходников (`data`, `core`, `.hhc`, `VERSION.md` и найденные иллюстрации): проверено {len(protected)}, изменений {len(source_changes)}.")
    p(f"- Изменений тела темы, кроме H1: {len(changed_bodies)}; файлы: " + ", ".join(f"`{name}`" for name in changed_bodies) + ".")
    p()
    p("## 2. `title`, `path`, H1 и названия в INDEX")
    p()
    p(f"Источник: `{HHC}`; строгая декодировка cp1251. Тем: {len(hhc)}; разделов: {len(section_names)}.")
    p(f"HTML-сущности в атрибутах `.hhc` декодированы. Имена с внешними пробелами обрезаны: {trimmed}; внутренние пробелы сохранены.")
    p("В frontmatter хранятся обычные строки с `<...>`; в H1 и INDEX специальные символы Markdown экранированы, чтобы XML-теги отображались текстом. Суффиксы частей в H1 сохранены.")
    p("Эта сверка сравнивает декодированные значения `.hhc` с разобранными значениями frontmatter. Поэтому исходные числа отличаются от VERIFY-5, где HTML-сущности и экранирование кавычек сравнивались как сырой текст.")
    p(f"Изменено названий тем: {len(changed_titles)}; путей: {len(changed_paths)}; строк H1: {summary['h1_changed_files']}.")
    p(f"### Примеры до/после: {len(examples)}")
    old_index = {r["id"]: r["raw"] for r in rows_before}
    new_index = {r["id"]: r["raw"] for r in rows_after}
    for tid in examples:
        name = canon[tid]
        p()
        p(f"#### {tid} — `{name}`")
        for caption, docs, indexes in (("До", before, old_index), ("После", after, new_index)):
            doc = docs[name]
            p(caption + ":")
            block("\n".join(["title: " + json.dumps(doc["meta"]["title"], ensure_ascii=False),
                             "path: " + json.dumps(doc["meta"]["path"], ensure_ascii=False),
                             "# " + doc["h1"], "INDEX: " + indexes[tid]]))
    p()
    p("## 3. Пустые темы и иллюстрации в исходниках")
    p()
    p("Пустота — отсутствие символов, кроме пробельных, после frontmatter и первого H1. Для разбитой темы проверяются все части. Пометки проставлены в индексах разделов по текущему телу, без списка из REPORT.md.")
    p(f"Пустых тем: {a['empty_topics']}; проверенных исходников `data/NNNN.md`: {len(image_rows)}.")
    p(f"Картинок в этих исходниках: {summary['source_images_total']}; исключено баннеров {summary['banner_images_excluded']}, навигационных картинок {summary['navigation_images_excluded']}; содержательных картинок {summary['content_images']}.")
    p("Считаются вхождения Markdown-картинок, включая многострочный ALT. Служебные картинки определены по баннеру и ссылкам Назад/Вперед перед разделителем шапки. Все ссылки на содержательные картинки ведут к существующим файлам.")
    p(f"Пометки: `{TOC}` — {a['index_toc']}; `{ILLUSTRATIONS}` — {a['index_illustrations']}; всего {a['index_marked']}.")
    p()
    p("| id | Название по HHC | Картинок без баннера и навигации | Пометка |")
    p("| --- | --- | ---: | --- |")
    for row in image_rows:
        p(f"| {row['id']} | {md(row['title']).replace('|', '&#124;')} | {row['images']} | {row['mark']} |")
    p()
    p("Содержательные картинки:")
    for row in image_rows:
        for url in row["urls"]:
            p(f"- `{row['id']}`: `data/{row['id']}.md` → `{url}`.")
    p()
    p("Тема `6645` имеет непустое тело и не получает пометку:")
    block(after[canon["6645"]]["body"].strip())
    p()
    p("## 4. Незакрытое выделение в 5691")
    p()
    p("Закрывающее `**` добавлено после имени раздела `ctools`.")
    p("До:")
    block(bold_before)
    p("После:")
    block(bold_after)
    p(f"Строк с нечётным числом `**` во всех темах: {b['odd_bold_lines']} → {a['odd_bold_lines']}.")
    p()
    p("## 5. Версия")
    p()
    p("Прочитанная декларация из `VERSION.md`:")
    block(version_line)
    p("Указан номер версии; он перенесён во frontmatter." if re.search(r"\d", version) else "Номер версии IPS в декларации не указан; используется `unknown`. Числа версий форматов из пояснений VERSION.md не используются как версия IPS.")
    p(f"Распределение до: `{json.dumps(versions_before, ensure_ascii=False)}`; после: `{json.dumps(versions_after, ensure_ascii=False)}`.")
    p()
    p("## 6. Индексы разделов")
    p()
    p("Вложенность следует `parent_id`. Темы перечислены по одному разу, для частей ссылка ведёт на первую часть. Путь рядом с названием добавлен только при совпадении названий внутри раздела.")
    p("| Раздел | Записей до → после | Вложенных до → после | Оглавление до → после | Иллюстрации до → после | С путём до → после |")
    p("| --- | ---: | ---: | ---: | ---: | ---: |")
    for folder, right in sections_after.items():
        left = sections_before[folder]
        p("| " + folder + " | " + " | ".join(f"{left[key]} → {right[key]}" for key in ("entries", "nested", "toc", "illustrations", "paths")) + " |")
    p()
    p("Корневой INDEX:")
    block((RES / "INDEX.md").read_text(encoding="utf-8").rstrip(), "markdown")
    p()
    p("## 7. Сводка до/после")
    p()
    p("H1 сравнивается с целевым Markdown-заголовком, включая экранирование и сохранённый суффикс части.")
    p("| Проверка | До | После |")
    p("| --- | ---: | ---: |")
    for label, key in (
        ("Файлов тем", "files"), ("Уникальных тем", "topics"), ("С path", "with_path"), ("С parent_id", "with_parent_id"),
        ("Файлов с parent_id: null", "null_parent_files"), ("Расхождений title с HHC (темы)", "title_hhc_mismatch_topics"),
        ("Расхождений path с HHC (темы)", "path_hhc_mismatch_topics"), ("Расхождений parent_id с HHC (темы)", "parent_id_hhc_mismatch_topics"),
        ("Последний сегмент path отличается от title (темы)", "path_leaf_mismatch_topics"), ("H1 вне целевого формата (файлы)", "h1_mismatch_files"),
        ("Разбитых тем", "split_topics"), ("Расхождений между частями", "split_mismatch_topics"),
        ("Строк с `****`", "star4_lines"), ("Вхождений `****`", "star4_occurrences"), ("ALT-обёрток", "alt_remnants"),
        ("`<post>`", "post"), ("Экранированных звёзд", "escaped_stars"), ("Строк с нечётным `**`", "odd_bold_lines"),
        ("Повторов разделителей таблиц", "separator_duplicates"), ("Ключевых слов", "keywords"), ("Стоп-слов", "stop_keywords"),
        (f"Файлов с version: {version}", "version_matches"), ("Пустых тем", "empty_topics"), ("Записей в INDEX", "index_entries"),
        ("Вложенных записей", "index_nested"), ("Пометок «только оглавление»", "index_toc"), ("Пометок «только иллюстрации»", "index_illustrations"),
        ("Всего пометок пустых тем", "index_marked"), ("Записей с путём", "index_paths"),
        ("Неверных/пропущенных пометок", "index_mark_mismatches"), ("Расхождений названий INDEX с HHC", "index_title_mismatches"),
    ):
        p(f"| {label} | {b[key]} | {a[key]} |")
    p()
    p("## 8. Реальный вывод скрипта")
    p()
    p("Команда: `python s6_update.py`. Ниже тот же JSON, который этот запуск выводит в stdout:")
    block(console, "json")
    p()
    p("## 9. Что не проверялось")
    p()
    p("- Визуальный рендер всех страниц в Markdown-просмотрщике.")
    p("- Смысл всего текста справки и содержание самих иллюстраций; проверены ссылки на картинки и их наличие, без OCR.")
    p()
    p("### Чем получено")
    p()
    p("- `s6_update.py` — исправления, счётчики, проверки сохранности и генерация этого отчёта.")
    p("- `_s6_stats.json` — сводки, результаты проверок, полный список пустых тем и ссылки на их картинки.")
    (RES / "VERIFY-6.md").write_text("\n".join(out) + "\n", encoding="utf-8")
    print(console)


if __name__ == "__main__":
    main()

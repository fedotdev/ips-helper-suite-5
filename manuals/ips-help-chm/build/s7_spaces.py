"""Сжатие подряд идущих пробелов в title/path/H1/INDEX после создания result_backup3."""
import json
import re
import shutil
import sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")

BASE = Path(__file__).resolve().parent
ROOT = BASE.parent
RES, BAK = ROOT, ROOT / "result_backup3"
HHC = Path(r"D:\parsing\html\IPSHelp.hhc")
FM = re.compile(r"\A---\n(.*?)\n---\n", re.S)
HEADER_RE = re.compile(r"^(#+) (.*)$", re.M)
ENTRY_RE = re.compile(r"^(\s*-\s*\[)(.*?)(]\()([^)(]*)\)(.*)$")
SPACE = re.compile(r"[ \t]+")
SAMPLE_IDS = ["4362", "6057", "5335", "6044", "3453"]


def collapse(text):
    return SPACE.sub(" ", text)


def main():
    if BAK.exists():
        shutil.rmtree(BAK)
    shutil.copytree(RES, BAK, copy_function=shutil.copy2)

    samples_before, samples_after = {}, {}
    for sample_id in SAMPLE_IDS:
        match = list(RES.rglob(f"{sample_id}_*.md"))
        if match:
            samples_before[sample_id] = match[0].read_text(encoding="utf-8")

    topic_changed_files = 0
    index_changed_files = 0
    changed_lines = 0

    # Topic files: frontmatter title/path, then first H1
    for path in sorted(RES.rglob("*.md")):
        if path.name in ("INDEX.md", "REPORT.md") or path.name.startswith("VERIFY"):
            continue
        text = path.read_text(encoding="utf-8")
        new = text
        changed_lines_local = 0
        if match := FM.match(text):
            lines = match[1].splitlines()
            new_lines = []
            for line in lines:
                if line.startswith("title: ") or line.startswith("path: "):
                    key = line.split(":", 1)[0]
                    value = json.loads(line.split(": ", 1)[1])
                    collapsed = collapse(value)
                    new_line = key + ": " + json.dumps(collapsed, ensure_ascii=False)
                    if new_line != line:
                        changed_lines_local += 1
                        line = new_line
                new_lines.append(line)
            new = "---\n" + "\n".join(new_lines) + "\n---\n" + text[match.end():]
        # first H1 only
        if header := HEADER_RE.search(new):
            title = collapse(header[2])
            if title != header[2]:
                changed_lines_local += 1
                new = new[:header.start(2)] + title + new[header.end(2):]
        if new != text:
            path.write_text(new, encoding="utf-8")
            topic_changed_files += 1
            changed_lines += changed_lines_local

    # INDEX.md files: collapse spaces only in titles (keep path structure intact)
    index_files = sorted(RES.rglob("INDEX.md"))
    ENTRY_TAIL = re.compile(r"^(\s*-\s*\[)(.*?)(\]\()(.*?)(\)(.*)$)")
    for path in index_files:
        text = path.read_text(encoding="utf-8")
        lines = text.splitlines()
        new_lines = []
        for line in lines:
            if line.startswith("#"):
                new_lines.append(line)
                continue
            m = ENTRY_TAIL.match(line)
            if m:
                title = collapse(m[2])
                new_line = m[1] + title + m[3] + m[4] + m[5]
                if new_line != line:
                    changed_lines += 1
                    line = new_line
            new_lines.append(line)
        new = "\n".join(new_lines) + ("\n" if text.endswith("\n") else "")
        if new != text:
            path.write_text(new, encoding="utf-8")
            index_changed_files += 1

    for sample_id in SAMPLE_IDS:
        match = list(RES.rglob(f"{sample_id}_*.md"))
        if match:
            samples_after[sample_id] = match[0].read_text(encoding="utf-8")

    summary = {
        "backup_copy": str(BAK),
        "topic_files_changed": topic_changed_files,
        "index_files_changed": index_changed_files,
        "total_index_files": len(index_files),
        "changed_lines_total": changed_lines,
    }
    (BASE / "_s7_stats.json").write_text(json.dumps(summary, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    for sid in SAMPLE_IDS:
        if sid in samples_before and sid in samples_after and samples_before[sid] != samples_after[sid]:
            print(f"\n=== {sid} ===")
            before_lines = samples_before[sid].splitlines()
            after_lines = samples_after[sid].splitlines()
            print("До:")
            for line in before_lines[:14]:
                print(f"  {line}")
            print("После:")
            for line in after_lines[:14]:
                print(f"  {line}")


if __name__ == "__main__":
    main()

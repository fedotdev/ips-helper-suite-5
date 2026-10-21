#!/usr/bin/env python3
# Запуск: python3 evals/run_evals.py
import json
import sys
from pathlib import Path

FILES = ("technologist.json", "system-helper.json", "programmer.json")
EXPECTED_FIELDS = ("expected_route", "expected_tools")

def main():
    root = Path(__file__).parent
    total = 0
    failures = []
    for filename in FILES:
        try:
            cases = json.loads((root / filename).read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            failures.append(f"{filename}: {error}")
            continue
        if not isinstance(cases, list):
            failures.append(f"{filename}: root is not an array")
            continue
        for index, case in enumerate(cases):
            total += 1
            if not isinstance(case, dict):
                failures.append(f"{filename}[{index}]: case is not an object")
                continue
            if not isinstance(case.get("prompt"), str) or not case["prompt"].strip():
                failures.append(f"{filename}[{index}]: prompt is empty")
            for field in EXPECTED_FIELDS:
                if field not in case:
                    failures.append(f"{filename}[{index}]: missing {field}")
    print(f"cases: {total}")
    print(f"files: {len(FILES)}")
    if failures:
        for failure in failures:
            print(f"FAIL {failure}")
        return 1
    print("status: OK")
    return 0

if __name__ == "__main__":
    sys.exit(main())

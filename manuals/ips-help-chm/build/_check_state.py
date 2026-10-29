from pathlib import Path
res = Path(__file__).resolve().parent
res = res.parent  # ips-help-chm/
print("=== Current state ===")
for item in sorted(res.iterdir()):
    full = res / item
    kind = "DIR" if full.is_dir() else "FILE"
    print(f"  {item.name}: {kind}")
print()
print("=== build/ ===")
for item in sorted((res / 'build').iterdir()):
    print(f"  {item.name}")

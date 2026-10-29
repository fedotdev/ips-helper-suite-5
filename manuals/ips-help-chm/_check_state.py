import os
base = os.getcwd()
print("=== Root (ips-help-chm/) ===")
for f in sorted(os.listdir('.')):
    full = os.path.join(base, f)
    if os.path.isdir(full):
        cnt = sum(1 for dp, dn, fn in os.walk(full) for _ in fn)
        print(f"{f}/ ({cnt} files)")
    else:
        print(f"{f} ({os.path.getsize(full)} bytes)")
print()
print("=== build/ ===")
bd = os.path.join(base, 'build')
for f in sorted(os.listdir(bd)):
    full = os.path.join(bd, f)
    if os.path.isdir(full):
        cnt = sum(1 for dp, dn, fn in os.walk(full) for _ in fn)
        print(f"build/{f}/ ({cnt} files)")
    else:
        print(f"build/{f} ({os.path.getsize(full)} bytes)")

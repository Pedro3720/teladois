#!/usr/bin/env python3
"""Renomeia os prints exportados pelo xcresulttool para '<nome do anexo>.png'.

Uso: python3 rename-screenshots.py <pasta com manifest.json>
O export grava '<UUID>.png' e um manifest.json com 'suggestedHumanReadableName'
no formato '<nome>_<índice>_<UUID>.png'. Mantém o manifest.
"""
import json
import pathlib
import re
import sys

folder = pathlib.Path(sys.argv[1])
manifest = folder / "manifest.json"
if not manifest.exists():
    sys.exit(f"manifest.json não encontrado em {folder}")

pattern = re.compile(r"^(?P<name>.+)_\d+_[0-9A-Fa-f-]+\.png$")
used = set()
for test in json.loads(manifest.read_text(encoding="utf-8")):
    for att in test.get("attachments", []):
        src = folder / att["exportedFileName"]
        if not src.exists() or src.suffix != ".png":
            continue
        m = pattern.match(att.get("suggestedHumanReadableName", ""))
        base = m.group("name") if m else src.stem
        target = f"{base}.png"
        n = 2
        while target in used:
            target = f"{base}-{n}.png"
            n += 1
        used.add(target)
        src.rename(folder / target)
        print(f"{att['exportedFileName']} -> {target}")

#!/usr/bin/env python3
"""Конвертация CHM-справки IPS v3."""
import argparse, json, re, shutil, sys
from pathlib import Path

sys.stdout.reconfigure(encoding="utf-8")

SRC_ROOT = Path(r"D:\parsing\chm-converter\output\IPSHelp")
DST_ROOT = Path(__file__).resolve().parent / "result"
SRC_DATA = SRC_ROOT / "data"
SRC_CORE = SRC_ROOT / "core"
SPLIT_LIMIT = 30 * 1024
VERSION_STR = "unknown"  # номер версии IPS не найден в метаданных CHM

id_lookup = json.loads((SRC_CORE / "id_lookup.json").read_text(encoding="utf-8"))

TRANSLIT = {
    "а":"a","б":"b","в":"v","г":"g","д":"d","е":"e","ё":"e",
    "ж":"zh","з":"z","и":"i","й":"y","к":"k","л":"l","м":"m",
    "н":"n","о":"o","п":"p","р":"r","с":"s","т":"t","у":"u",
    "ф":"f","х":"h","ц":"c","ч":"ch","ш":"sh","щ":"sch","ъ":"",
    "ы":"y","ь":"","э":"e","ю":"yu","я":"ya",
}

STOPWORDS = {
    "в","на","и","по","с","к","от","для","из","как","что","этот","эта",
    "этот","не","да","же","ли","бы","ни","либо","или","а","но","все","всё",
    "каждый","каждая","каждое","каждые","это","эти","тот","та","те","без",
    "над","под","при","со","про","через",
}

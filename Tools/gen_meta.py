#!/usr/bin/env python3
"""Gera arquivos .meta para tudo em Assets/ que ainda não tem um.

Por que: arquivos criados fora da Unity (ex.: pelo Claude na nuvem) não têm .meta.
Commitando o .meta junto, o GUID de cada asset fica estável para todos.
Tipos com importer complexo (ex.: .inputactions, .png) são pulados:
a Unity gera o .meta deles e você commita.
"""
import os
import sys
import uuid

COMMON = "  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
TEMPLATES = {
    "folder": "folderAsset: yes\nDefaultImporter:\n" + COMMON,
    ".cs": "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n"
           "  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
    ".asmdef": "AssemblyDefinitionImporter:\n" + COMMON,
    ".md": "TextScriptImporter:\n" + COMMON,
    ".txt": "TextScriptImporter:\n" + COMMON,
    ".json": "TextScriptImporter:\n" + COMMON,
}


def write_meta(path, kind):
    with open(path + ".meta", "w", newline="\n") as f:
        f.write(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{TEMPLATES[kind]}")
    print("meta:", path)


def main(root):
    skipped = []
    for dirpath, dirnames, filenames in os.walk(root):
        dirnames[:] = [d for d in dirnames if not d.startswith(".")]
        for d in dirnames:
            p = os.path.join(dirpath, d)
            if not os.path.exists(p + ".meta"):
                write_meta(p, "folder")
        for name in filenames:
            if name.startswith(".") or name.endswith(".meta"):
                continue
            p = os.path.join(dirpath, name)
            if os.path.exists(p + ".meta"):
                continue
            ext = os.path.splitext(name)[1].lower()
            if ext in TEMPLATES:
                write_meta(p, ext)
            else:
                skipped.append(p)
    for p in skipped:
        print("pulado (a Unity gera o .meta):", p)


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Assets")

"""Merge a content batch's loc.json into the shared localization, the wiki's Russian strings
and the wiki's tier table.

    uv run --with hjson --python 3.12 python Art/merge_loc.py Art/shellback/loc.json [...]

A batch that is built in its own folders (a boss, its drops, its mobs) never edits the three
shared files itself - several batches may be built at once, and three writers on one hjson is
how a file loses half its keys. Each batch hands over a loc.json instead:

    {"en": {"Items": {Class: {DisplayName, Tooltip}}, "NPCs": {Class: {DisplayName, Bestiary}},
            "Projectiles": {Class: name}, "Buffs": {Class: {DisplayName, Description}},
            "Chat": {Key: text}},
     "ru": {"items": {Class: [name, tip]}, "npcs": {Class: [name, text]}, "buffs": {...}},
     "tiers": {Class: tier}}

Existing keys are left alone, so running it twice is harmless.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HJSON = ROOT / "Localization/en-US_Mods.Terrapex.hjson"
RU = ROOT / "Wiki/data/ru.json"
TIERS = ROOT / "Wiki/data/tiers.json"


def hval(text):
    """An hjson value: quoted when it could be misread (leading brace, colon, quote)."""
    if "\n" in text:
        body = "\n".join("\t\t\t" + l for l in text.split("\n"))
        return "\n\t\t\t'''\n" + body + "\n\t\t\t'''"
    if text[:1] in "{[\"'#" or text.strip() != text or not text:
        return json.dumps(text, ensure_ascii=False)
    return text


def block(name, fields):
    lines = [f"\t{name}: {{"]
    for k, v in fields.items():
        lines.append(f"\t\t{k}: {hval(v)}")
    lines.append("\t}")
    return "\n".join(lines) + "\n\n"


def insert_after_section_open(s, section, text):
    head = f"\n{section}: {{\n"
    i = s.index(head) + len(head)
    return s[:i] + text + s[i:]


def merge(path):
    data = json.loads(Path(path).read_text(encoding="utf-8"))
    en = data.get("en", {})
    s = HJSON.read_text(encoding="utf-8")

    for section in ("Items", "NPCs", "Buffs"):
        text = ""
        for cls, fields in en.get(section, {}).items():
            if f"\n\t{cls}: {{" not in s:
                text += block(cls, fields)
        if text:
            s = insert_after_section_open(s, section, text)
    text = ""
    for cls, name in en.get("Projectiles", {}).items():
        if f"\t{cls}.DisplayName:" not in s:
            text += f"\t{cls}.DisplayName: {hval(name)}\n"
    if text:
        s = insert_after_section_open(s, "Projectiles", text)
    # flat sections: one key, one line of text (Chat, SetBonus, ...)
    for section, lines in en.items():
        if section in ("Items", "NPCs", "Buffs", "Projectiles"):
            continue
        text = ""
        for key, line in lines.items():
            if f"\n\t{key}: " not in s:
                text += block(key, line) if isinstance(line, dict) else f"\t{key}: {hval(line)}\n"
        if text:
            if f"\n{section}: {{\n" not in s:
                s = s.rstrip("\n") + f"\n\n{section}: {{\n}}\n"
            s = insert_after_section_open(s, section, text)
    HJSON.write_text(s, encoding="utf-8")

    # the two wiki tables are hand-grouped by line, so they are inserted into as text rather
    # than re-dumped - a json.dumps would reflow every line of them for a few new keys
    rs = RU.read_text(encoding="utf-8")
    have = json.loads(rs)
    for section in ("items", "npcs", "buffs"):
        new = [f'    {json.dumps(cls)}: {json.dumps(pair, ensure_ascii=False)},'
               for cls, pair in data.get("ru", {}).get(section, {}).items()
               if cls not in have.get(section, {})]
        if new:
            head = f'  "{section}": {{\n'
            i = rs.index(head) + len(head)
            rs = rs[:i] + "\n".join(new) + "\n" + rs[i:]
    json.loads(rs)
    RU.write_text(rs, encoding="utf-8")

    ts = TIERS.read_text(encoding="utf-8")
    have = json.loads(ts)["content"]
    new = [f'{json.dumps(cls)}: {t}' for cls, t in data.get("tiers", {}).items() if cls not in have]
    if new:
        head = '  "content": {\n'
        i = ts.index(head) + len(head)
        ts = ts[:i] + "    " + ", ".join(new) + ",\n\n" + ts[i:]
    json.loads(ts)
    TIERS.write_text(ts, encoding="utf-8")
    print(f"merged {path}")


if __name__ == "__main__":
    for p in sys.argv[1:]:
        merge(p)
    import hjson
    hjson.loads(HJSON.read_text(encoding="utf-8"))
    print("hjson ok")

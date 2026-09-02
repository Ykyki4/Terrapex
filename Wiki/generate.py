"""Builds the Terrapex wiki out of the mod's own sources.

    uv run --with pillow --python 3.12 python Wiki/generate.py

Writes Wiki/docs/{en,ru}/ and the two mkdocs configs beside them. Everything in
there is disposable — it is regenerated from Content/, Localization/ and the two
tables in Wiki/data/ every run, so a balance change reaches the wiki by running
this rather than by editing prose.

The one thing that is NOT generated is Wiki/pages/{en,ru}/*.md: hand-written
pages (boss guides, the class setups intro) that get copied through and can
reference generated pages by their stable paths.
"""

from __future__ import annotations

import json
import re
import shutil
import sys
from collections import defaultdict
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from terrapex_parse import (ROOT, WIKI, HJSON, RARITY, coins, parse_hjson,
                            parse_loot, parse_source, parse_spawn)

try:
    from PIL import Image
except ImportError:
    sys.exit("Pillow is required:  uv run --with pillow --python 3.12 python Wiki/generate.py")

DOCS = WIKI / "docs"
LANGS = ("en", "ru")

# Folder -> (section slug, English label). Order here is the order in the nav.
ITEM_SECTIONS = [
    ("Weapons", "weapons", "Weapons"),
    ("Armor", "armor", "Armor"),
    ("Accessories", "accessories", "Accessories"),
    ("Tools", "tools", "Tools"),
    ("Ammo", "ammo", "Ammo"),
    ("Consumables", "consumables", "Consumables"),
    ("Materials", "materials", "Materials"),
    ("Placeable", "placeable", "Placeable"),
    ("Mounts", "mounts", "Mounts"),
    ("Pets", "pets", "Pets"),
]

CLASSES = [("melee", "Melee"), ("ranged", "Ranged"), ("magic", "Magic"), ("summon", "Summon")]


# ---------------------------------------------------------------- model

class Wiki:
    def __init__(self):
        self.loc = parse_hjson(HJSON.read_text(encoding="utf-8"))
        self.ru = json.loads((WIKI / "data" / "ru.json").read_text(encoding="utf-8"))
        self.tiers = json.loads((WIKI / "data" / "tiers.json").read_text(encoding="utf-8"))
        self.missing: list[str] = []

        self.ent = {}
        for p in sorted((ROOT / "Content").rglob("*.cs")):
            e = parse_source(p)
            if e:
                self.ent[e.name] = e

        # NPC extras
        self.loot, self.spawn, self.frames = {}, {}, {}
        for n, e in self.ent.items():
            if e.kind != "npc":
                continue
            src = e.path.read_text(encoding="utf-8", errors="replace")
            self.loot[n] = parse_loot(src)
            self.spawn[n] = parse_spawn(src)
            fc = re.search(r"Main\.npcFrameCount\[\s*Type\s*\]\s*=\s*(\d+)", src)
            self.frames[n] = int(fc.group(1)) if fc else 1
            if "NPC.boss = true" in src.replace(" ", "").replace("NPC.boss=true", "NPC.boss = true"):
                e.boss = True
            if re.search(r"NPC\.boss\s*=\s*true", src):
                e.boss = True

        # reverse indices
        self.dropped_by = defaultdict(list)
        for npc, rows in self.loot.items():
            for r in rows:
                self.dropped_by[r["item"]].append(dict(npc=npc, **r))

        self.used_in = defaultdict(list)
        for n, e in self.ent.items():
            for rec in e.recipes:
                for ing in rec.ingredients:
                    if ing.modded:
                        self.used_in[ing.ref].append((n, ing.count))

        # which item summons which boss, read from the summon item's tooltip
        self.summoned_by = {}
        for n, e in self.ent.items():
            if e.kind != "item":
                continue
            tip = self.loc["Items"].get(n, {}).get("Tooltip", "")
            m = re.search(r"Summons the ([\w' ]+)", tip)
            if m:
                target = m.group(1).strip().rstrip(".")
                for nn, ee in self.ent.items():
                    if ee.kind == "npc" and self.name(nn, "en").lower() == target.lower():
                        self.summoned_by[nn] = n

        # armour sets, by tier
        self.sets = self.tiers["sets"]

    # ---- strings

    def name(self, key: str, lang: str) -> str:
        if lang == "ru":
            for sec in ("items", "npcs", "buffs"):
                if key in self.ru[sec]:
                    return self.ru[sec][key][0]
            self.missing.append(f"ru name: {key}")
        for sec in ("Items", "NPCs", "Buffs", "Tiles", "Projectiles"):
            v = self.loc.get(sec, {}).get(key)
            if v and v.get("DisplayName"):
                return v["DisplayName"]
        return re.sub(r"(?<!^)(?=[A-Z])", " ", key)

    def tooltip(self, key: str, lang: str) -> str:
        if lang == "ru":
            for sec in ("items", "npcs", "buffs"):
                if key in self.ru[sec]:
                    return self.ru[sec][key][1]
        for sec in ("Items", "Buffs"):
            v = self.loc.get(sec, {}).get(key)
            if v:
                t = v.get("Tooltip") or v.get("Description") or ""
                return re.sub(r"\{\$CommonItemTooltip\.RightClickToOpen\}",
                              "Right click to open", t)
        return ""

    def bestiary(self, key: str, lang: str) -> str:
        if lang == "ru" and key in self.ru["npcs"]:
            return self.ru["npcs"][key][1]
        return self.loc.get("NPCs", {}).get(key, {}).get("Bestiary", "")

    def t(self, s: str, lang: str) -> str:
        """UI string."""
        if lang == "en":
            return s
        out = self.ru["ui"].get(s)
        if out is None:
            self.missing.append(f"ru ui: {s}")
            return s
        return out

    def rarity(self, key: str, lang: str):
        """(label, colour) for an ItemRarityID name."""
        label, colour = RARITY.get(key, (key, "#ffffff"))
        if lang == "ru":
            label = self.ru["rarity"].get(label, label)
        return label, colour

    def vanilla_item(self, key: str, lang: str) -> str:
        if lang == "ru":
            v = self.ru["vanilla_items"].get(key)
            if v:
                return v
            self.missing.append(f"ru vanilla item: {key}")
        return re.sub(r"(?<!^)(?=[A-Z])", " ", key)

    def station(self, key: str, lang: str) -> str:
        if key.startswith("vanilla:"):
            k = key.split(":", 1)[1]
            if lang == "ru":
                v = self.ru["vanilla_tiles"].get(k)
                if v:
                    return v
                self.missing.append(f"ru vanilla tile: {k}")
            return re.sub(r"(?<!^)(?=[A-Z])", " ", k)
        # a modded tile: name it after the item that places it, which is what
        # the player actually has in hand
        for n, e in self.ent.items():
            if e.kind == "item" and key.startswith(n):
                return self.name(n, lang)
        return self.name(key.replace("Tile", ""), lang)

    def tier(self, key: str):
        return self.tiers["content"].get(key)

    def tier_label(self, n: int, lang: str) -> str:
        return self.tiers["tiers"][str(n)][lang]


# ---------------------------------------------------------------- sprites

def export_sprites(w: Wiki) -> dict[str, str]:
    """Copies one representative frame of every sprite into docs/assets.

    NPC sheets are vertical atlases (see CLAUDE.md), so the whole file would
    render as a column of frames; frame 0 is what a wiki entry wants.
    """
    out = DOCS / "_assets" / "sprites"
    out.mkdir(parents=True, exist_ok=True)
    index = {}
    for name, e in w.ent.items():
        png = (ROOT / e.texture).with_suffix(".png") if e.texture else e.path.with_suffix(".png")
        if not png.exists():
            continue
        dest = out / f"{name}.png"
        try:
            img = Image.open(png).convert("RGBA")
            n = w.frames.get(name, 1)
            if e.kind == "npc" and n > 1 and img.height % n == 0:
                img = img.crop((0, 0, img.width, img.height // n))
            img.save(dest)
            index[name] = f"{name}.png"
        except Exception as exc:                       # a broken PNG must not stop the build
            print(f"  ! sprite {name}: {exc}")
    for extra, src in (("icon", ROOT / "icon.png"),
                       ("logo", WIKI / "Terrapex_Logo.png")):
        if src.exists():
            shutil.copy(src, out / f"{extra}.png")
            index[extra] = f"{extra}.png"
    return index


# ---------------------------------------------------------------- markdown bits

SPRITES: set[str] = set()


def sprite(name: str, depth: int, alt: str = "", cls: str = "sprite") -> str:
    """Empty when there is no sheet — a few classes ship no art of their own."""
    if name not in SPRITES:
        return ""
    return f'<img class="{cls}" src="{"../" * depth}assets/sprites/{name}.png" alt="{alt or name}">'


def link(w: Wiki, key: str, lang: str, depth: int, icon: bool = True) -> str:
    e = w.ent.get(key)
    if not e:
        return w.vanilla_item(key, lang)
    href = f'{"../" * depth}{page_path(e)}'
    label = w.name(key, lang)
    pic = sprite(key, depth, label, "inline-sprite") + " " if icon else ""
    return f"{pic}[{label}]({href})"


def page_path(e) -> str:
    if e.kind == "npc":
        return f"{'bosses' if e.boss else 'enemies'}/{e.name}.md"
    if e.kind == "buff":
        return f"buffs/{e.name}.md"
    slug = next((s for f, s, _ in ITEM_SECTIONS if f == e.folder), "other")
    return f"items/{slug}/{e.name}.md"


def table(head: list[str], rows: list[list[str]]) -> str:
    if not rows:
        return ""
    out = ["| " + " | ".join(head) + " |",
           "|" + "|".join("---" for _ in head) + "|"]
    out += ["| " + " | ".join(str(c) for c in r) + " |" for r in rows]
    return "\n".join(out) + "\n"


def ticks(v) -> str:
    return f"{v} ({v / 60:.2f}s)" if isinstance(v, (int, float)) else str(v)


# ---------------------------------------------------------------- item pages

def item_stats(w: Wiki, e, lang: str) -> str:
    s, rows = e.stats, []
    T = lambda k: w.t(k, lang)

    if "damage" in s:
        dc = T(s.get("damageClass", "Generic"))
        rows.append([T("Damage"), f"**{s['damage']}** {dc.lower()}"])
    if "defense" in s:
        rows.append([T("Defense"), f"**{s['defense']}**"])
    if "crit" in s:
        rows.append([T("Critical chance"), f"{4 + s['crit']}%"])
    if "knockBack" in s:
        rows.append([T("Knockback"), f"{s['knockBack']:g}"])
    if "useTime" in s:
        rows.append([T("Use time"), ticks(s["useTime"])])
    if "mana" in s:
        rows.append([T("Mana cost"), s["mana"]])
    if "shootSpeed" in s:
        rows.append([T("Velocity"), f"{s['shootSpeed']:g}"])
    if "pick" in s:
        rows.append(["Pickaxe power" if lang == "en" else "Сила кирки", f"{s['pick']}%"])
    if "axe" in s:
        rows.append(["Axe power" if lang == "en" else "Сила топора", f"{s['axe'] * 5}%"])
    if "hammer" in s:
        rows.append(["Hammer power" if lang == "en" else "Сила молота", f"{s['hammer']}%"])
    if "healLife" in s:
        rows.append(["Restores" if lang == "en" else "Восстанавливает", f"{s['healLife']} HP"])
    if "buffTime" in s:
        rows.append([T("Duration"), f"{s['buffTime'] // 60} {T('seconds')}"])
    if "rare" in s:
        label, colour = w.rarity(s["rare"], lang)
        rows.append([T("Rarity"), f'<span class="rarity" style="color:{colour}">{label}</span>'])
    if s.get("value"):
        rows.append([T("Sell"), money(s["value"] // 5, lang)])
    return table([T("Statistics"), ""], rows)


def crafting_block(w: Wiki, e, lang: str, depth: int) -> str:
    out = []
    T = lambda k: w.t(k, lang)
    if e.recipes:
        rows = []
        for rec in e.recipes:
            ing = "<br>".join(f"{link(w, i.ref, lang, depth)} ×{i.count}"
                              for i in rec.ingredients)
            st = " / ".join(w.station(t, lang) for t in rec.tiles)
            if rec.condition:
                cond = w.ru["conditions"].get(rec.condition, rec.condition) if lang == "ru" \
                    else re.sub(r"(?<!^)(?=[A-Z])", " ", rec.condition)
                st += f"<br>*{cond}*"
            rows.append([ing, st, f"×{rec.result}"])
        out.append(f"### {T('Crafting')}\n\n" +
                   table([T("Ingredients"), T("Station"), T("Result")], rows))

    used = w.used_in.get(e.name, [])
    if used:
        rows = [[link(w, target, lang, depth), f"×{n}"] for target, n in sorted(used)]
        out.append(f"### {T('Used in')}\n\n" + table([T("Result"), T("Quantity")], rows))
    return "\n".join(out)


def drop_block(w: Wiki, e, lang: str, depth: int) -> str:
    rows = w.dropped_by.get(e.name, [])
    if not rows:
        return ""
    T = lambda k: w.t(k, lang)
    body = [[link(w, r["npc"], lang, depth), chance(r["chance"], lang), r["amount"],
             note_ru(w, r["note"], lang)] for r in rows]
    return f"### {T('Dropped by')}\n\n" + table(
        [T("Source"), T("Chance"), T("Quantity"), ""], body)


COIN_RU = {"platinum": "плт", "gold": "зол", "silver": "сер", "copper": "мед"}


def money(copper, lang: str) -> str:
    """Coin string; Russian gets the short suffixes the game itself uses."""
    out = coins(copper)
    if lang == "ru":
        for en, ru in COIN_RU.items():
            out = out.replace(en, ru)
    return out


def chance(text: str, lang: str) -> str:
    if lang == "en":
        return text
    m = re.match(r"one of (\d+)$", text)
    return f"одно из {m.group(1)}" if m else text


def note_ru(w: Wiki, note: str, lang: str) -> str:
    if lang == "en" or not note:
        return note
    return {"Classic only": "только классика",
            "Expert+": "эксперт и выше",
            "Expert+ (treasure bag)": "эксперт и выше (из сумки)",
            "conditional": "по условию"}.get(note, note)


def item_page(w: Wiki, e, lang: str) -> str:
    depth = 2                       # items/<section>/<Name>.md -> two dirs up
    T = lambda k: w.t(k, lang)
    tier = w.tier(e.name)
    title = w.name(e.name, lang)

    md = [f"# {title}\n"]
    md.append('<div class="hero">')
    md.append(sprite(e.name, depth, title, "hero-sprite"))
    tip = w.tooltip(e.name, lang)
    if tip:
        md.append('<div class="tooltip">' + "<br>".join(
            l for l in tip.split("\n")) + "</div>")
    md.append("</div>\n")

    if tier is not None:
        md.append(f'!!! tier "{T("Tier")} {tier} — {w.tier_label(tier, lang)}"\n')

    md.append(item_stats(w, e, lang))

    if e.set_bonus_key:
        bonus = (w.ru["set_bonus"].get(e.set_bonus_key) if lang == "ru"
                 else w.loc["SetBonus"].get(e.set_bonus_key, ""))
        if bonus:
            bonus = bonus.replace("{0}", "8").replace("{1}", "15")
            md.append(f"### {T('Set bonus')}\n\n> {bonus}\n")
        if e.set_pieces:
            md.append("\n".join("- " + link(w, p, lang, depth)
                                for p in [e.name] + e.set_pieces) + "\n")

    md.append(crafting_block(w, e, lang, depth))
    md.append(drop_block(w, e, lang, depth))

    if e.summary:
        md.append(f"### {T('Notes')}\n\n{e.summary}\n")

    md.append(f'\n<small class="internal">`{e.name}` · '
              f'`Content/Items/{e.folder}/{e.name}.cs`</small>\n')
    return "\n".join(x for x in md if x)


# ---------------------------------------------------------------- npc pages

def npc_page(w: Wiki, e, lang: str) -> str:
    depth = 1                       # bosses|enemies/<Name>.md
    T = lambda k: w.t(k, lang)
    title = w.name(e.name, lang)
    tier = w.tier(e.name)

    md = [f"# {title}\n", '<div class="hero">',
          sprite(e.name, depth, title, "hero-sprite")]
    best = w.bestiary(e.name, lang)
    if best:
        md.append(f'<div class="tooltip">{best}</div>')
    md.append("</div>\n")

    if tier is not None:
        md.append(f'!!! tier "{T("Tier")} {tier} — {w.tier_label(tier, lang)}"\n')

    s = e.stats
    rows = []
    if "lifeMax" in s:
        rows.append([T("Health"), f"**{s['lifeMax']:,}**".replace(",", " ")])
    if "damage" in s:
        rows.append([T("Contact damage"), s["damage"]])
    if "defense" in s:
        rows.append([T("Defense"), s["defense"]])
    if s.get("value"):
        rows.append([T("Money"), money(int(s["value"]), lang)])
    if w.spawn.get(e.name):
        rows.append([T("Environment"), w.spawn[e.name]])
    md.append(table([T("Statistics"), ""], rows))

    if e.boss:
        md.append(f'!!! warning "{T("Boss")}"\n\n    '
                  + ("Health and damage shown are the base values; Expert and Master "
                     "scale them up." if lang == "en" else
                     "Здоровье и урон указаны базовые; эксперт и мастер их поднимают.") + "\n")

    if e.name in w.summoned_by:
        md.append(f"### {T('Summoned by')}\n\n"
                  + link(w, w.summoned_by[e.name], lang, depth) + "\n")

    rows = w.loot.get(e.name, [])
    if rows:
        body = [[link(w, r["item"], lang, depth), chance(r["chance"], lang), r["amount"],
                 note_ru(w, r["note"], lang)] for r in rows]
        md.append(f"### {T('Drops')}\n\n"
                  + table([T("Result"), T("Chance"), T("Quantity"), ""], body))

    if e.summary:
        md.append(f"### {T('Behaviour')}\n\n{e.summary}\n")

    md.append(f'\n<small class="internal">`{e.name}` · '
              f'`{e.path.relative_to(ROOT).as_posix()}`</small>\n')
    return "\n".join(x for x in md if x)


# ---------------------------------------------------------------- buff pages

def buff_page(w: Wiki, e, lang: str) -> str:
    depth = 1                       # buffs/<Name>.md
    T = lambda k: w.t(k, lang)
    title = w.name(e.name, lang)
    kind = T("Tag") if "tagBuff" in e.flags else T("Debuff") if "debuff" in e.flags else T("Buff")

    md = [f"# {title}\n", '<div class="hero">',
          sprite(e.name, depth, title, "hero-sprite"),
          f'<div class="tooltip">{w.tooltip(e.name, lang)}</div>', "</div>\n",
          table([T("Statistics"), ""], [[T("Type"), kind]])]

    # A whip or minion projectile is usually what calls AddBuff, but the player
    # holds the weapon, so resolve a projectile back to whatever shoots it.
    shot_by = {other.stats["shoot"]: n for n, other in w.ent.items()
               if other.kind == "item" and "shoot" in other.stats}
    givers = set()
    for n, other in w.ent.items():
        if other.stats.get("buffType") == e.name or other.stats.get("grantsBuff") == e.name:
            givers.add(shot_by.get(n, n) if other.kind == "projectile" else n)
    givers = sorted(g for g in givers if w.ent.get(g) and w.ent[g].kind != "projectile")
    if givers:
        md.append(f"### {T('Granted by')}\n\n"
                  + "\n".join("- " + link(w, g, lang, depth) for g in sorted(givers)) + "\n")

    if e.summary:
        md.append(f"### {T('Notes')}\n\n{e.summary}\n")
    md.append(f'\n<small class="internal">`{e.name}`</small>\n')
    return "\n".join(md)


# ---------------------------------------------------------------- index pages

def item_row(w: Wiki, e, lang: str, depth: int) -> list[str]:
    s = e.stats
    dmg = f"{s['damage']} {w.t(s.get('damageClass', 'Generic'), lang).lower()}" if "damage" in s else ""
    if "defense" in s and "damage" not in s:
        dmg = f"{s['defense']} {w.t('Defense', lang).lower()}"
    rare = w.rarity(s["rare"], lang) if "rare" in s else ("", "#fff")
    tier = w.tier(e.name)
    return [link(w, e.name, lang, depth),
            "" if tier is None else f"T{tier}",
            dmg,
            f'<span class="rarity" style="color:{rare[1]}">{rare[0]}</span>' if rare[0] else ""]


def section_index(w: Wiki, folder: str, label: str, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    ents = sorted((e for e in w.ent.values() if e.kind == "item" and e.folder == folder),
                  key=lambda e: (w.tier(e.name) if w.tier(e.name) is not None else 99,
                                 w.name(e.name, lang)))
    rows = [item_row(w, e, lang, 2) for e in ents]
    return (f"# {T(label)}\n\n"
            + f"{len(ents)} " + ("items" if lang == "en" else "предметов") + ".\n\n"
            + table(["", T("Tier"), T("Statistics"), T("Rarity")], rows))


def npc_index(w: Wiki, boss: bool, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    ents = sorted((e for e in w.ent.values() if e.kind == "npc" and e.boss == boss),
                  key=lambda e: (w.tier(e.name) if w.tier(e.name) is not None else 99,
                                 -(e.stats.get("lifeMax") or 0)))
    rows = []
    for e in ents:
        tier = w.tier(e.name)
        rows.append([link(w, e.name, lang, 1),
                     "" if tier is None else f"T{tier}",
                     f"{e.stats.get('lifeMax', ''):,}".replace(",", " "),
                     e.stats.get("damage", ""),
                     e.stats.get("defense", ""),
                     w.spawn.get(e.name, "")])
    return (f"# {T('Bosses' if boss else 'Enemies')}\n\n"
            + table(["", T("Tier"), T("Health"), T("Contact damage"),
                     T("Defense"), T("Environment")], rows))


def buff_index(w: Wiki, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    ents = sorted(( e for e in w.ent.values() if e.kind == "buff"),
                  key=lambda e: (w.tier(e.name) if w.tier(e.name) is not None else 99,
                                 w.name(e.name, lang)))
    rows = [[link(w, e.name, lang, 1),
             T("Tag") if "tagBuff" in e.flags else T("Debuff") if "debuff" in e.flags else T("Buff"),
             w.tooltip(e.name, lang).replace("\n", " · ")] for e in ents]
    return f"# {T('Buffs')}\n\n" + table(["", T("Type"), T("Effect")], rows)


def items_index(w: Wiki, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    total = sum(1 for e in w.ent.values() if e.kind == "item")
    md = [f"# {T('Items')}\n",
          (f"{total} items across seven tiers." if lang == "en"
           else f"{total} предметов на семь тиров.") + "\n"]
    rows = []
    for folder, slug, label in ITEM_SECTIONS:
        n = sum(1 for e in w.ent.values() if e.kind == "item" and e.folder == folder)
        if n:
            rows.append([f"[{T(label)}]({slug}/index.md)", n])
    md.append(table(["", "" if lang == "en" else "Штук"], rows))
    return "\n".join(md)


# ---------------------------------------------------------------- progression

def progression_page(w: Wiki, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    md = [f"# {T('Progression')}\n"]
    md.append(("Terrapex runs alongside vanilla rather than after it. Each tier opens "
               "at a vanilla milestone and closes at one of its own bosses.\n"
               if lang == "en" else
               "Terrapex идёт параллельно ванили, а не после неё. Каждый тир "
               "открывается ванильной вехой и закрывается собственным боссом.\n"))

    for n in range(7):
        info = w.tiers["tiers"][str(n)]
        md.append(f"## {T('Tier')} {n} — {info[lang]}\n")
        md.append(f"*{info['gate_' + lang]}*\n")

        bosses = [e for e in w.ent.values()
                  if e.kind == "npc" and e.boss and w.tier(e.name) == n]
        for b in bosses:
            hp = b.stats.get("lifeMax", 0)
            md.append(f"**{T('Boss')}:** {link(w, b.name, lang, 0)} — "
                      f"{hp:,}".replace(",", " ") + " HP\n")

        st = [(k, v) for k, v in w.sets.items() if v["tier"] == n]
        for key, s in st:
            heads = ", ".join(link(w, h, lang, 0, icon=False) for h in s["heads"])
            md.append(f"**{T('Armour')}:** {heads} + "
                      f"{link(w, s['body'], lang, 0, icon=False)} + "
                      f"{link(w, s['legs'], lang, 0, icon=False)} — "
                      f"{s['defense']} {T('Defense').lower()}\n")

        weapons = sorted((e for e in w.ent.values()
                          if e.kind == "item" and e.folder == "Weapons" and w.tier(e.name) == n),
                         key=lambda e: -(e.stats.get("damage") or 0))
        if weapons:
            rows = [[link(w, e.name, lang, 0),
                     w.t(e.stats.get("damageClass", "Generic"), lang),
                     e.stats.get("damage", "")] for e in weapons]
            md.append(table(["", T("Class"), T("Damage")], rows))
    return "\n".join(md)


# ---------------------------------------------------------------- class setups

def class_of(e) -> str | None:
    dc = e.stats.get("damageClass", "")
    if dc.startswith("Summon"):
        return "summon"
    return {"Melee": "melee", "Ranged": "ranged", "Magic": "magic"}.get(dc)


def head_class(w: Wiki, head: str) -> str:
    """The class a set head serves, from what its UpdateEquip actually boosts."""
    src = w.ent[head].path.read_text(encoding="utf-8", errors="replace")
    for cls, pat in (("melee", "Melee"), ("ranged", "Ranged"), ("magic", "Magic")):
        if re.search(rf"GetDamage\(DamageClass\.{pat}\)|GetCritChance\(DamageClass\.{pat}\)", src):
            return cls
    if re.search(r"maxMinions|GetDamage\(DamageClass\.Summon\)", src):
        return "summon"
    return "melee"


def head_for(w: Wiki, s: dict, cls: str) -> str | None:
    """The head of set `s` that serves `cls`, or None if the set does not."""
    if s["class"] == cls:
        return s["heads"][0]
    if s["class"] != "all":
        return None
    for h in s["heads"]:
        if head_class(w, h) == cls:
            return h
    return None


def armour_cell(w: Wiki, s: dict, head: str, lang: str) -> str:
    bonus_key = key_for_bonus(w, head)
    bonus = (w.ru["set_bonus"].get(bonus_key, "") if lang == "ru"
             else w.loc["SetBonus"].get(bonus_key, ""))
    bonus = bonus.replace("{0}", "8").replace("{1}", "15")
    cell = (f"{link(w, head, lang, 0)} + "
            f"{link(w, s['body'], lang, 0, icon=False)} + "
            f"{link(w, s['legs'], lang, 0, icon=False)}"
            f" — {s['defense']} {w.t('Defense', lang).lower()}")
    return cell + (f"<br>*{bonus}*" if bonus else "")


OFF_CLASS = {
    "en": ("This tier has no {cls} set of its own — {set} is the only one, and it "
           "spends its bonus elsewhere. Vanilla armour is usually the better pick here."),
    "ru": ("Своего сета под класс «{cls}» у этого тира нет — {set} единственный, и его "
           "бонус тратится на другое. Здесь обычно лучше ванильная броня."),
}


def class_setups_page(w: Wiki, lang: str) -> str:
    """One table per class per tier.

    The honesty rule: tiers 0-2 ship a single set each, aimed at one class, and
    showing it four times without comment would read as a recommendation to wear
    a mage's robe into melee. Where the tier has no set for the class, the table
    says so instead of pretending.
    """
    T = lambda k: w.t(k, lang)
    md = [f"# {T('Class setups')}\n"]
    md.append(("What Terrapex itself offers a build at each step. Vanilla gear stays "
               "better in several slots, so read these as the mod's contribution to a "
               "loadout rather than the whole of one. A tier with no set for your "
               "class says so.\n" if lang == "en" else
               "Что даёт сборке сам Terrapex на каждом шаге. В ряде слотов ваниль "
               "остаётся лучше, так что это вклад мода в набор, а не весь набор "
               "целиком. Если у тира нет сета под ваш класс, об этом сказано прямо.\n"))

    for n in range(7):
        info = w.tiers["tiers"][str(n)]
        md.append(f"## {T('Tier')} {n} — {info[lang]}\n")

        tier_items = [e for e in w.ent.values()
                      if e.kind == "item" and w.tier(e.name) == n]
        sets_here = [(k, s) for k, s in w.sets.items() if s["tier"] == n]

        # accessories and tools are class-agnostic, so they are listed once for
        # the tier rather than repeated in all four tables
        extras = sorted((e for e in tier_items if e.folder in ("Accessories", "Tools")),
                        key=lambda e: (e.folder, w.name(e.name, lang)))
        if extras:
            md.append(f"**{T('Accessories')}:** "
                      + " · ".join(link(w, e.name, lang, 0) for e in extras) + "\n")

        for cls, label in CLASSES:
            weapons = sorted((e for e in tier_items
                              if e.folder == "Weapons" and class_of(e) == cls),
                             key=lambda e: -(e.stats.get("damage") or 0))
            on_class = [(k, s, head_for(w, s, cls)) for k, s in sets_here
                        if head_for(w, s, cls)]

            if not weapons and not sets_here:
                continue

            md.append(f"### {T(label)}\n")
            rows = []
            if weapons:
                rows.append([T("Weapons"),
                             "<br>".join(f"{link(w, e.name, lang, 0)} — "
                                         f"**{e.stats.get('damage', '?')}**"
                                         for e in weapons)])
            if on_class:
                rows.append([T("Armour"), "<br><br>".join(
                    armour_cell(w, s, h, lang) for _, s, h in on_class)])
            elif sets_here:
                k, s = sets_here[0]
                rows.append([T("Armour"),
                             armour_cell(w, s, s["heads"][0], lang) + "<br>*"
                             + OFF_CLASS[lang].format(
                                 cls=T(label).lower(),
                                 set=w.ru["sets"].get(k, k) if lang == "ru" else k)
                             + "*"])
            md.append(table(["", ""], rows))
    return "\n".join(md)


def key_for_bonus(w: Wiki, head: str) -> str:
    return w.ent[head].set_bonus_key


# ---------------------------------------------------------------- home

def home_page(w: Wiki, lang: str) -> str:
    T = lambda k: w.t(k, lang)
    counts = {k: sum(1 for e in w.ent.values() if e.kind == k)
              for k in ("item", "npc", "buff")}
    bosses = sum(1 for e in w.ent.values() if e.kind == "npc" and e.boss)
    if lang == "en":
        blurb = (
            "**Terrapex** adds a full progression to Terraria: seven tiers, four bosses "
            "and two mini-bosses, running from your first cave to the far side of the "
            "Moon Lord. The world cracked long before you got here, and the crack is not "
            "an event but a substance — it crystallises into ore, grows dust, grows "
            "creatures, and eventually grows an eye to keep watch over itself.\n\n"
            "Every page here is generated from the mod's own source, so the numbers on "
            "it are the numbers in the game.")
    else:
        blurb = (
            "**Terrapex** добавляет в Terraria целую прогрессию: семь тиров, четыре "
            "босса и два мини-босса — от первой пещеры до того, что за Лунным Лордом. "
            "Мир треснул задолго до вас, и трещина — не событие, а вещество: она "
            "кристаллизуется в руду, растит прах, растит существ и в конце концов "
            "отращивает глаз, чтобы следить за собой.\n\n"
            "Каждая страница здесь сгенерирована из исходников мода, так что числа на "
            "ней — те же, что в игре.")

    cards = [
        (T("Progression"), "progression.md", "🪨"),
        (T("Class setups"), "class-setups.md", "⚔"),
        (T("Bosses"), "bosses/index.md", "👁"),
        (T("Enemies"), "enemies/index.md", "🕷"),
        (T("Items"), "items/index.md", "🔮"),
        (T("Buffs"), "buffs/index.md", "✨"),
    ]
    md = [f'<div class="home-hero">{sprite("logo", 0, "Terrapex", "logo")}</div>\n',
          f"# Terrapex {T('Wiki')}\n", blurb, "\n",
          '<div class="grid cards" markdown>\n']
    for label, href, emoji in cards:
        md.append(f"- {emoji} **[{label}]({href})**\n")
    md.append("</div>\n")
    md.append(table(["", ""], [
        [T("Items"), counts["item"]],
        [T("NPCs"), counts["npc"]],
        [T("Bosses"), bosses],
        [T("Buffs"), counts["buff"]],
    ]))
    return "\n".join(md)


# ---------------------------------------------------------------- nav + config

def build_nav(w: Wiki, lang: str) -> list:
    T = lambda k: w.t(k, lang)

    def sorted_items(folder):
        return sorted((e for e in w.ent.values()
                       if e.kind == "item" and e.folder == folder),
                      key=lambda e: (w.tier(e.name) if w.tier(e.name) is not None else 99,
                                     w.name(e.name, lang)))

    items = [{T("Items"): "items/index.md"}]
    for folder, slug, label in ITEM_SECTIONS:
        ents = sorted_items(folder)
        if not ents:
            continue
        items.append({T(label): [{T(label): f"items/{slug}/index.md"}] +
                      [{w.name(e.name, lang): f"items/{slug}/{e.name}.md"} for e in ents]})

    def npcs(boss):
        ents = sorted((e for e in w.ent.values() if e.kind == "npc" and e.boss == boss),
                      key=lambda e: (w.tier(e.name) if w.tier(e.name) is not None else 99,
                                     w.name(e.name, lang)))
        d = "bosses" if boss else "enemies"
        return [{T("Bosses" if boss else "Enemies"): f"{d}/index.md"}] + \
               [{w.name(e.name, lang): f"{d}/{e.name}.md"} for e in ents]

    buffs = sorted((e for e in w.ent.values() if e.kind == "buff"),
                   key=lambda e: w.name(e.name, lang))

    return [
        {T("Home"): "index.md"},
        {T("Progression"): "progression.md"},
        {T("Class setups"): "class-setups.md"},
        {T("Bosses"): npcs(True)},
        {T("Enemies"): npcs(False)},
        {T("Items"): items},
        {T("Buffs"): [{T("Buffs"): "buffs/index.md"}] +
                     [{w.name(e.name, lang): f"buffs/{e.name}.md"} for e in buffs]},
    ]


def yaml_dump(node, indent=0) -> str:
    """Just enough YAML for a mkdocs nav — avoids a PyYAML dependency."""
    pad = "  " * indent
    if isinstance(node, list):
        out = []
        for it in node:
            body = yaml_dump(it, indent + 1).lstrip()
            out.append(f"{pad}- {body}")
        return "\n".join(out)
    if isinstance(node, dict):
        (k, v), = node.items()
        if isinstance(v, str):
            return f"{pad}{json.dumps(k, ensure_ascii=False)}: {v}"
        return f"{pad}{json.dumps(k, ensure_ascii=False)}:\n" + yaml_dump(v, indent + 1)
    return f"{pad}{node}"


CONFIG = """\
site_name: {site_name}
site_description: {site_desc}
site_url: {site_url}
docs_dir: docs/{lang}
site_dir: {site_dir}
use_directory_urls: false

theme:
  name: material
  custom_dir: overrides
  language: {theme_lang}
  favicon: assets/sprites/icon.png
  logo: assets/sprites/icon.png
  icon:
    repo: fontawesome/brands/github
  palette:
    - media: "(prefers-color-scheme: dark)"
      scheme: slate
      primary: deep purple
      accent: purple
      toggle: {{icon: material/weather-sunny, name: Light}}
    - media: "(prefers-color-scheme: light)"
      scheme: default
      primary: deep purple
      accent: purple
      toggle: {{icon: material/weather-night, name: Dark}}
  features:
    - navigation.instant
    - navigation.tracking
    - navigation.top
    - navigation.sections
    - navigation.indexes
    - search.suggest
    - search.highlight
    - content.tooltips
    - toc.follow

extra_css:
  - assets/terrapex.css

extra:
  alternate:
    - name: English
      link: {alt_en}
      lang: en
    - name: Русский
      link: {alt_ru}
      lang: ru
  social:
    - icon: fontawesome/brands/github
      link: {repo_url}

markdown_extensions:
  - admonition
  - attr_list
  - md_in_html
  - tables
  - footnotes
  - toc:
      permalink: true
  - pymdownx.details
  - pymdownx.superfences
  - pymdownx.emoji:
      emoji_index: !!python/name:material.extensions.emoji.twemoji
      emoji_generator: !!python/name:material.extensions.emoji.to_svg

plugins:
  - search:
      lang: {search_lang}

nav:
{nav}
"""

def write_config(w: Wiki, lang: str):
    """Writes mkdocs.<lang>.yml.

    The config is generated rather than checked in because the nav is 200 entries
    long and has to follow whatever is in Content/. Everything site-specific
    (owner, repo name) comes from Wiki/data/site.json, which is the one file a
    human edits.
    """
    site = json.loads((WIKI / "data" / "site.json").read_text(encoding="utf-8"))
    owner, repo = site["owner"], site["repo"]
    base = f"/{repo}/"
    root = f"https://{owner}.github.io{base}"

    nav = yaml_dump(build_nav(w, lang), 1)
    cfg = CONFIG.format(
        site_name=site[f"site_name_{lang}"],
        site_desc=("Reference for the Terrapex mod for Terraria" if lang == "en"
                   else "Справочник по моду Terrapex для Terraria"),
        site_url=root if lang == "en" else root + "ru/",
        lang=lang,
        site_dir="site" if lang == "en" else "site/ru",
        theme_lang="en" if lang == "en" else "ru",
        search_lang="en" if lang == "en" else "ru",
        alt_en=base, alt_ru=base + "ru/",
        repo_url=f"https://github.com/{owner}/{repo}",
        nav=nav)
    (WIKI / f"mkdocs.{lang}.yml").write_text(cfg, encoding="utf-8")


# ---------------------------------------------------------------- driver

def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def main():
    w = Wiki()
    if DOCS.exists():
        shutil.rmtree(DOCS)

    sprites = export_sprites(w)
    SPRITES.update(sprites)
    print(f"{len(sprites)} sprites")

    for lang in LANGS:
        d = DOCS / lang
        # every language gets its own copy of the assets: mkdocs only serves
        # what is inside docs_dir, and docs_dir is per language
        shutil.copytree(DOCS / "_assets" / "sprites", d / "assets" / "sprites")
        shutil.copy(WIKI / "assets" / "terrapex.css", d / "assets" / "terrapex.css")

        write(d / "index.md", home_page(w, lang))
        write(d / "progression.md", progression_page(w, lang))
        write(d / "class-setups.md", class_setups_page(w, lang))

        for boss in (True, False):
            sub = "bosses" if boss else "enemies"
            write(d / sub / "index.md", npc_index(w, boss, lang))
        write(d / "buffs" / "index.md", buff_index(w, lang))
        write(d / "items" / "index.md", items_index(w, lang))
        for folder, slug, label in ITEM_SECTIONS:
            if any(e.folder == folder for e in w.ent.values() if e.kind == "item"):
                write(d / "items" / slug / "index.md", section_index(w, folder, label, lang))

        for e in w.ent.values():
            if e.kind == "item":
                write(d / page_path(e), item_page(w, e, lang))
            elif e.kind == "npc":
                write(d / page_path(e), npc_page(w, e, lang))
            elif e.kind == "buff":
                write(d / page_path(e), buff_page(w, e, lang))

        # hand-written pages win over generated ones of the same name
        hand = WIKI / "pages" / lang
        if hand.exists():
            for p in hand.rglob("*.md"):
                dest = d / p.relative_to(hand)
                dest.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy(p, dest)

        n = len(list(d.rglob("*.md")))
        print(f"{lang}: {n} pages")
        write_config(w, lang)

    shutil.rmtree(DOCS / "_assets")     # staging only; each language has its copy

    unmapped = [n for n, e in w.ent.items()
                if e.kind in ("item", "npc", "buff") and w.tier(n) is None]
    if unmapped:
        print("! no tier assigned:", ", ".join(sorted(unmapped)))
    if w.missing:
        seen = sorted(set(w.missing))
        print(f"! {len(seen)} missing translations")
        for m in seen[:20]:
            print("   ", m)


if __name__ == "__main__":
    main()

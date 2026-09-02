"""Reads the mod's own sources and returns plain dicts.

Nothing in here knows about Markdown or MkDocs — that is generate.py's job. The
split matters because the C# is the source of truth: when a damage number moves,
this file is where it is read, and every page that mentions it follows.
"""

from __future__ import annotations

import json
import re
from dataclasses import dataclass, field
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
WIKI = ROOT / "Wiki"
HJSON = ROOT / "Localization" / "en-US_Mods.Terrapex.hjson"


# ---------------------------------------------------------------- hjson

def parse_hjson(text: str) -> dict:
    """A parser for the one dialect tModLoader writes.

    Only three value forms occur in the file: a bare scalar to end of line, a
    ''' … ''' block, and a nested { } object. A real hjson library would be
    heavier and would still need the triple-quote handling.
    """
    root: dict = {}
    stack = [root]
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        raw = lines[i]
        line = raw.strip()
        i += 1
        if not line or line.startswith("#") or line.startswith("//"):
            continue
        if line == "}":
            if len(stack) > 1:
                stack.pop()
            continue
        m = re.match(r"^([A-Za-z0-9_]+):\s*\{\s*$", line)
        if m:
            child: dict = {}
            stack[-1][m.group(1)] = child
            stack.append(child)
            continue
        m = re.match(r"^([A-Za-z0-9_]+):\s*(.*)$", line)
        if not m:
            continue
        key, val = m.group(1), m.group(2).strip()
        if val == "":
            # a ''' block on the following lines
            if i < len(lines) and lines[i].strip() == "'''":
                i += 1
                body = []
                while i < len(lines) and lines[i].strip() != "'''":
                    body.append(lines[i].strip())
                    i += 1
                i += 1
                stack[-1][key] = "\n".join(body)
            else:
                stack[-1][key] = ""
            continue
        if val == '""' or val == "''":
            stack[-1][key] = ""
            continue
        stack[-1][key] = val.strip('"')
    return root


# ---------------------------------------------------------------- C#

RARITY = {
    "White": ("White", "#ffffff"), "Blue": ("Blue", "#9696ff"),
    "Green": ("Green", "#96ff96"), "Orange": ("Orange", "#ffc896"),
    "LightRed": ("Light Red", "#ff9696"), "Pink": ("Pink", "#ff96ff"),
    "LightPurple": ("Light Purple", "#d2a0ff"), "Lime": ("Lime", "#96ff0a"),
    "Yellow": ("Yellow", "#ffff0a"), "Cyan": ("Cyan", "#5ad2ff"),
    "Red": ("Red", "#ff2864"), "Purple": ("Purple", "#b428ff"),
    "Master": ("Master", "#ff3232"), "Expert": ("Expert", "#ffaf00"),
    "Quest": ("Quest", "#e4a010"), "Gray": ("Gray", "#828282"),
}

USE_STYLE = {
    "Swing": "Swing", "Shoot": "Shoot", "Thrust": "Thrust",
    "DrinkLiquid": "Drink", "EatFood": "Eat", "HoldUp": "Hold up",
    "Rapier": "Rapier", "MowTheLawn": "Swing", "None": "None",
}

DAMAGE_CLASS = {
    "Melee": "Melee", "Ranged": "Ranged", "Magic": "Magic",
    "Summon": "Summon", "SummonMeleeSpeed": "Summon (whip)",
    "Generic": "Generic", "Default": "Generic", "Throwing": "Throwing",
    "MeleeNoSpeed": "Melee",
}


@dataclass
class Ingredient:
    ref: str            # class name, or "ItemID.X"
    count: int = 1
    modded: bool = True


@dataclass
class Recipe:
    ingredients: list[Ingredient] = field(default_factory=list)
    tiles: list[str] = field(default_factory=list)
    result: int = 1
    condition: str = ""


@dataclass
class Entity:
    name: str                       # class name
    kind: str                       # item | npc | buff | tile
    path: Path = None
    folder: str = ""
    summary: str = ""
    stats: dict = field(default_factory=dict)
    flags: set = field(default_factory=set)
    recipes: list = field(default_factory=list)
    equip: list = field(default_factory=list)
    set_bonus_key: str = ""
    set_pieces: list = field(default_factory=list)
    boss: bool = False
    banner: str = ""
    texture: str = ""       # `override string Texture`, when the class borrows one


def _num(v: str):
    v = v.strip().rstrip("f").rstrip(";")
    try:
        return int(v)
    except ValueError:
        try:
            return float(v)
        except ValueError:
            return None


def _price(expr: str) -> int | None:
    """Item.sellPrice(gold: 8) → copper."""
    mult = {"platinum": 1000000, "gold": 10000, "silver": 100, "copper": 1}
    m = re.search(r"(?:sellPrice|buyPrice)\((.*?)\)", expr)
    if not m:
        n = _num(expr)
        return int(n) if isinstance(n, (int, float)) else None
    total = 0
    for k, v in re.findall(r"(\w+)\s*:\s*(\d+)", m.group(1)):
        total += mult.get(k, 0) * int(v)
    return total


def coins(copper: int | None) -> str:
    if not copper:
        return ""
    parts = []
    for name, unit in (("platinum", 1000000), ("gold", 10000), ("silver", 100), ("copper", 1)):
        n, copper = divmod(copper, unit)
        if n:
            parts.append(f"{n} {name}")
    return " ".join(parts)


ASSIGN = re.compile(r"^\s*(?:Item|NPC)\.(\w+)\s*(?:=|\+=)\s*(.+?);\s*$", re.M)


def parse_source(path: Path) -> Entity | None:
    src = path.read_text(encoding="utf-8", errors="replace")

    m = re.search(r"public\s+class\s+(\w+)\s*:\s*(\w+)", src)
    if not m:
        return None
    cls, base = m.group(1), m.group(2)
    kind = {"ModItem": "item", "ModNPC": "npc", "ModBuff": "buff",
            "ModTile": "tile", "ModProjectile": "projectile",
            "ModMount": "mount"}.get(base)
    if kind is None:
        return None

    e = Entity(name=cls, kind=kind, path=path)
    e.folder = path.parent.name

    doc = re.search(r"///\s*<summary>(.*?)///\s*</summary>", src, re.S)
    if doc:
        body = re.sub(r"^\s*///\s?", "", doc.group(1), flags=re.M)
        body = re.sub(r"<c>(.*?)</c>", r"`\1`", body, flags=re.S)
        body = re.sub(r"<see cref=\"(.*?)\"\s*/>", r"`\1`", body)
        e.summary = " ".join(body.split())

    # A class with `override string Texture` has no .png of its own -- it
    # borrows another's sheet, which is how FissureSlimelet costs no art.
    tex = re.search(r'override\s+string\s+Texture\s*=>\s*"Terrapex/([^"]+)"', src)
    if tex:
        e.texture = tex.group(1)

    # ---- attributes
    for et in re.findall(r"\[AutoloadEquip\(([^)]*)\)\]", src):
        e.equip += re.findall(r"EquipType\.(\w+)", et)
    if "[AutoloadBossHead]" in src:
        e.boss = True

    # ---- field assignments (whole file; SetDefaults is where they live but a
    #      few classes set damage in SetStaticDefaults or a helper)
    for key, val in ASSIGN.findall(src):
        val = val.strip()
        if key in ("width", "height", "damage", "defense", "crit", "useTime",
                   "useAnimation", "mana", "healLife", "healMana", "pick",
                   "axe", "hammer", "lifeMax", "maxStack", "buffTime",
                   "shootSpeed", "knockBack", "scale", "useTurn", "reuseDelay"):
            n = _num(val)
            if n is not None:
                e.stats.setdefault(key, n)
        elif key == "value":
            v = _price(val)
            if v is not None:
                e.stats.setdefault("value", v)
        elif key == "rare":
            r = re.search(r"ItemRarityID\.(\w+)", val)
            if r:
                e.stats.setdefault("rare", r.group(1))
            elif "ModContent.RarityType" in val:
                e.stats.setdefault("rare", "Custom")
        elif key == "DamageType":
            d = re.search(r"DamageClass\.(\w+)", val)
            if d:
                e.stats["damageClass"] = DAMAGE_CLASS.get(d.group(1), d.group(1))
        elif key == "useStyle":
            d = re.search(r"ItemUseStyleID\.(\w+)", val)
            if d:
                e.stats["useStyle"] = USE_STYLE.get(d.group(1), d.group(1))
        elif key == "buffType":
            d = re.search(r"BuffType<(\w+)>", val)
            if d:
                e.stats["buffType"] = d.group(1)
        elif key == "shoot":
            d = re.search(r"ProjectileType<(\w+)>", val)
            if d:
                e.stats["shoot"] = d.group(1)
            else:
                d = re.search(r"ProjectileID\.(\w+)", val)
                if d:
                    e.stats["shoot"] = "vanilla:" + d.group(1)
        elif key == "useAmmo":
            d = re.search(r"(?:AmmoID|ItemID)\.(\w+)", val)
            if d:
                e.stats["useAmmo"] = d.group(1)
        elif key == "ammo":
            d = re.search(r"(?:AmmoID|ItemID)\.(\w+)", val)
            if d:
                e.stats["ammo"] = d.group(1)
        elif val.strip() in ("true", "false"):
            if val.strip() == "true":
                e.flags.add(key)

    # anchored on Item./NPC. so a local `Dust d; d.noGravity = true` is not
    # mistaken for a property of the item
    for f in ("accessory", "consumable", "autoReuse", "noMelee", "channel",
              "vanity", "expert", "noUseGraphic", "boss", "friendly",
              "noGravity", "noTileCollide", "useTurn"):
        if re.search(rf"(?:Item|NPC)\.{f}\s*=\s*true", src):
            e.flags.add(f)

    if re.search(r"ItemID\.Sets\.Spears\[\s*(?:Type|Item\.type)\s*\]\s*=\s*true", src):
        e.flags.add("spear")
    if "ItemID.Sets.Yoyo" in src:
        e.flags.add("yoyo")
    if re.search(r"Item\.DefaultToWhip|ItemID\.Sets\.Whips", src):
        e.flags.add("whip")
    if re.search(r"NPCID\.Sets\.MPAllowedEnemies", src):
        e.flags.add("mpBoss")
    if re.search(r"Main\.debuff\[\s*Type\s*\]\s*=\s*true", src):
        e.flags.add("debuff")
    if re.search(r"BuffID\.Sets\.IsATagBuff\[\s*Type\s*\]\s*=\s*true", src):
        e.flags.add("tagBuff")
    if re.search(r"Main\.buffNoTimeDisplay\[\s*Type\s*\]\s*=\s*true", src):
        e.flags.add("noTimeDisplay")

    # ---- banner
    b = re.search(r"Banner\s*=\s*Item\.NPCtoBanner\(NPCID\.\w+\)|BannerItem\s*=\s*ModContent\.ItemType<(\w+)>", src)
    if b and b.group(1):
        e.banner = b.group(1)

    # ---- set bonus
    sb = re.search(r'Language\.GetTextValue\("Mods\.Terrapex\.SetBonus\.(\w+)"', src)
    if sb:
        e.set_bonus_key = sb.group(1)
    sp = re.search(r"IsArmorSet\(.*?\)\s*=>(.*?);", src, re.S)
    if sp:
        e.set_pieces = re.findall(r"ItemType<(\w+)>", sp.group(1))

    # ---- recipes
    e.recipes = parse_recipes(src)

    # ---- buff granted by an accessory/armour via AddBuff
    for bt in re.findall(r"AddBuff\(ModContent\.BuffType<(\w+)>", src):
        e.stats.setdefault("grantsBuff", bt)

    return e


REC_SPLIT = re.compile(r"CreateRecipe\(([^)]*)\)")


def calls(body: str, method: str):
    """Yields (generic-arg, argument-text) for every `.Method<T>(args)` in body.

    Written as a paren scan rather than a regex because the arguments routinely
    contain parentheses of their own — `AddTile(ModContent.TileType<X>())` is the
    common form in this mod, and a non-greedy `\\(([^)]*)\\)` truncates it.
    """
    for m in re.finditer(rf"\.{method}(?:<(\w+)>)?\(", body):
        depth, i = 1, m.end()
        while i < len(body) and depth:
            depth += {"(": 1, ")": -1}.get(body[i], 0)
            i += 1
        yield m.group(1), body[m.end():i - 1]


def parse_recipes(src: str) -> list[Recipe]:
    out = []
    for m in REC_SPLIT.finditer(src):
        result = _num(m.group(1)) or 1
        tail = src[m.end():]
        stop = tail.find(".Register()")
        if stop < 0:
            continue
        body = tail[:stop]
        r = Recipe(result=int(result))
        for generic, args in calls(body, "AddIngredient"):
            if generic:
                n = re.search(r"(\d+)", args)
                r.ingredients.append(Ingredient(generic, int(n.group(1)) if n else 1))
                continue
            mm = re.match(r"\s*ModContent\.ItemType<(\w+)>\(\)\s*(?:,\s*(\d+))?", args)
            if mm:
                r.ingredients.append(Ingredient(mm.group(1), int(mm.group(2) or 1)))
                continue
            vm = re.match(r"\s*ItemID\.(\w+)\s*(?:,\s*(\d+))?", args)
            if vm:
                r.ingredients.append(
                    Ingredient(vm.group(1), int(vm.group(2) or 1), modded=False))
        for _, args in calls(body, "AddRecipeGroup"):
            rg = re.match(r'\s*"?([\w:]+)"?\s*(?:,\s*(\d+))?', args)
            if rg:
                r.ingredients.append(
                    Ingredient(rg.group(1).split(":")[-1], int(rg.group(2) or 1), modded=False))
        for generic, args in calls(body, "AddTile"):
            if generic:
                r.tiles.append(generic)
                continue
            mt = re.search(r"ModContent\.TileType<(\w+)>", args)
            if mt:
                r.tiles.append(mt.group(1))
                continue
            vt = re.search(r"TileID\.(\w+)", args)
            if vt:
                r.tiles.append("vanilla:" + vt.group(1))
        cond = re.search(r"\.AddCondition\(Condition\.(\w+)\)", body)
        if cond:
            r.condition = cond.group(1)
        if not r.tiles:
            r.tiles.append("vanilla:ByHand")
        out.append(r)
    return out


# ---------------------------------------------------------------- loot

DROP_LINE = re.compile(r"ItemType<(\w+)>")


def _block(src: str, header: str) -> str:
    """Returns the brace body of the first method matching `header` (a regex)."""
    m = re.search(header, src)
    if not m:
        return ""
    depth, i = 1, m.end()
    while i < len(src) and depth:
        depth += {"{": 1, "}": -1}.get(src[i], 0)
        i += 1
    return src[m.end():i - 1]


CONDITION_LABEL = [
    (r"Conditions\.NotExpert", "Classic only"),
    (r"Conditions\.IsExpert|IsMasterMode", "Expert+"),
    (r"Conditions\.DownedAllMechBosses", "after all mech bosses"),
    (r"Conditions\.SoulOfNight|Conditions\.DownedPlantera", "conditional"),
]


def parse_loot(src: str) -> list[dict]:
    """Pulls (item, chance, amount, condition) out of a ModifyNPCLoot body.

    Statement-based rather than line-based: a rule routinely wraps across lines,
    and `ItemDropRule.Common(
    ModContent.ItemType<X>(), 34)` is the form that
    reads as a bare item with no chance if you scan line by line.

    Deliberately shallow: it reports what a player needs — the item, roughly how
    often, and whether a difficulty gate applies — rather than modelling
    tModLoader's full rule tree.
    """
    body = _block(src, r"ModifyNPCLoot\s*\(\s*NPCLoot\s+\w+\s*\)\s*\{")
    if not body:
        return []
    # strip comments first: statements are split on ";", so a trailing comment
    # would otherwise become the head of the next statement and swallow it
    body = re.sub(r"//[^\n]*", "", body)
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)

    # variable name -> the label its OnSuccess children inherit
    labels: dict[str, str] = {}
    out: list[dict] = []

    for stmt in body.split(";"):
        s = " ".join(stmt.split())
        if not s or s.startswith("//"):
            continue

        decl = re.match(r"(?:LeadingConditionRule|IItemDropRule|var)\s+(\w+)\s*=", s)
        if decl:
            label = ""
            for pat, text in CONDITION_LABEL:
                if re.search(pat, s):
                    label = text
                    break
            labels[decl.group(1)] = label
            continue

        owner = re.match(r"(\w+)\.OnSuccess\(", s)
        note = labels.get(owner.group(1), "") if owner else ""

        if "BossBag(" in s:
            for it in DROP_LINE.findall(s):
                out.append(dict(item=it, chance="100%", amount="1",
                                note="Expert+ (treasure bag)"))
            continue

        ofo = re.search(r"OneFromOptions\(\s*(\d+)\s*,(.*)$", s)
        if ofo:
            picks = DROP_LINE.findall(ofo.group(2))
            for it in picks:
                out.append(dict(item=it, chance=f"one of {len(picks)}",
                                amount="1", note=note))
            continue

        rule = re.search(r"(?:Common|NotScalingWithLuck)\(\s*ModContent\.ItemType<(\w+)>\(\)"
                         r"\s*(?:,\s*(\d+))?"
                         r"\s*(?:,\s*(\d+)\s*,\s*(\d+))?\s*\)", s)
        if rule:
            item, denom, lo, hi = rule.groups()
            chance = "100%" if denom in (None, "1") else f"1/{denom}"
            amount = f"{lo}–{hi}" if lo else "1"
            out.append(dict(item=item, chance=chance, amount=amount, note=note))
            continue

        for it in DROP_LINE.findall(s):
            out.append(dict(item=it, chance="—", amount="1", note=note))

    seen, dedup = set(), []
    for r in out:
        key = (r["item"], r["note"])
        if key not in seen:
            seen.add(key)
            dedup.append(r)
    return dedup


# ---------------------------------------------------------------- spawn

def parse_spawn(src: str) -> str:
    m = re.search(r"SpawnChance\s*\([^)]*\)\s*\{(.*?)\n\t\t\}", src, re.S)
    if not m:
        m2 = re.search(r"SpawnChance\s*\([^)]*\)\s*=>(.*?);", src, re.S)
        if not m2:
            return ""
        body = m2.group(1)
    else:
        body = m.group(1)
    bits = []
    zones = {
        "ZoneRockLayerHeight": "Cavern", "ZoneDirtLayerHeight": "Underground",
        "ZoneNormalUnderground": "Underground", "ZoneDungeon": "Dungeon",
        "ZoneCorrupt": "Corruption", "ZoneCrimson": "Crimson",
        "ZoneHallow": "Hallow", "ZoneJungle": "Jungle", "ZoneDesert": "Desert",
        "ZoneSnow": "Snow", "ZoneBeach": "Ocean", "ZoneSkyHeight": "Sky",
        "ZoneUnderworldHeight": "Underworld", "ZoneOverworldHeight": "Surface",
    }
    for k, v in zones.items():
        if k in body:
            bits.append(v)
    gates = {
        "downedBoss1": "after Eye of Cthulhu", "downedBoss2": "after Evil boss",
        "downedBoss3": "after Skeletron", "downedQueenBee": "after Queen Bee",
        "hardMode": "Hardmode", "downedMechBossAny": "after any mech boss",
        "downedMechBossAll": "after all mech bosses",
        "downedPlantBoss": "after Plantera", "downedGolemBoss": "after Golem",
        "downedMoonlord": "after Moon Lord", "downedKeeper": "after Keeper of the Rift",
        "downedWeaver": "after The Weaver", "downedDormantEye": "after The Dormant Eye",
        "downedFirstKeeper": "after The First Keeper",
    }
    for k, v in gates.items():
        if re.search(rf"\b{k}\b", body):
            bits.append(v)
    # `spawnInfo.Water` almost always appears inside the early-out guard, i.e. as
    # a reason NOT to spawn. Only a positive test counts as a water spawn.
    if re.search(r"spawnInfo\.Water\s*(?:&&|\?)", body) or re.search(r"return[^;]*spawnInfo\.Water", body):
        bits.append("water")
    chances = [float(c) for c in re.findall(r"return\s+([0-9.]+)f", body) if float(c) > 0]
    if chances:
        bits.append(f"weight {max(chances):g}")
    return ", ".join(dict.fromkeys(bits))


# ---------------------------------------------------------------- collect

def collect() -> dict:
    loc = parse_hjson(HJSON.read_text(encoding="utf-8"))
    entities: dict[str, Entity] = {}
    for p in sorted((ROOT / "Content").rglob("*.cs")):
        e = parse_source(p)
        if e:
            entities[e.name] = e

    # NPC-only extras
    loot_by_npc: dict[str, list] = {}
    for name, e in entities.items():
        if e.kind != "npc":
            continue
        src = e.path.read_text(encoding="utf-8", errors="replace")
        loot_by_npc[name] = parse_loot(src)
        e.stats["spawn"] = parse_spawn(src)
        if re.search(r"NPC\.boss\s*=\s*true", src):
            e.boss = True

    # reverse index: item -> where it drops from
    sources: dict[str, list] = {}
    for npc, rows in loot_by_npc.items():
        for r in rows:
            sources.setdefault(r["item"], []).append(dict(npc=npc, **r))

    tiers = json.loads((WIKI / "data" / "tiers.json").read_text(encoding="utf-8"))
    ru = json.loads((WIKI / "data" / "ru.json").read_text(encoding="utf-8"))

    return dict(loc=loc, entities=entities, loot=loot_by_npc,
                sources=sources, tiers=tiers, ru=ru)


if __name__ == "__main__":
    import sys
    data = collect() if (WIKI / "data" / "tiers.json").exists() else None
    if data is None:
        loc = parse_hjson(HJSON.read_text(encoding="utf-8"))
        ents = {}
        for p in sorted((ROOT / "Content").rglob("*.cs")):
            e = parse_source(p)
            if e:
                ents[e.name] = e
        print(f"{len(ents)} entities, sections: {list(loc)}")
        for k in ("item", "npc", "buff", "tile", "projectile", "mount"):
            print(f"  {k:11} {sum(1 for e in ents.values() if e.kind == k)}")
        sys.exit()
    print(f"{len(data['entities'])} entities")

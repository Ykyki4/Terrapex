"""Inspection only: the reaper's four sets, in order, on a real Terraria player.

uv run --with pillow --python 3.12 python Art/reaper/preview_biome_sets.py

Scarecrow -> Tidewrack -> Sunwrought -> Ronin, so a set that only recolours its
neighbour shows up at once. The base player comes from Art/reference/vanilla_player/
(git-ignored, Re-Logic's assets - never published); without it the open face of a
helmet reads as a hole. Also checks every new PNG is 100% aligned 2x2 blocks.
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
REPO = HERE / "../.."
ARM = REPO / "Content/Items/Armor"
ARMR = ARM / "Reaper"
ACCR = REPO / "Content/Items/Accessories/Reaper"
VAN = REPO / "Art/reference/vanilla_player"
K = 4
BG = (46, 40, 62, 255)
SKIN = (255, 125, 90)
EYES = (105, 90, 75)

SETS = [
    ("Scarecrow", ARM, "ScarecrowHat", "ScarecrowShirt", "ScarecrowTrousers"),
    ("Tidewrack", ARMR, "TidewrackHood", "TidewrackCoat", "TidewrackBoots"),
    ("Sunwrought", ARMR, "SunwroughtNemes", "SunwroughtCarapace", "SunwroughtGreaves"),
    ("Ronin", ARM, "RoninKabuto", "RoninDo", "RoninSuneate"),
]
ACCS = ["BarnacledHone", "ScarabAmulet", "RimeboundWrap"]
POSES = [(0, (2, 0)), (8, (7, 0)), (12, (8, 0)), (0, (4, 0)), (0, (5, 0))]
BACK_ARM = (7, 2)


def load(p):
    return Image.open(p).convert("RGBA")


def tint(im, c):
    r, g, b, a = im.split()
    return Image.merge("RGBA", (r.point(lambda v: v * c[0] // 255), g.point(lambda v: v * c[1] // 255),
                                b.point(lambda v: v * c[2] // 255), a))


def cell(s, cx, cy):
    return s.crop((cx * 40, cy * 56, cx * 40 + 40, cy * 56 + 56))


def strip(s, f):
    return s.crop((0, f * 56, 40, f * 56 + 56))


base = None
if VAN.exists():
    base = {
        "head": tint(load(VAN / "Player_0_0.png"), SKIN),
        "whites": load(VAN / "Player_0_1.png"),
        "eyes": tint(load(VAN / "Player_0_2.png"), EYES),
        "torso": tint(load(VAN / "Player_0_3.png"), SKIN),
        "hand_back": tint(load(VAN / "Player_0_5.png"), SKIN),
        "arm": tint(load(VAN / "Player_0_7.png"), SKIN),
        "hand": tint(load(VAN / "Player_0_9.png"), SKIN),
        "legs": tint(load(VAN / "Player_0_10.png"), SKIN),
    }


def compose(head, body, legs, f, arm):
    out = Image.new("RGBA", (40, 56))
    L = out.alpha_composite
    if base:
        L(cell(base["arm"], *BACK_ARM)); L(cell(base["hand_back"], *BACK_ARM))
    L(cell(body, *BACK_ARM))
    if base:
        L(strip(base["legs"], f))
    L(strip(legs, f))
    if base:
        L(cell(base["torso"], 0, 0))
    L(cell(body, 0, 0))
    if base:
        for k in ("head", "whites", "eyes"):
            L(strip(base[k], f))
    L(strip(head, f))
    if base:
        L(cell(base["arm"], *arm)); L(cell(base["hand"], *arm))
    L(cell(body, *arm))
    L(cell(body, 0, 1))
    return out


def aligned(path):
    im = load(path)
    w, h = im.size
    if w % 2 or h % 2:
        return 0.0
    px = im.load()
    total = ok = 0
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            q = {px[x, y], px[x + 1, y], px[x, y + 1], px[x + 1, y + 1]}
            total += 1
            ok += len(q) == 1
    return ok / total


rows = []
for name, d, h, b, l in SETS:
    head, body, legs = load(d / f"{h}_Head.png"), load(d / f"{b}_Body.png"), load(d / f"{l}_Legs.png")
    rows.append((name, [compose(head, body, legs, f, a) for f, a in POSES], [load(d / f"{n}.png") for n in (h, b, l)]))
rows.append(("Accessories", [], [load(ACCR / f"{n}.png") for n in ACCS]))

FW = 40 * K + 10
W = 16 + len(POSES) * FW + 3 * (48 * K // 2 + 16) + 140
RH = 56 * K + 36
sheet = Image.new("RGBA", (W, RH * len(rows) + 8), BG)
dr = ImageDraw.Draw(sheet)
for r, (name, frames, icons) in enumerate(rows):
    y = r * RH + 8
    if r:
        dr.line([(0, y - 6), (W, y - 6)], fill=(80, 72, 100, 255), width=2)
    dr.text((16, y), name, fill=(240, 230, 250, 255))
    x = 16
    for fr in frames:
        sheet.alpha_composite(fr.resize((40 * K, 56 * K), Image.NEAREST), (x, y + 20))
        x += FW
    x += 8
    for ic in icons:
        s = K if frames else K + 1
        sheet.alpha_composite(ic.resize((ic.width * s, ic.height * s), Image.NEAREST), (x, y + 50))
        x += ic.width * s + 16
sheet.save(HERE / "preview_biome_sets.png")
print(sheet.size, "with vanilla player" if base else "NO vanilla player base")

bad = []
for p in sorted(list(ARMR.glob("*.png")) + list(ACCR.glob("*.png"))):
    a = aligned(p)
    print(f"  {p.name:34s} {load(p).size}  2x2 {a:.0%}")
    if a < 1:
        bad.append(p.name)
print("ALL 2x2" if not bad else f"NOT ALIGNED: {bad}")

"""Inspection only: Ronin next to three Calamity sets, on a real Terraria player.

uv run --with pillow --python 3.12 python Art/concepts/ronin/compare.py

The base player comes from Art/reference/vanilla_player/ - layers extracted from
the local Terraria install (git-ignored, Re-Logic's assets). Without it the
armour is judged on an empty mannequin, and the open face of a helmet reads as
a hole instead of as a face.
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
REF = HERE / "../../reference"
CAL = REF / "calamity/armor"
VAN = REF / "vanilla_player"
K = 5
BG = (46, 40, 62, 255)

SKIN = (255, 125, 90)
EYES = (105, 90, 75)

SETS = [
    ("Ronin (ours)", HERE, "RoninKabuto", "RoninDo", "RoninSuneate"),
    ("Reaver", CAL, "ReaverHeadTank", "ReaverScaleMail", "ReaverCuisses"),
    ("Daedalus", CAL, "DaedalusHeadMelee", "DaedalusBreastplate", "DaedalusLeggings"),
    ("Hydrothermic", CAL, "HydrothermicHeadMelee", "HydrothermicArmor", "HydrothermicSubligar"),
]
# (leg/head frame, front arm cell)
POSES = [(0, (2, 0)), (8, (7, 0)), (12, (8, 0)), (0, (4, 0)), (0, (5, 0))]
BACK_ARM = (7, 2)


def load(path):
    return Image.open(path).convert("RGBA")


def tint(im, color):
    r, g, b, a = im.split()
    out = Image.merge("RGBA", (
        r.point(lambda v: v * color[0] // 255),
        g.point(lambda v: v * color[1] // 255),
        b.point(lambda v: v * color[2] // 255),
        a))
    return out


def cell(sheet, cx, cy):
    return sheet.crop((cx * 40, cy * 56, cx * 40 + 40, cy * 56 + 56))


def strip(sheet, f):
    return sheet.crop((0, f * 56, 40, f * 56 + 56))


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

    def layer(im):
        out.alpha_composite(im)

    if base:
        layer(cell(base["arm"], *BACK_ARM)); layer(cell(base["hand_back"], *BACK_ARM))
    layer(cell(body, *BACK_ARM))
    if base:
        layer(strip(base["legs"], f))
    layer(strip(legs, f))
    if base:
        layer(cell(base["torso"], 0, 0))
    layer(cell(body, 0, 0))
    if base:
        for k in ("head", "whites", "eyes"):
            layer(strip(base[k], f))
    layer(strip(head, f))
    if base:
        layer(cell(base["arm"], *arm)); layer(cell(base["hand"], *arm))
    layer(cell(body, *arm))
    layer(cell(body, 0, 1))  # front shoulder piece
    return out


rows = []
for name, d, h, b, l in SETS:
    head, body, legs = load(d / f"{h}_Head.png"), load(d / f"{b}_Body.png"), load(d / f"{l}_Legs.png")
    frames = [compose(head, body, legs, f, arm) for f, arm in POSES]
    icons = [load(d / f"{n}.png") for n in (h, b, l)]
    rows.append((name, frames, icons))

FW = 40 * K + 12
ICON_W = max(sum(i.width * K + 16 for i in ic) for _, _, ic in rows)
W = 16 + len(POSES) * FW + ICON_W + 16
RH = 56 * K + 40
sheet = Image.new("RGBA", (W, RH * len(rows) + 8), BG)
draw = ImageDraw.Draw(sheet)
for r, (name, frames, icons) in enumerate(rows):
    y = r * RH + 8
    if r:
        draw.line([(0, y - 6), (W, y - 6)], fill=(80, 72, 100, 255), width=2)
    draw.text((16, y), name, fill=(240, 230, 250, 255))
    x = 16
    for fr in frames:
        sheet.alpha_composite(fr.resize((40 * K, 56 * K), Image.NEAREST), (x, y + 24))
        x += FW
    x += 8
    for ic in icons:
        sheet.alpha_composite(ic.resize((ic.width * K, ic.height * K), Image.NEAREST), (x, y + 60))
        x += ic.width * K + 16
sheet.save(HERE / "compare_calamity.png")
print(sheet.size, "with vanilla player" if base else "NO vanilla player base")

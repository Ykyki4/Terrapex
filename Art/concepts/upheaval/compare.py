"""Inspection only: Upheaval next to three Calamity sets, same frames, same scale.

uv run --with pillow --python 3.12 python Art/concepts/upheaval/compare.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
CAL = HERE / "../../reference/calamity/armor"
K = 5
BG = (46, 40, 62, 255)

SETS = [
    ("Upheaval (ours)", HERE, "UpheavalHelm", "UpheavalPlate", "UpheavalGreaves"),
    ("Reaver", CAL, "ReaverHeadTank", "ReaverScaleMail", "ReaverCuisses"),
    ("Daedalus", CAL, "DaedalusHeadMelee", "DaedalusBreastplate", "DaedalusLeggings"),
    ("Hydrothermic", CAL, "HydrothermicHeadMelee", "HydrothermicArmor", "HydrothermicSubligar"),
]
# (leg/head frame, front arm cell)
POSES = [(0, (2, 0)), (8, (7, 0)), (12, (8, 0)), (0, (4, 0)), (0, (5, 0))]


def load(d, n):
    return Image.open(d / f"{n}.png").convert("RGBA")


def cell(sheet, cx, cy):
    return sheet.crop((cx * 40, cy * 56, cx * 40 + 40, cy * 56 + 56))


rows = []
for name, d, h, b, l in SETS:
    head, body, legs = load(d, h + "_Head"), load(d, b + "_Body"), load(d, l + "_Legs")
    frames = []
    for f, arm in POSES:
        out = Image.new("RGBA", (40, 56))
        out.alpha_composite(cell(body, 7, 2))
        out.alpha_composite(legs.crop((0, f * 56, 40, f * 56 + 56)))
        out.alpha_composite(cell(body, 0, 0))
        out.alpha_composite(head.crop((0, f * 56, 40, f * 56 + 56)))
        out.alpha_composite(cell(body, *arm))
        frames.append(out.crop((0, 0, 40, 56)))
    icons = [load(d, n) for n in (h, b, l)]
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
print(sheet.size)

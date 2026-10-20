"""Inspection only: lays the ranged tree out next to its ingredients, and checks the 2x2 grid.

    uv run --with pillow --python 3.12 python Art/trees/preview_ranged.py
"""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
ING = ["Content/Items/Weapons/Shellback/BarnacleBlunderbuss.png", "Content/Items/Weapons/Scarab/SunwingBow.png",
       "Content/Items/Weapons/Rime/HoarfrostBow.png", "Content/Items/Weapons/Cleft.png",
       "Content/Items/Weapons/Rib.png", "Content/Items/Weapons/Weft.png", "Content/Items/Weapons/Unblinking.png"]
TREE = ["Wayfinder", "CompassRose", "TradeWind", "Horizon"]
PROJ = ["WindArrow", "WindPellet", "WindIcicle"]
S = 4


def aligned(im):
    a = im.load()
    tot = ok = 0
    for y in range(0, im.height - 1, 2):
        for x in range(0, im.width - 1, 2):
            px = {a[x, y], a[x + 1, y], a[x, y + 1], a[x + 1, y + 1]}
            tot += 1
            ok += len(px) == 1
    return ok / max(tot, 1)


def row(paths, label):
    ims = [Image.open(ROOT / p).convert("RGBA") for p in paths]
    for p, im in zip(paths, ims):
        print(f"{label:5} {Path(p).name:28} {im.size}  2x2 aligned {aligned(im) * 100:.1f}%")
    return ims


ing = row(ING, "ing")
tree = row([f"Content/Items/Weapons/Trees/Ranged/{n}.png" for n in TREE], "tree")
proj = row([f"Content/Projectiles/Trees/Ranged/{n}.png" for n in PROJ], "proj")

rows = [ing, tree, proj]
W = max(sum(i.width * S + 24 for i in r) for r in rows) + 24
H = sum(max(i.height for i in r) * S + 30 for r in rows) + 24
canvas = Image.new("RGBA", (W, H), (34, 38, 54, 255))
d = ImageDraw.Draw(canvas)
y = 12
for r in rows:
    x = 12
    for i in r:
        canvas.alpha_composite(i.resize((i.width * S, i.height * S), Image.NEAREST), (x, y))
        x += i.width * S + 24
    y += max(i.height for i in r) * S + 30
canvas.save(ROOT / "Art/trees/preview_ranged.png")
print("wrote Art/trees/preview_ranged.png")
